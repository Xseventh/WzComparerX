namespace WzComparerX.WzLib;

public sealed record WzMsContainerInspection(
    WzMsContainerHeaderInspection Header,
    IReadOnlyList<WzMsContainerEntryInspection> Entries);

public sealed record WzMsContainerHeaderInspection(
    string SourcePath,
    int Version,
    int HeaderHash,
    int EntryCount,
    long HeaderStartPosition,
    long EntryStartPosition,
    long DataStartPosition,
    int RandomByteCount,
    int SaltLength,
    long FileSize);

public sealed record WzMsContainerEntryInspection(
    int Index,
    string Name,
    string Path,
    int Checksum,
    int Flags,
    int RelativeBlock,
    long Offset,
    int Size,
    int SizeAligned,
    int Unknown1,
    int Unknown2,
    int Unknown3,
    int Unknown4);
