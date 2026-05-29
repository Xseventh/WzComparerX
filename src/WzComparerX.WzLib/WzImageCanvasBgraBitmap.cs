namespace WzComparerX.WzLib;

public sealed record WzImageCanvasBgraBitmap(
    int Width,
    int Height,
    int Format,
    string PixelFormat,
    byte[] Pixels)
{
    public int Stride => Width * 4;
}
