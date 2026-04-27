using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed record ResourceExportOptions(
    ResourceExportKind Kind = ResourceExportKind.Metadata,
    WzStringEncryptionKind? StringKey = WzStringEncryptionKind.None,
    int MaxPropertyDepth = WzImageInspectionReader.MaxPropertyInspectionDepth);
