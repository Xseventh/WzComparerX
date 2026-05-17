namespace WzComparerX.WzLib;

public sealed record WzImageCanvasBitmap(
    int Width,
    int Height,
    int Format,
    int Scale,
    byte[] Pixels);
