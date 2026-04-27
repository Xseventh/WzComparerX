namespace WzComparerX.WzLib;

public sealed record WzImageInspection(
    WzPackageHeader Header,
    string Selector,
    WzDirectoryEntryInspection? Entry,
    string? ObjectType,
    int? PropertyCount = null,
    IReadOnlyList<WzImagePropertyInspectionEntry>? Properties = null,
    object? ObjectValue = null)
{
    public bool IsValid => Header.IsValid && Entry is not null && ObjectType is not null;
}
