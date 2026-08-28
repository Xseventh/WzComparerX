namespace WzComparerX.Core;

public sealed record ResourceSpineExportDocument(
    string SourcePath,
    string Selector,
    string ValuePath,
    string SpineName,
    string AtlasFileName,
    string SkeletonFileName,
    string? SpineVersion,
    IReadOnlyList<ResourceSpineExportPage> Pages,
    IReadOnlyList<ResourceExportFile> Files);
