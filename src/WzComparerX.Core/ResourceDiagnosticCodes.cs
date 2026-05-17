namespace WzComparerX.Core;

public static class ResourceDiagnosticCodes
{
    public const string CanvasPixelDecodingPartial = "wcx.payload.canvas.pixelsPartial";
    public const string RawDataPayloadDecodingUnsupported = "wcx.payload.rawData.unsupported";
    public const string VideoPayloadDecodingUnsupported = "wcx.payload.video.unsupported";
    public const string AudioPayloadDecodingUnsupported = "wcx.payload.audio.unsupported";
    public const string Pkg2DirectoryInspectionUnsupported = "wcx.package.pkg2.directoryUnsupported";
    public const string MsContainerInspectionUnsupported = "wcx.package.ms.directoryUnsupported";
    public const string MsImageInspectionUnsupported = "wcx.package.ms.imageUnsupported";
    public const string SplitPackageLinkUnresolved = "wcx.package.link.unresolved";
    public const string PackageGroupShardMissing = "wcx.package.group.shardMissing";
    public const string PackageGroupShardInvalid = "wcx.package.group.shardInvalid";
    public const string ImageEntryNotFound = "wcx.inspection.image.notFound";
    public const string ExportLuaMultipleBlocks = "wcx.export.lua.multipleBlocks";
    public const string ExportUnsupported = "wcx.export.unsupported";
    public const string ExportCanvasCompressionUnsupported = "wcx.export.canvas.compressionUnsupported";
    public const string ExportCanvasFormatUnsupported = "wcx.export.canvas.formatUnsupported";
    public const string ExportCanvasDecodeFailed = "wcx.export.canvas.decodeFailed";
    public const string ExportBinaryOutRequired = "wcx.export.binary.outRequired";
    public const string ExportValueRequired = "wcx.export.value.required";
    public const string ExportValueNotFound = "wcx.export.value.notFound";
    public const string ExportValueUnsupported = "wcx.export.value.unsupported";
    public const string ExportValueAmbiguous = "wcx.export.value.ambiguous";
    public const string CanvasPreviewCompressionUnsupported = "wcx.viewer.canvas.compressionUnsupported";
    public const string CanvasPreviewFormatUnsupported = "wcx.viewer.canvas.formatUnsupported";
    public const string CanvasPreviewScaleUnsupported = "wcx.viewer.canvas.scaleUnsupported";
    public const string CanvasPreviewDecodeFailed = "wcx.viewer.canvas.decodeFailed";
    public const string CanvasPreviewLinkUnresolved = "wcx.viewer.canvas.linkUnresolved";
    public const string CanvasPreviewValueRequired = "wcx.viewer.canvas.valueRequired";
    public const string CanvasPreviewValueNotFound = "wcx.viewer.canvas.valueNotFound";
    public const string CanvasPreviewValueUnsupported = "wcx.viewer.canvas.valueUnsupported";
    public const string CanvasPreviewValueAmbiguous = "wcx.viewer.canvas.valueAmbiguous";
}
