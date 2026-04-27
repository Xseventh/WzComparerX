namespace WzComparerX.Core;

public sealed record ResourceInspectionDiagnostic(
    string Severity,
    string Message,
    string? Path = null,
    string? Code = null,
    string? Source = null);
