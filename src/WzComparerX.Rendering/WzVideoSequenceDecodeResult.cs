namespace WzComparerX.Rendering;

public sealed record WzVideoSequenceDecodeResult(
    WzDecodedVideoSequence? Sequence,
    WzVideoDecodeDiagnostic? Diagnostic)
{
    public bool Succeeded => Sequence is not null && Diagnostic is null;

    public static WzVideoSequenceDecodeResult Success(WzDecodedVideoSequence sequence)
    {
        return new WzVideoSequenceDecodeResult(sequence, null);
    }

    public static WzVideoSequenceDecodeResult Fail(WzVideoDecodeDiagnostic diagnostic)
    {
        return new WzVideoSequenceDecodeResult(null, diagnostic);
    }
}
