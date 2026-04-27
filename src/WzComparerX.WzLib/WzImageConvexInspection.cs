namespace WzComparerX.WzLib;

public sealed record WzImageConvexInspection(IReadOnlyList<WzImageVectorInspection> Points)
{
    public override string ToString()
    {
        return $"points={Points.Count} [{string.Join(", ", Points)}]";
    }
}
