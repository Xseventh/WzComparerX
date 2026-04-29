namespace WzComparerX.Core;

public sealed class ResourceCanvasImageException : Exception
{
    public ResourceCanvasImageException(ResourceInspectionDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }

    public ResourceInspectionDiagnostic Diagnostic { get; }
}
