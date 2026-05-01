namespace WzComparerX.Core;

public sealed record ResourceInspectionIdentity(
    string? PackagePath = null,
    string? ImageSelector = null,
    string? ValuePath = null,
    string? LinkedTarget = null,
    ResourceInspectionResolvedLinkTarget? ResolvedLinkedTarget = null);

public sealed record ResourceInspectionResolvedLinkTarget(
    string PackagePath,
    string ImageSelector,
    string? ValuePath = null);
