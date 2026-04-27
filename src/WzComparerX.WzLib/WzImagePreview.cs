namespace WzComparerX.WzLib;

public sealed record WzImagePreview(
    WzPackageHeader Header,
    string Selector,
    WzDirectoryEntryPreview? Entry,
    string? ObjectType,
    int? PropertyCount = null,
    IReadOnlyList<WzImagePropertyPreviewEntry>? Properties = null,
    object? ObjectValue = null)
{
    public bool IsValid => Header.IsValid && Entry is not null && ObjectType is not null;
}
