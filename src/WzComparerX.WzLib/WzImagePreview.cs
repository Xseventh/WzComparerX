namespace WzComparerX.WzLib;

public sealed record WzImagePreview(
    WzPackageHeader Header,
    string Selector,
    WzDirectoryEntryPreview? Entry,
    string? ObjectType)
{
    public bool IsValid => Header.IsValid && Entry is not null && ObjectType is not null;
}
