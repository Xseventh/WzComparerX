namespace WzComparerX.Core;

public static class ResourceDiagnosticCodes
{
    public const string CanvasPixelDecodingUnsupported = "wcx.payload.canvas.pixelsUnsupported";
    public const string RawDataPayloadDecodingUnsupported = "wcx.payload.rawData.unsupported";
    public const string VideoPayloadDecodingUnsupported = "wcx.payload.video.unsupported";
    public const string AudioPayloadDecodingUnsupported = "wcx.payload.audio.unsupported";
    public const string ExportLuaMultipleBlocks = "wcx.export.lua.multipleBlocks";
    public const string ExportUnsupported = "wcx.export.unsupported";
    public const string ExportCanvasCompressionUnsupported = "wcx.export.canvas.compressionUnsupported";
    public const string ExportCanvasFormatUnsupported = "wcx.export.canvas.formatUnsupported";
    public const string ExportCanvasDecodeFailed = "wcx.export.canvas.decodeFailed";
    public const string ExportBinaryOutRequired = "wcx.export.binary.outRequired";
}
