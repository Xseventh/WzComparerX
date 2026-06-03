namespace WzComparerX.Rendering;

public sealed class ResourceVideoSequenceException : Exception
{
    public ResourceVideoSequenceException(WzVideoDecodeDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }

    public WzVideoDecodeDiagnostic Diagnostic { get; }
}
