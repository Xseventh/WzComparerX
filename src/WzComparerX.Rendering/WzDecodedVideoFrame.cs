namespace WzComparerX.Rendering;

public sealed record WzDecodedVideoFrame(
    int Width,
    int Height,
    WzVideoPixelFormat PixelFormat,
    byte[] Pixels,
    int Stride,
    int FrameIndex,
    long StartTimeInNanoseconds,
    long DelayInNanoseconds);
