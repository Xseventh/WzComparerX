namespace WzComparerX.Core;

public sealed class ResourceInspectionException : Exception
{
    public ResourceInspectionException(ResourceInspectionDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }

    public ResourceInspectionDiagnostic Diagnostic { get; }
}
