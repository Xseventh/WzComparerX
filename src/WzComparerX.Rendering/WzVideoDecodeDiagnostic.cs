namespace WzComparerX.Rendering;

public sealed record WzVideoDecodeDiagnostic(
    string Code,
    string Message)
{
    public static WzVideoDecodeDiagnostic InvalidMetadata(string message)
    {
        return new WzVideoDecodeDiagnostic("wcx.video.metadata.invalid", message);
    }

    public static WzVideoDecodeDiagnostic InvalidOptions(string message)
    {
        return new WzVideoDecodeDiagnostic("wcx.video.options.invalid", message);
    }

    public static WzVideoDecodeDiagnostic UnsupportedCodec(string codec)
    {
        return new WzVideoDecodeDiagnostic("wcx.video.codec.unsupported", $"Unsupported video codec: {codec}.");
    }

    public static WzVideoDecodeDiagnostic InvalidFrameIndex(int frameIndex)
    {
        return new WzVideoDecodeDiagnostic("wcx.video.frame.invalidIndex", $"Video frame index is outside the frame table: {frameIndex}.");
    }

    public static WzVideoDecodeDiagnostic NoDisplayFrame(int frameIndex)
    {
        return new WzVideoDecodeDiagnostic("wcx.video.frame.noDisplay", $"Video frame {frameIndex} updates decoder state but does not contain a display frame.");
    }

    public static WzVideoDecodeDiagnostic FrameDecodeFailed(int frameIndex, WzVideoDecodeDiagnostic diagnostic)
    {
        return new WzVideoDecodeDiagnostic(
            "wcx.video.frame.decodeFailed",
            $"Video frame {frameIndex} failed with {diagnostic.Code}: {diagnostic.Message}");
    }

    public static WzVideoDecodeDiagnostic PacketOutOfBounds(string packetName, long offset, int length, long streamLength)
    {
        return new WzVideoDecodeDiagnostic(
            "wcx.video.packet.outOfBounds",
            $"{packetName} packet is outside the image payload stream: offset={offset}, length={length}, streamLength={streamLength}.");
    }

    public static WzVideoDecodeDiagnostic AlphaDimensionMismatch(int colorWidth, int colorHeight, int alphaWidth, int alphaHeight)
    {
        return new WzVideoDecodeDiagnostic(
            "wcx.video.alpha.dimensionMismatch",
            $"Video alpha frame dimensions do not match color frame dimensions: color={colorWidth}x{colorHeight}, alpha={alphaWidth}x{alphaHeight}.");
    }

    public static WzVideoDecodeDiagnostic AlphaPixelFormatUnsupported(WzVideoPixelFormat colorFormat, WzVideoPixelFormat alphaFormat)
    {
        return new WzVideoDecodeDiagnostic(
            "wcx.video.alpha.pixelFormatUnsupported",
            $"Video alpha merge requires BGRA8888 color and alpha frames: color={colorFormat}, alpha={alphaFormat}.");
    }

    public static WzVideoDecodeDiagnostic DecoderFailure(string code, string message)
    {
        return new WzVideoDecodeDiagnostic($"wcx.video.decoder.{code}", message);
    }
}
