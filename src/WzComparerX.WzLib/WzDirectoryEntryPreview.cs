namespace WzComparerX.WzLib;

public sealed record WzDirectoryEntryPreview(
    int Index,
    byte NodeType,
    WzDirectoryEntryKind Kind,
    string? Name,
    int DataSize,
    int Checksum,
    long HashOffsetPosition,
    uint HashOffset);
