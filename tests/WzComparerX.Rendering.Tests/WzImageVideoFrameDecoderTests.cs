using System.Security.Cryptography;
using WzComparerX.Rendering;
using WzComparerX.WzLib;

namespace WzComparerX.Rendering.Tests;

public sealed class WzImageVideoFrameDecoderTests
{
    private const string MainPacketPath = "/tmp/vp9-main-frame-0.vp9";
    private const string AlphaPacketPath = "/tmp/vp9-alpha-frame-0.vp9";
    private const string MergedFrameHash = "c8095ee5e4b760a8a6f7c18d10b357b9f579c6864bb1cd815061d8d6e930a2ff";

    [Fact]
    public void DecodeFrame_ReadsColorAndAlphaPacketsFromImagePayloadStream()
    {
        var imagePayload = new byte[32];
        imagePayload[12] = 1;
        imagePayload[13] = 2;
        imagePayload[14] = 3;
        imagePayload[20] = 9;
        imagePayload[21] = 8;
        var decoder = new RecordingRawVideoPacketDecoder();
        var service = new WzImageVideoFrameDecoder(decoder);
        var video = CreateVideo(
            dataOffset: 10,
            fourCc: "VP90",
            frames:
            [
                new WzImageVideoFrameInspection(
                    0,
                    DataOffset: 2,
                    DataLength: 3,
                    AlphaDataOffset: 10,
                    AlphaDataLength: 2,
                    DelayInNanoseconds: 16000000,
                    StartTimeInNanoseconds: 32000000)
            ]);

        var result = service.DecodeFrame(new MemoryStream(imagePayload), video, 0);

        Assert.True(result.Succeeded, result.Diagnostic?.Message);
        Assert.Equal([1, 2, 3], decoder.ColorPacket);
        Assert.Equal([9, 8], decoder.AlphaPacket);
        Assert.Equal(4, decoder.ExpectedWidth);
        Assert.Equal(2, decoder.ExpectedHeight);
        Assert.NotNull(result.Frame);
        Assert.Equal(0, result.Frame.FrameIndex);
        Assert.Equal(16000000, result.Frame.DelayInNanoseconds);
        Assert.Equal(32000000, result.Frame.StartTimeInNanoseconds);
        Assert.Equal(WzVideoPixelFormat.Bgra8888, result.Frame.PixelFormat);
    }

    [Fact]
    public void DecodeFrame_WhenAlphaPacketIsAbsent_DecodesColorOnly()
    {
        var imagePayload = new byte[16];
        imagePayload[6] = 4;
        imagePayload[7] = 5;
        var decoder = new RecordingRawVideoPacketDecoder();
        var service = new WzImageVideoFrameDecoder(decoder);
        var video = CreateVideo(
            dataOffset: 4,
            fourCc: "VP90",
            frames:
            [
                new WzImageVideoFrameInspection(
                    0,
                    DataOffset: 2,
                    DataLength: 2,
                    AlphaDataOffset: -1,
                    AlphaDataLength: 0,
                    DelayInNanoseconds: 1,
                    StartTimeInNanoseconds: 0)
            ]);

        var result = service.DecodeFrame(new MemoryStream(imagePayload), video, 0);

        Assert.True(result.Succeeded, result.Diagnostic?.Message);
        Assert.Equal([4, 5], decoder.ColorPacket);
        Assert.Null(decoder.AlphaPacket);
    }

    [Fact]
    public void DecodeFrame_WhenCodecIsUnsupported_ReturnsDiagnosticWithoutCallingRawDecoder()
    {
        var decoder = new RecordingRawVideoPacketDecoder();
        var service = new WzImageVideoFrameDecoder(decoder);
        var video = CreateVideo(
            dataOffset: 0,
            fourCc: "VP80",
            frames:
            [
                new WzImageVideoFrameInspection(0, 0, 1, -1, 0, 0, 0)
            ]);

        var result = service.DecodeFrame(new MemoryStream([0]), video, 0);

        Assert.False(result.Succeeded);
        Assert.Equal("wcx.video.codec.unsupported", result.Diagnostic?.Code);
        Assert.False(decoder.WasCalled);
    }

    [Fact]
    public void DecodeFrame_WhenFrameIndexIsOutsideTable_ReturnsDiagnostic()
    {
        var service = new WzImageVideoFrameDecoder(new RecordingRawVideoPacketDecoder());
        var video = CreateVideo(dataOffset: 0, fourCc: "VP90", frames: []);

        var result = service.DecodeFrame(new MemoryStream(), video, 0);

        Assert.False(result.Succeeded);
        Assert.Equal("wcx.video.frame.invalidIndex", result.Diagnostic?.Code);
    }

    [Fact]
    public void DecodeFrame_WhenPacketExtendsPastStream_ReturnsDiagnostic()
    {
        var service = new WzImageVideoFrameDecoder(new RecordingRawVideoPacketDecoder());
        var video = CreateVideo(
            dataOffset: 8,
            fourCc: "VP90",
            frames:
            [
                new WzImageVideoFrameInspection(0, 2, 4, -1, 0, 0, 0)
            ]);

        var result = service.DecodeFrame(new MemoryStream(new byte[10]), video, 0);

        Assert.False(result.Succeeded);
        Assert.Equal("wcx.video.packet.outOfBounds", result.Diagnostic?.Code);
    }

    [Fact]
    public void DecodeFrame_WhenHeaderIsMissing_ReturnsMetadataDiagnostic()
    {
        var service = new WzImageVideoFrameDecoder(new RecordingRawVideoPacketDecoder());
        var video = new WzImageVideoInspection(0, 0, 0, Header: null, HeaderError: "bad header");

        var result = service.DecodeFrame(new MemoryStream(), video, 0);

        Assert.False(result.Succeeded);
        Assert.Equal("wcx.video.metadata.invalid", result.Diagnostic?.Code);
        Assert.Contains("bad header", result.Diagnostic?.Message);
    }

    [Fact]
    public void Reset_ForwardsToRawDecoder()
    {
        var decoder = new RecordingRawVideoPacketDecoder();
        var service = new WzImageVideoFrameDecoder(decoder);

        service.Reset();

        Assert.True(decoder.WasReset);
    }

    [Fact]
    public void Vp9RawVideoPacketDecoder_WhenOutputFormatIsInvalid_ReturnsDiagnostic()
    {
        var decoder = new Vp9RawVideoPacketDecoder();

        var result = decoder.Decode(
            new byte[] { 0 },
            alphaPacket: null,
            expectedWidth: 1,
            expectedHeight: 1,
            new WzVideoDecodeOptions(OutputFormat: (WzVideoPixelFormat)99));

        Assert.False(result.Succeeded);
        Assert.Equal("wcx.video.options.invalid", result.Diagnostic?.Code);
    }

    [Fact]
    public void DecodeFrame_WithOptionalVp9Samples_DecodesMergedBgraFrame()
    {
        if (!File.Exists(MainPacketPath) || !File.Exists(AlphaPacketPath))
        {
            return;
        }

        var colorPacket = File.ReadAllBytes(MainPacketPath);
        var alphaPacket = File.ReadAllBytes(AlphaPacketPath);
        var imagePayload = new byte[16 + colorPacket.Length + 8 + alphaPacket.Length];
        colorPacket.CopyTo(imagePayload.AsSpan(16));
        alphaPacket.CopyTo(imagePayload.AsSpan(16 + colorPacket.Length + 8));
        var video = CreateVideo(
            dataOffset: 4,
            fourCc: "VP90",
            width: 2656,
            height: 1352,
            frames:
            [
                new WzImageVideoFrameInspection(
                    0,
                    DataOffset: 12,
                    DataLength: colorPacket.Length,
                    AlphaDataOffset: 12 + colorPacket.Length + 8,
                    AlphaDataLength: alphaPacket.Length,
                    DelayInNanoseconds: 1000000,
                    StartTimeInNanoseconds: 0)
            ]);
        var service = new WzImageVideoFrameDecoder();

        var result = service.DecodeFrame(new MemoryStream(imagePayload), video, 0);

        Assert.True(result.Succeeded, result.Diagnostic?.Message);
        Assert.NotNull(result.Frame);
        Assert.Equal(2656, result.Frame.Width);
        Assert.Equal(1352, result.Frame.Height);
        Assert.Equal(WzVideoPixelFormat.Bgra8888, result.Frame.PixelFormat);
        Assert.Equal(2656 * 1352 * 4, result.Frame.Pixels.Length);
        Assert.Equal(MergedFrameHash, Hash(result.Frame.Pixels));
    }

    private static WzImageVideoInspection CreateVideo(
        long dataOffset,
        string fourCc,
        IReadOnlyList<WzImageVideoFrameInspection> frames,
        int width = 4,
        int height = 2)
    {
        return new WzImageVideoInspection(
            Unknown: 0,
            DataOffset: dataOffset,
            DataLength: 0,
            Header: new WzImageVideoHeaderInspection(
                Signature: "MCV0",
                HeaderLength: 36,
                FourCc: 0,
                FourCcText: fourCc,
                Width: width,
                Height: height,
                FrameCount: frames.Count,
                DataFlags: WzImageVideoDataFlags.Default,
                FrameDelayUnit: 1,
                DefaultDelay: 0,
                Frames: frames));
    }

    private static string Hash(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private sealed class RecordingRawVideoPacketDecoder : IWzRawVideoPacketDecoder
    {
        public byte[] ColorPacket { get; private set; } = [];

        public byte[]? AlphaPacket { get; private set; }

        public int ExpectedWidth { get; private set; }

        public int ExpectedHeight { get; private set; }

        public bool WasCalled { get; private set; }

        public bool WasReset { get; private set; }

        public WzRawVideoPacketDecodeResult Decode(
            ReadOnlyMemory<byte> colorPacket,
            ReadOnlyMemory<byte>? alphaPacket,
            int expectedWidth,
            int expectedHeight,
            WzVideoDecodeOptions options)
        {
            WasCalled = true;
            ColorPacket = colorPacket.ToArray();
            AlphaPacket = alphaPacket.HasValue
                ? alphaPacket.Value.ToArray()
                : null;
            ExpectedWidth = expectedWidth;
            ExpectedHeight = expectedHeight;
            return WzRawVideoPacketDecodeResult.Success(new WzRawDecodedVideoFrame(
                expectedWidth,
                expectedHeight,
                options.OutputFormat,
                [1, 2, 3, 255],
                expectedWidth * 4));
        }

        public void Reset()
        {
            WasReset = true;
        }
    }
}
