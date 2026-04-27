namespace WzComparerX.Core;

public sealed record ResourceInspectionNode(
    string Name,
    string Kind,
    string? Path = null,
    string? DisplayValue = null,
    IReadOnlyList<ResourceInspectionNode>? Children = null,
    IReadOnlyList<ResourceInspectionMetadata>? DebugMetadata = null,
    IReadOnlyList<ResourceInspectionDiagnostic>? Diagnostics = null)
{
    public IReadOnlyList<ResourceInspectionNode> Children { get; init; } = Children ?? Array.Empty<ResourceInspectionNode>();
}
