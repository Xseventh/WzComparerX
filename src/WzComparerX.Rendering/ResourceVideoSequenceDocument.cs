namespace WzComparerX.Rendering;

public sealed record ResourceVideoSequenceDocument(
    string SourcePath,
    string Selector,
    string? ValuePath,
    string FourCc,
    int Width,
    int Height,
    WzVideoPixelFormat PixelFormat,
    IReadOnlyList<WzDecodedVideoFrame> Frames)
{
    public int FrameCount => Frames.Count;

    public long DurationInNanoseconds => Frames.Count == 0
        ? 0
        : Frames.Max(frame => frame.StartTimeInNanoseconds + frame.DelayInNanoseconds);

    public string Title => ValuePath is null
        ? $"{Selector} ({Width}x{Height}, {FrameCount} frames)"
        : $"{Selector}/{ValuePath} ({Width}x{Height}, {FrameCount} frames)";
}
