namespace WzComparerX.WzLib;

public sealed record WzImageVideoInspection(
    int Unknown,
    long DataOffset,
    int DataLength,
    WzImageVideoHeaderInspection? Header = null,
    string? HeaderError = null)
{
    public override string ToString()
    {
        return $"unknown={Unknown}, dataLength={DataLength}, dataOffset={DataOffset}";
    }
}

public sealed record WzImageVideoHeaderInspection(
    string Signature,
    int HeaderLength,
    uint FourCc,
    string FourCcText,
    int Width,
    int Height,
    int FrameCount,
    WzImageVideoDataFlags DataFlags,
    long FrameDelayUnit,
    int DefaultDelay,
    IReadOnlyList<WzImageVideoFrameInspection> Frames);

[Flags]
public enum WzImageVideoDataFlags
{
    Default = 0,
    AlphaMap = 1,
    PerFrameDelay = 2,
    PerFrameTimeline = 4
}

public sealed record WzImageVideoFrameInspection(
    int Index,
    long DataOffset,
    int DataLength,
    long AlphaDataOffset,
    int AlphaDataLength,
    long DelayInNanoseconds,
    long StartTimeInNanoseconds);
