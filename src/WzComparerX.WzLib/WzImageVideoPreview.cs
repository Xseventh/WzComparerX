namespace WzComparerX.WzLib;

public sealed record WzImageVideoPreview(
    int Unknown,
    long DataOffset,
    int DataLength)
{
    public override string ToString()
    {
        return $"unknown={Unknown}, dataLength={DataLength}, dataOffset={DataOffset}";
    }
}
