using WzComparerX.WzLib;

namespace WzComparerX.Core;

internal sealed record WzImageInspectionContext(
    WzDirectoryInspection DirectoryInspection,
    WzImageInspection ImageInspection,
    WzStringEncryptionKind StringKey);
