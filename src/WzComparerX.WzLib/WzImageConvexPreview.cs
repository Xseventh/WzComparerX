namespace WzComparerX.WzLib;

public sealed record WzImageConvexPreview(IReadOnlyList<WzImageVectorPreview> Points)
{
    public override string ToString()
    {
        return $"points={Points.Count} [{string.Join(", ", Points)}]";
    }
}
