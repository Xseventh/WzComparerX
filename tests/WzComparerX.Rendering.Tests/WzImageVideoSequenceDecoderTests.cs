using System.Security.Cryptography;
using WzComparerX.Rendering;
using WzComparerX.WzLib;

namespace WzComparerX.Rendering.Tests;

public sealed class WzImageVideoSequenceDecoderTests
{
    private const string MainPacketPath = "/tmp/vp9-main-frame-0.vp9";
    private const string AlphaPacketPath = "/tmp/vp9-alpha-frame-0.vp9";
    private const string MergedFrameHash = "c8095ee5e4b760a8a6f7c18d10b357b9f579c6864bb1cd815061d8d6e930a2ff";

    [Fact]
    public void DecodeSequence_DecodesAllFramesInOrderWithSeparateAlphaState()
    {
        var imagePayload = new byte[16];
        imagePayload[5] = 1;
        imagePayload[6] = 1;
        imagePayload[7] = 2;
        imagePayload[8] = 2;
        imagePayload[9] = 9;
        imagePayload[10] = 8;
        var colorDecoder = new QueueRawVideoPacketDecoder(
            RawBgraFrame([10, 20, 30, 255, 40, 50, 60, 255], width: 2, height: 1),
            RawBgraFrame([11, 21, 31, 255, 41, 51, 61, 255], width: 2, height: 1));
        var alphaDecoder = new QueueRawVideoPacketDecoder(
            RawBgraFrame([0, 0, 7, 255, 0, 0, 8, 255], width: 2, height: 1),
            RawBgraFrame([0, 0, 17, 255, 0, 0, 18, 255], width: 2, height: 1));
        var decoders = new Queue<IWzRawVideoPacketDecoder>([colorDecoder, alphaDecoder]);
        var service = new WzImageVideoSequenceDecoder(() => decoders.Dequeue());
        var video = CreateVideo(
            dataOffset: 5,
            fourCc: "VP90",
            flags: WzImageVideoDataFlags.AlphaMap,
            frames:
            [
                new WzImageVideoFrameInspection(0, 0, 2, 4, 1, 10, 0),
                new WzImageVideoFrameInspection(1, 2, 2, 5, 1, 20, 10)
            ],
            width: 2,
            height: 1);

        var result = service.DecodeSequence(
            new MemoryStream(imagePayload),
            video,
            new WzVideoDecodeOptions(OutputFormat: WzVideoPixelFormat.Rgba8888));

        Assert.True(result.Succeeded, result.Diagnostic?.Message);
        Assert.Empty(decoders);
        Assert.Collection(
            colorDecoder.Packets,
            packet => Assert.Equal([1, 1], packet),
            packet => Assert.Equal([2, 2], packet));
        Assert.Collection(
            alphaDecoder.Packets,
            packet => Assert.Equal([9], packet),
            packet => Assert.Equal([8], packet));
        Assert.All(colorDecoder.Options, option => Assert.Equal(WzVideoPixelFormat.Bgra8888, option.OutputFormat));
        Assert.All(alphaDecoder.Options, option => Assert.Equal(WzVideoPixelFormat.Bgra8888, option.OutputFormat));
        Assert.NotNull(result.Sequence);
        var sequence = result.Sequence;
        Assert.Equal(2, sequence.Width);
        Assert.Equal(1, sequence.Height);
        Assert.Equal(WzVideoPixelFormat.Rgba8888, sequence.PixelFormat);
        Assert.Equal(30, sequence.DurationInNanoseconds);
        Assert.Equal(2, sequence.Frames.Count);
        Assert.Equal([30, 20, 10, 7, 60, 50, 40, 8], sequence.Frames[0].Pixels);
        Assert.Equal([31, 21, 11, 17, 61, 51, 41, 18], sequence.Frames[1].Pixels);
        Assert.Equal(0, sequence.Frames[0].FrameIndex);
        Assert.Equal(1, sequence.Frames[1].FrameIndex);
    }

    [Fact]
    public void DecodeSequence_WhenAlphaIsAbsent_UsesOnlyColorDecoder()
    {
        var imagePayload = new byte[8];
        imagePayload[2] = 1;
        imagePayload[3] = 2;
        var colorDecoder = new QueueRawVideoPacketDecoder(
            RawBgraFrame([1, 2, 3, 255]),
            RawBgraFrame([4, 5, 6, 255]));
        var factoryCallCount = 0;
        var service = new WzImageVideoSequenceDecoder(() =>
        {
            factoryCallCount++;
            return colorDecoder;
        });
        var video = CreateVideo(
            dataOffset: 2,
            fourCc: "VP90",
            flags: WzImageVideoDataFlags.Default,
            frames:
            [
                new WzImageVideoFrameInspection(0, 0, 1, -1, 0, 10, 0),
                new WzImageVideoFrameInspection(1, 1, 1, -1, 0, 10, 10)
            ]);

        var result = service.DecodeSequence(new MemoryStream(imagePayload), video);

        Assert.True(result.Succeeded, result.Diagnostic?.Message);
        Assert.Equal(1, factoryCallCount);
        Assert.Collection(
            colorDecoder.Packets,
            packet => Assert.Equal([1], packet),
            packet => Assert.Equal([2], packet));
        Assert.NotNull(result.Sequence);
        var sequence = result.Sequence;
        Assert.Equal(2, sequence.Frames.Count);
        Assert.Equal(WzVideoPixelFormat.Bgra8888, sequence.PixelFormat);
    }

    [Fact]
    public void DecodeSequence_WhenCodecIsUnsupported_ReturnsDiagnosticWithoutCreatingDecoders()
    {
        var factoryCallCount = 0;
        var service = new WzImageVideoSequenceDecoder(() =>
        {
            factoryCallCount++;
            return new QueueRawVideoPacketDecoder();
        });
        var video = CreateVideo(
            dataOffset: 0,
            fourCc: "VP80",
            flags: WzImageVideoDataFlags.Default,
            frames:
            [
                new WzImageVideoFrameInspection(0, 0, 1, -1, 0, 0, 0)
            ]);

        var result = service.DecodeSequence(new MemoryStream([0]), video);

        Assert.False(result.Succeeded);
        Assert.Equal("wcx.video.codec.unsupported", result.Diagnostic?.Code);
        Assert.Equal(0, factoryCallCount);
    }

    [Fact]
    public void DecodeSequence_WhenColorDecodeFails_ReturnsDiagnosticAndStops()
    {
        var colorDecoder = new QueueRawVideoPacketDecoder(
            WzRawVideoPacketDecodeResult.Fail(WzVideoDecodeDiagnostic.DecoderFailure("color", "color failed")));
        var alphaDecoder = new QueueRawVideoPacketDecoder(RawBgraFrame([0, 0, 1, 255]));
        var decoders = new Queue<IWzRawVideoPacketDecoder>([colorDecoder, alphaDecoder]);
        var service = new WzImageVideoSequenceDecoder(() => decoders.Dequeue());
        var video = CreateVideo(
            dataOffset: 0,
            fourCc: "VP90",
            flags: WzImageVideoDataFlags.AlphaMap,
            frames:
            [
                new WzImageVideoFrameInspection(0, 0, 1, 1, 1, 0, 0)
            ]);

        var result = service.DecodeSequence(new MemoryStream([1, 2]), video);

        Assert.False(result.Succeeded);
        Assert.Equal("wcx.video.frame.decodeFailed", result.Diagnostic?.Code);
        Assert.Contains("Video frame 0 failed with wcx.video.decoder.color", result.Diagnostic?.Message);
        Assert.Single(colorDecoder.Packets);
        Assert.Empty(alphaDecoder.Packets);
    }

    [Fact]
    public void DecodeSequence_WhenAlphaDimensionsDiffer_ReturnsDiagnostic()
    {
        var colorDecoder = new QueueRawVideoPacketDecoder(RawBgraFrame([1, 2, 3, 255, 4, 5, 6, 255], width: 2, height: 1));
        var alphaDecoder = new QueueRawVideoPacketDecoder(RawBgraFrame([0, 0, 7, 255], width: 1, height: 1));
        var decoders = new Queue<IWzRawVideoPacketDecoder>([colorDecoder, alphaDecoder]);
        var service = new WzImageVideoSequenceDecoder(() => decoders.Dequeue());
        var video = CreateVideo(
            dataOffset: 0,
            fourCc: "VP90",
            flags: WzImageVideoDataFlags.AlphaMap,
            frames:
            [
                new WzImageVideoFrameInspection(0, 0, 1, 1, 1, 0, 0)
            ],
            width: 2,
            height: 1);

        var result = service.DecodeSequence(new MemoryStream([1, 2]), video);

        Assert.False(result.Succeeded);
        Assert.Equal("wcx.video.alpha.dimensionMismatch", result.Diagnostic?.Code);
    }

    [Fact]
    public void DecodeSequence_WhenOutputFormatIsInvalid_ReturnsDiagnostic()
    {
        var service = new WzImageVideoSequenceDecoder();
        var video = CreateVideo(
            dataOffset: 0,
            fourCc: "VP90",
            flags: WzImageVideoDataFlags.Default,
            frames:
            [
                new WzImageVideoFrameInspection(0, 0, 1, -1, 0, 0, 0)
            ]);

        var result = service.DecodeSequence(
            new MemoryStream([0]),
            video,
            new WzVideoDecodeOptions(OutputFormat: (WzVideoPixelFormat)99));

        Assert.False(result.Succeeded);
        Assert.Equal("wcx.video.options.invalid", result.Diagnostic?.Code);
    }

    [Fact]
    public void DecodeSequence_WithOptionalVp9Samples_DecodesMergedBgraFrame()
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
            flags: WzImageVideoDataFlags.AlphaMap,
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
        var service = new WzImageVideoSequenceDecoder();

        var result = service.DecodeSequence(new MemoryStream(imagePayload), video);

        Assert.True(result.Succeeded, result.Diagnostic?.Message);
        Assert.NotNull(result.Sequence);
        var sequence = result.Sequence;
        var frame = Assert.Single(sequence.Frames);
        Assert.Equal(2656, frame.Width);
        Assert.Equal(1352, frame.Height);
        Assert.Equal(WzVideoPixelFormat.Bgra8888, frame.PixelFormat);
        Assert.Equal(2656 * 1352 * 4, frame.Pixels.Length);
        Assert.Equal(MergedFrameHash, Hash(frame.Pixels));
    }

    private static WzImageVideoInspection CreateVideo(
        long dataOffset,
        string fourCc,
        WzImageVideoDataFlags flags,
        IReadOnlyList<WzImageVideoFrameInspection> frames,
        int width = 1,
        int height = 1)
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
                DataFlags: flags,
                FrameDelayUnit: 1,
                DefaultDelay: 0,
                Frames: frames));
    }

    private static WzRawVideoPacketDecodeResult RawBgraFrame(byte[] pixels, int width = 1, int height = 1)
    {
        return WzRawVideoPacketDecodeResult.Success(new WzRawDecodedVideoFrame(
            width,
            height,
            WzVideoPixelFormat.Bgra8888,
            pixels,
            width * 4));
    }

    private static string Hash(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private sealed class QueueRawVideoPacketDecoder : IWzRawVideoPacketDecoder
    {
        private readonly Queue<WzRawVideoPacketDecodeResult> results;

        public QueueRawVideoPacketDecoder(params WzRawVideoPacketDecodeResult[] results)
        {
            this.results = new Queue<WzRawVideoPacketDecodeResult>(results);
        }

        public List<byte[]> Packets { get; } = [];

        public List<WzVideoDecodeOptions> Options { get; } = [];

        public WzRawVideoPacketDecodeResult Decode(
            ReadOnlyMemory<byte> colorPacket,
            ReadOnlyMemory<byte>? alphaPacket,
            int expectedWidth,
            int expectedHeight,
            WzVideoDecodeOptions options)
        {
            Assert.Null(alphaPacket);
            Packets.Add(colorPacket.ToArray());
            Options.Add(options);
            return results.Count == 0
                ? WzRawVideoPacketDecodeResult.Fail(WzVideoDecodeDiagnostic.DecoderFailure("missingTestResult", "Test decoder has no queued result."))
                : results.Dequeue();
        }

        public void Reset()
        {
        }
    }
}
