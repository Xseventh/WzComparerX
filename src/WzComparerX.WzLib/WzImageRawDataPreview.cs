namespace WzComparerX.WzLib;

public sealed record WzImageRawDataPreview(
    int Version,
    long DataOffset,
    int DataLength)
{
    public override string ToString()
    {
        return $"version={Version}, dataLength={DataLength}, dataOffset={DataOffset}";
    }
}
