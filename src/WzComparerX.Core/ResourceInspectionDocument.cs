namespace WzComparerX.Core;

public sealed record ResourceInspectionDocument(
    string SourcePath,
    string Format,
    ResourceInspectionNode Root);
