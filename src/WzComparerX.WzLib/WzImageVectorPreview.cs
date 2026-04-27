namespace WzComparerX.WzLib;

public sealed record WzImageVectorPreview(int X, int Y)
{
    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}
