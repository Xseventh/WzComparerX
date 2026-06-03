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

        var diagnostic = WzImageVideoDecodePrimitives.ValidateVideo(imagePayloadStream, video, out var header);
        if (diagnostic is not null)
        {
            return WzVideoDecodeResult.Fail(diagnostic);
        }

        if (frameIndex < 0 || frameIndex >= header!.Frames.Count)
        {
            return WzVideoDecodeResult.Fail(WzVideoDecodeDiagnostic.InvalidFrameIndex(frameIndex));
        }

        var frame = header.Frames[frameIndex];
        var colorPacket = WzImageVideoDecodePrimitives.ReadPacket(imagePayloadStream, video, frame.DataOffset, frame.DataLength, "Color");
        if (colorPacket.Diagnostic is not null)
        {
            return WzVideoDecodeResult.Fail(colorPacket.Diagnostic);
        }

        ReadOnlyMemory<byte>? alphaPacket = null;
        if (frame.AlphaDataOffset >= 0 && frame.AlphaDataLength > 0)
        {
            var alpha = WzImageVideoDecodePrimitives.ReadPacket(imagePayloadStream, video, frame.AlphaDataOffset, frame.AlphaDataLength, "Alpha");
            if (alpha.Diagnostic is not null)
            {
                return WzVideoDecodeResult.Fail(alpha.Diagnostic);
            }

            alphaPacket = alpha.Packet;
        }

        var rawResult = vp9Decoder.Decode(
            colorPacket.Packet,
            alphaPacket,
            header.Width,
            header.Height,
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
}
