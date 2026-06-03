namespace WzComparerX.Rendering;

public sealed record WzVideoDecodeOptions(
    WzVideoPixelFormat OutputFormat = WzVideoPixelFormat.Bgra8888,
    int MaxWidth = 16_384,
    int MaxHeight = 16_384,
    long MaxPixelCount = 268_435_456)
{
    public static WzVideoDecodeOptions Default { get; } = new();
}
