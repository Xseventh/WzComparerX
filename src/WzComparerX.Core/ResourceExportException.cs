namespace WzComparerX.Core;

public sealed class ResourceExportException : Exception
{
    public ResourceExportException(ResourceInspectionDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }

    public ResourceInspectionDiagnostic Diagnostic { get; }
}
