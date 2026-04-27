namespace WzComparerX.WzLib;

public sealed record WzImageVectorInspection(int X, int Y)
{
    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}
