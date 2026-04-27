namespace WzComparerX.Core;

public sealed record ResourceInspectionDocument(
    string SourcePath,
    string Format,
    ResourceInspectionNode Root,
    IReadOnlyList<ResourceInspectionMetadata>? DebugMetadata = null,
    IReadOnlyList<ResourceInspectionDiagnostic>? Diagnostics = null);
