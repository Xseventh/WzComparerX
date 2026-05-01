namespace WzComparerX.Core;

public sealed record ResourceInspectionIdentity(
    string? PackagePath = null,
    string? ImageSelector = null,
    string? ValuePath = null,
    string? LinkedTarget = null);
