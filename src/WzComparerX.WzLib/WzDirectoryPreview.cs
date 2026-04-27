namespace WzComparerX.WzLib;

public sealed record WzDirectoryPreview(
    WzPackageHeader Header,
    int EntryCount,
    IReadOnlyList<WzDirectoryEntryPreview> Entries);

