namespace WzComparerX.Core;

public sealed record ResourceExportFile(
    string RelativePath,
    string ContentType,
    byte[] Content);
