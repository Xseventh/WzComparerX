namespace WzComparerX.Rendering;

public interface IWzRawVideoPacketDecoder
{
    WzRawVideoPacketDecodeResult Decode(
        ReadOnlyMemory<byte> colorPacket,
        ReadOnlyMemory<byte>? alphaPacket,
        int expectedWidth,
        int expectedHeight,
        WzVideoDecodeOptions options);

    void Reset();
}

public sealed record WzRawVideoPacketDecodeResult(
    WzRawDecodedVideoFrame? Frame,
    WzVideoDecodeDiagnostic? Diagnostic)
{
    public bool Succeeded => Frame is not null && Diagnostic is null;

    public static WzRawVideoPacketDecodeResult Success(WzRawDecodedVideoFrame frame)
    {
        return new WzRawVideoPacketDecodeResult(frame, null);
    }

    public static WzRawVideoPacketDecodeResult Fail(WzVideoDecodeDiagnostic diagnostic)
    {
        return new WzRawVideoPacketDecodeResult(null, diagnostic);
    }
}

public sealed record WzRawDecodedVideoFrame(
    int Width,
    int Height,
    WzVideoPixelFormat PixelFormat,
    byte[] Pixels,
    int Stride);
