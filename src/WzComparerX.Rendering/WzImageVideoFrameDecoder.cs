using WzComparerX.WzLib;

namespace WzComparerX.Rendering;

public sealed class WzImageVideoFrameDecoder
{
    private readonly IWzRawVideoPacketDecoder vp9Decoder;

    public WzImageVideoFrameDecoder(IWzRawVideoPacketDecoder? vp9Decoder = null)
    {
        this.vp9Decoder = vp9Decoder ?? new Vp9RawVideoPacketDecoder();
    }

    public WzVideoDecodeResult DecodeFrame(
        Stream imagePayloadStream,
        WzImageVideoInspection video,
        int frameIndex,
        WzVideoDecodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(imagePayloadStream);
        ArgumentNullException.ThrowIfNull(video);
        options ??= WzVideoDecodeOptions.Default;

        if (!imagePayloadStream.CanRead || !imagePayloadStream.CanSeek)
        {
            return WzVideoDecodeResult.Fail(WzVideoDecodeDiagnostic.InvalidMetadata("Video frame decode requires a readable and seekable image payload stream."));
        }

        if (video.Header is null)
        {
            var message = video.HeaderError is null
                ? "Video metadata does not include a readable MCV header."
                : $"Video metadata does not include a readable MCV header: {video.HeaderError}";
            return WzVideoDecodeResult.Fail(WzVideoDecodeDiagnostic.InvalidMetadata(message));
        }

        if (video.Header.FourCcText != "VP90")
        {
            return WzVideoDecodeResult.Fail(WzVideoDecodeDiagnostic.UnsupportedCodec(video.Header.FourCcText));
        }

        if (frameIndex < 0 || frameIndex >= video.Header.Frames.Count)
        {
            return WzVideoDecodeResult.Fail(WzVideoDecodeDiagnostic.InvalidFrameIndex(frameIndex));
        }

        var frame = video.Header.Frames[frameIndex];
        var colorPacket = ReadPacket(imagePayloadStream, video, frame.DataOffset, frame.DataLength, "Color");
        if (colorPacket.Diagnostic is not null)
        {
            return WzVideoDecodeResult.Fail(colorPacket.Diagnostic);
        }

        ReadOnlyMemory<byte>? alphaPacket = null;
        if (frame.AlphaDataOffset >= 0 && frame.AlphaDataLength > 0)
        {
            var alpha = ReadPacket(imagePayloadStream, video, frame.AlphaDataOffset, frame.AlphaDataLength, "Alpha");
            if (alpha.Diagnostic is not null)
            {
                return WzVideoDecodeResult.Fail(alpha.Diagnostic);
            }

            alphaPacket = alpha.Packet;
        }

        var rawResult = vp9Decoder.Decode(
            colorPacket.Packet,
            alphaPacket,
            video.Header.Width,
            video.Header.Height,
            options);
        if (!rawResult.Succeeded)
        {
            return WzVideoDecodeResult.Fail(rawResult.Diagnostic!);
        }

        var rawFrame = rawResult.Frame!;
        return WzVideoDecodeResult.Success(new WzDecodedVideoFrame(
            rawFrame.Width,
            rawFrame.Height,
            rawFrame.PixelFormat,
            rawFrame.Pixels,
            rawFrame.Stride,
            frame.Index,
            frame.StartTimeInNanoseconds,
            frame.DelayInNanoseconds));
    }

    public void Reset()
    {
        vp9Decoder.Reset();
    }

    private static PacketReadResult ReadPacket(
        Stream imagePayloadStream,
        WzImageVideoInspection video,
        long packetOffsetInVideoPayload,
        int packetLength,
        string packetName)
    {
        var absoluteOffset = video.DataOffset + packetOffsetInVideoPayload;
        if (packetLength < 0 ||
            absoluteOffset < 0 ||
            absoluteOffset > imagePayloadStream.Length ||
            packetLength > imagePayloadStream.Length - absoluteOffset)
        {
            return PacketReadResult.Fail(WzVideoDecodeDiagnostic.PacketOutOfBounds(
                packetName,
                absoluteOffset,
                packetLength,
                imagePayloadStream.Length));
        }

        var packet = new byte[packetLength];
        imagePayloadStream.Position = absoluteOffset;
        imagePayloadStream.ReadExactly(packet);
        return PacketReadResult.Success(packet);
    }

    private sealed record PacketReadResult(
        ReadOnlyMemory<byte> Packet,
        WzVideoDecodeDiagnostic? Diagnostic)
    {
        public static PacketReadResult Success(byte[] packet)
        {
            return new PacketReadResult(packet, null);
        }

        public static PacketReadResult Fail(WzVideoDecodeDiagnostic diagnostic)
        {
            return new PacketReadResult(ReadOnlyMemory<byte>.Empty, diagnostic);
        }
    }
}
