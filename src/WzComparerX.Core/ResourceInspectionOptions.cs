using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed record ResourceInspectionOptions(
    WzStringEncryptionKind? StringKey = WzStringEncryptionKind.None,
    int MaxPropertyDepth = WzImageInspectionReader.FullPropertyInspectionDepth,
    bool IncludeDebugMetadata = false);
