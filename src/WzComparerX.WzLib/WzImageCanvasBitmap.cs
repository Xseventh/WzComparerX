namespace WzComparerX.WzLib;

public sealed record WzImageCanvasBitmap(
    int Width,
    int Height,
    int Format,
    byte[] Pixels);
