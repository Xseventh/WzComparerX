namespace WzComparerX.Core;

public sealed class ResourceVideoTargetException : Exception
{
    public ResourceVideoTargetException(ResourceInspectionDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }

    public ResourceInspectionDiagnostic Diagnostic { get; }
}
