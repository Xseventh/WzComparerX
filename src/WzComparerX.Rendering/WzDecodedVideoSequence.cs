namespace WzComparerX.Rendering;

public sealed record WzDecodedVideoSequence(
    int Width,
    int Height,
    WzVideoPixelFormat PixelFormat,
    IReadOnlyList<WzDecodedVideoFrame> Frames)
{
    public long DurationInNanoseconds => Frames.Count == 0
        ? 0
        : Frames.Max(frame => frame.StartTimeInNanoseconds + frame.DelayInNanoseconds);
}
