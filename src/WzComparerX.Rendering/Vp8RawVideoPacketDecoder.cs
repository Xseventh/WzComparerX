using VPDecoder;

namespace WzComparerX.Rendering;

public sealed class Vp8RawVideoPacketDecoder : IWzRawVideoPacketDecoder
{
    private readonly RawVp8Decoder decoder = new();

    public WzRawVideoPacketDecodeResult Decode(
        ReadOnlyMemory<byte> colorPacket,
        ReadOnlyMemory<byte>? alphaPacket,
        int expectedWidth,
        int expectedHeight,
        WzVideoDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Enum.IsDefined(options.OutputFormat))
        {
            return WzRawVideoPacketDecodeResult.Fail(
                WzVideoDecodeDiagnostic.InvalidOptions($"Unsupported WCX video output pixel format value {options.OutputFormat}."));
        }

        if (alphaPacket is { Length: > 0 })
        {
            return WzRawVideoPacketDecodeResult.Fail(
                WzVideoDecodeDiagnostic.DecoderFailure(
                    "vp8AlphaUnsupported",
                    "VP8 alpha packet composition is not supported by the raw decoder adapter yet."));
        }

        var vp8Options = new Vp8DecodeOptions(
            expectedWidth,
            expectedHeight,
            MapOutputFormat(options.OutputFormat),
            options.MaxWidth,
            options.MaxHeight,
            options.MaxPixelCount);

        var result = decoder.DecodeFrame(colorPacket, vp8Options);
        if (result.NoDisplayFrame)
        {
            return WzRawVideoPacketDecodeResult.NoDisplay();
        }

        if (!result.Succeeded)
        {
            var diagnostic = result.Diagnostic is null
                ? WzVideoDecodeDiagnostic.DecoderFailure("internalDecodeFailure", "VP8 decode failed without a diagnostic.")
                : WzVideoDecodeDiagnostic.DecoderFailure(result.Diagnostic.Code.ToString(), result.Diagnostic.Message);
            return WzRawVideoPacketDecodeResult.Fail(diagnostic);
        }

        var frame = result.Frame!;
        return WzRawVideoPacketDecodeResult.Success(new WzRawDecodedVideoFrame(
            frame.Width,
            frame.Height,
            MapOutputFormat(frame.PixelFormat),
            frame.Pixels,
            frame.Stride));
    }

    public void Reset()
    {
        decoder.Reset();
    }

    private static Vp9OutputPixelFormat MapOutputFormat(WzVideoPixelFormat format)
    {
        return format switch
        {
            WzVideoPixelFormat.Bgra8888 => Vp9OutputPixelFormat.Bgra8888,
            WzVideoPixelFormat.Rgba8888 => Vp9OutputPixelFormat.Rgba8888,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported WCX video output pixel format.")
        };
    }

    private static WzVideoPixelFormat MapOutputFormat(Vp9OutputPixelFormat format)
    {
        return format switch
        {
            Vp9OutputPixelFormat.Bgra8888 => WzVideoPixelFormat.Bgra8888,
            Vp9OutputPixelFormat.Rgba8888 => WzVideoPixelFormat.Rgba8888,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported VP8 output pixel format for WCX video frames.")
        };
    }
}
