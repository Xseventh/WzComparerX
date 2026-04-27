namespace WzComparerX.WzLib;

public sealed record WzImageCanvasPreview(
    int Width,
    int Height,
    int Format,
    int Scale,
    int Pages,
    int Unknown1,
    long DataOffset,
    int DataLength)
{
    public override string ToString()
    {
        return $"{Width}x{Height}, format={Format}, scale={Scale}, pages={Pages}, dataLength={DataLength}, dataOffset={DataOffset}";
    }
}
