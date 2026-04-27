namespace WzComparerX.WzLib;

public sealed record WzImageRawDataInspection(
    int Version,
    long DataOffset,
    int DataLength)
{
    public override string ToString()
    {
        return $"version={Version}, dataLength={DataLength}, dataOffset={DataOffset}";
    }
}
