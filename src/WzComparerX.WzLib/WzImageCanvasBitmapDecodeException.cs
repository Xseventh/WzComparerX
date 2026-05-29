namespace WzComparerX.WzLib;

public sealed class WzImageCanvasBitmapDecodeException : Exception
{
    private WzImageCanvasBitmapDecodeException(
        WzImageCanvasBitmapDecodeFailureKind kind,
        string? path,
        WzImageCanvasCompressionKind? compressionKind = null,
        int? format = null,
        int? scale = null,
        Exception? innerException = null)
        : base(CreateMessage(kind, path, compressionKind, format, scale), innerException)
    {
        Kind = kind;
        Path = path;
        CompressionKind = compressionKind;
        Format = format;
        Scale = scale;
    }

    public WzImageCanvasBitmapDecodeFailureKind Kind { get; }

    public string? Path { get; }

    public WzImageCanvasCompressionKind? CompressionKind { get; }

    public int? Format { get; }

    public int? Scale { get; }

    public static WzImageCanvasBitmapDecodeException CompressionUnsupported(
        WzImageCanvasCompressionKind compressionKind,
        string? path)
    {
        return new WzImageCanvasBitmapDecodeException(
            WzImageCanvasBitmapDecodeFailureKind.CompressionUnsupported,
            path,
            compressionKind: compressionKind);
    }

    public static WzImageCanvasBitmapDecodeException FormatUnsupported(
        int format,
        string? path)
    {
        return new WzImageCanvasBitmapDecodeException(
            WzImageCanvasBitmapDecodeFailureKind.FormatUnsupported,
            path,
            format: format);
    }

    public static WzImageCanvasBitmapDecodeException ScaleUnsupported(
        int scale,
        string? path)
    {
        return new WzImageCanvasBitmapDecodeException(
            WzImageCanvasBitmapDecodeFailureKind.ScaleUnsupported,
            path,
            scale: scale);
    }

    public static WzImageCanvasBitmapDecodeException DecodeFailed(
        string? path,
        Exception? innerException = null)
    {
        return new WzImageCanvasBitmapDecodeException(
            WzImageCanvasBitmapDecodeFailureKind.DecodeFailed,
            path,
            innerException: innerException);
    }

    private static string CreateMessage(
        WzImageCanvasBitmapDecodeFailureKind kind,
        string? path,
        WzImageCanvasCompressionKind? compressionKind,
        int? format,
        int? scale)
    {
        return kind switch
        {
            WzImageCanvasBitmapDecodeFailureKind.CompressionUnsupported =>
                $"Canvas bitmap decode does not support compression kind {compressionKind}.",
            WzImageCanvasBitmapDecodeFailureKind.FormatUnsupported =>
                $"Canvas bitmap decode does not support format {format}.",
            WzImageCanvasBitmapDecodeFailureKind.ScaleUnsupported =>
                $"Canvas bitmap decode does not support scale {scale}.",
            _ => $"Canvas bitmap decode failed for {path ?? "selected Canvas"}."
        };
    }
}

public enum WzImageCanvasBitmapDecodeFailureKind
{
    CompressionUnsupported,
    FormatUnsupported,
    ScaleUnsupported,
    DecodeFailed
}
