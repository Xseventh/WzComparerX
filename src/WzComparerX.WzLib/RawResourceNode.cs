namespace WzComparerX.WzLib;

public sealed record RawResourceNode(
    string Name,
    RawResourceNodeKind Kind,
    IReadOnlyList<RawResourceNode> Children,
    string? ValueKind = null,
    string? Value = null)
{
    public bool HasChildren => Children.Count > 0;
}

