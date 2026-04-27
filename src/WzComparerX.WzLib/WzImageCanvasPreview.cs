namespace WzComparerX.WzLib;

public sealed record WzImageCanvasPreview(
    int Width,
    int Height,
    int Format,
    int Scale,
    int Pages,
    int Unknown1,
    long DataOffset,
    int DataLength,
    WzImageCanvasCompressionKind CompressionKind = WzImageCanvasCompressionKind.Unknown,
    int? UncompressedDataLength = null)
{
    public int ActualScale => Scale > 0 ? 1 << Scale : 1;

    public int ActualPages => Pages > 0 ? Pages : 1;

    public override string ToString()
    {
        return $"{Width}x{Height}, format={Format}, scale={Scale}, pages={Pages}, compression={CompressionKind}, dataLength={DataLength}, uncompressed={UncompressedDataLength}, dataOffset={DataOffset}";
    }
}
