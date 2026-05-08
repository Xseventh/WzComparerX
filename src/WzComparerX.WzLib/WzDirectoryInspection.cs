namespace WzComparerX.WzLib;

public sealed record WzDirectoryInspection(
    WzPackageHeader Header,
    int EntryCount,
    IReadOnlyList<WzDirectoryEntryInspection> Entries,
    int? WzVersion = null,
    uint? HashVersion = null,
    WzStringEncryptionKind? StringEncryptionKind = null,
    string? FormatProfile = null);
