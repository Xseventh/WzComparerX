namespace WzComparerX.Core;

public sealed record ResourceExportDocument(
    string SourcePath,
    ResourceExportKind Kind,
    string ContentType,
    string Content);
