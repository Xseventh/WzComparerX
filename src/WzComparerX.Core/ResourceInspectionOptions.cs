using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed record ResourceInspectionOptions(
    WzStringEncryptionKind? StringKey = WzStringEncryptionKind.None,
    int MaxPropertyDepth = 1,
    bool IncludeDebugMetadata = false);
