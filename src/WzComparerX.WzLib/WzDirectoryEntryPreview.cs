namespace WzComparerX.WzLib;

public sealed record WzDirectoryEntryPreview(
    int Index,
    byte NodeType,
    WzDirectoryEntryKind Kind,
    int DataSize,
    int Checksum,
    long HashOffsetPosition,
    uint HashOffset);

