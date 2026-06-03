using WzComparerX.WzLib;

namespace WzComparerX.Rendering;

public sealed class WzImageVideoFrameDecoder
{
    private readonly Func<string, IWzRawVideoPacketDecoder> decoderFactory;
    private readonly bool useSharedDecoder;
    private IWzRawVideoPacketDecoder? decoder;
    private string? decoderCodec;

    public WzImageVideoFrameDecoder()
        : this(CreateDefaultDecoder)
    {
    }

    public WzImageVideoFrameDecoder(IWzRawVideoPacketDecoder decoder)
    {
        ArgumentNullException.ThrowIfNull(decoder);
        decoderFactory = _ => decoder;
        useSharedDecoder = true;
        this.decoder = decoder;
        decoderCodec = string.Empty;
    }

    public WzImageVideoFrameDecoder(Func<string, IWzRawVideoPacketDecoder> decoderFactory)
    {
        this.decoderFactory = decoderFactory ?? throw new ArgumentNullException(nameof(decoderFactory));
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

        if (decoder is null || (!useSharedDecoder && decoderCodec != header.FourCcText))
        {
            decoder = decoderFactory(header.FourCcText);
            decoderCodec = header.FourCcText;
        }

        var rawResult = decoder.Decode(
            colorPacket.Packet,
            alphaPacket,
            header.Width,
            header.Height,
            options);
        if (!rawResult.Succeeded)
        {
            return WzVideoDecodeResult.Fail(rawResult.Diagnostic!);
        }

        if (rawResult.NoDisplayFrame)
        {
            return WzVideoDecodeResult.Fail(WzVideoDecodeDiagnostic.NoDisplayFrame(frame.Index));
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
        decoder?.Reset();
    }

    private static IWzRawVideoPacketDecoder CreateDefaultDecoder(string fourCc)
    {
        return fourCc switch
        {
            "VP90" => new Vp9RawVideoPacketDecoder(),
            "VP80" => new Vp8RawVideoPacketDecoder(),
            _ => throw new ArgumentOutOfRangeException(nameof(fourCc), fourCc, "Unsupported video codec.")
        };
    }
}
