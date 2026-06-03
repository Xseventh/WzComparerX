namespace WzComparerX.Rendering;

public sealed record WzVideoDecodeResult(
    WzDecodedVideoFrame? Frame,
    WzVideoDecodeDiagnostic? Diagnostic)
{
    public bool Succeeded => Frame is not null && Diagnostic is null;

    public static WzVideoDecodeResult Success(WzDecodedVideoFrame frame)
    {
        return new WzVideoDecodeResult(frame, null);
    }

    public static WzVideoDecodeResult Fail(WzVideoDecodeDiagnostic diagnostic)
    {
        return new WzVideoDecodeResult(null, diagnostic);
    }
}
