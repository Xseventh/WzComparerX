namespace WzComparerX.Core;

public sealed record ResourceInspectionDiagnostic(
    string Severity,
    string Message,
    string? Path = null);
