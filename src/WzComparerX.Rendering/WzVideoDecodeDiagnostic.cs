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

    public static WzVideoDecodeDiagnostic PacketOutOfBounds(string packetName, long offset, int length, long streamLength)
    {
        return new WzVideoDecodeDiagnostic(
            "wcx.video.packet.outOfBounds",
            $"{packetName} packet is outside the image payload stream: offset={offset}, length={length}, streamLength={streamLength}.");
    }

    public static WzVideoDecodeDiagnostic DecoderFailure(string code, string message)
    {
        return new WzVideoDecodeDiagnostic($"wcx.video.decoder.{code}", message);
    }
}
