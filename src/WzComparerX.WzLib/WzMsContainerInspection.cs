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
    long FileSize)
{
    public WzMsContainerEncryptionKind EncryptionKind { get; init; } = WzMsContainerEncryptionKind.Unknown;

    public string KeySalt { get; init; } = string.Empty;

    public string FileNameWithSalt { get; init; } = string.Empty;
}

public enum WzMsContainerEncryptionKind
{
    Unknown,
    Snow,
    ChaCha20
}

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
    int Unknown4)
{
    public IReadOnlyList<byte> Key { get; init; } = Array.Empty<byte>();
}
