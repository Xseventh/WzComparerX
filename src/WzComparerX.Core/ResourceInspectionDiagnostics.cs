using WzComparerX.WzLib;

namespace WzComparerX.Core;

public static class ResourceInspectionDiagnostics
{
    public static ResourceInspectionDiagnostic CanvasPixelDecodingPartial(string? path)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Info,
            "Canvas pixel decoding is lazy and currently supports a narrow direct-zlib format slice.",
            path,
            ResourceDiagnosticCodes.CanvasPixelDecodingPartial,
            ResourceDiagnosticSources.Parser);
    }

    public static ResourceInspectionDiagnostic RawDataPayloadDecodingUnsupported(string? path)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Info,
            "RawData payload decoding is not implemented.",
            path,
            ResourceDiagnosticCodes.RawDataPayloadDecodingUnsupported,
            ResourceDiagnosticSources.Parser);
    }

    public static ResourceInspectionDiagnostic VideoPayloadDecodingUnsupported(string? path)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Info,
            "Video payload decoding is not implemented.",
            path,
            ResourceDiagnosticCodes.VideoPayloadDecodingUnsupported,
            ResourceDiagnosticSources.Parser);
    }

    public static ResourceInspectionDiagnostic AudioPayloadDecodingUnsupported(string? path)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Info,
            "Audio payload decoding is not implemented.",
            path,
            ResourceDiagnosticCodes.AudioPayloadDecodingUnsupported,
            ResourceDiagnosticSources.Parser);
    }

    public static ResourceInspectionDiagnostic ExportLuaMultipleBlocks(int blockCount, string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Info,
            $"Exported {blockCount} Lua blocks in stream order.",
            selector,
            ResourceDiagnosticCodes.ExportLuaMultipleBlocks,
            ResourceDiagnosticSources.Export);
    }

    public static ResourceInspectionDiagnostic ExportUnsupported(ResourceExportKind kind, string? selector)
    {
        var message = kind switch
        {
            ResourceExportKind.Text => $"Selected image is not a supported text IMG: {selector}.",
            ResourceExportKind.Lua => $"Selected image is not a supported Lua IMG: {selector}.",
            ResourceExportKind.Canvas => $"Selected image is not a supported Canvas IMG: {selector}.",
            _ => $"Unsupported export type: {kind}."
        };

        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            message,
            selector,
            ResourceDiagnosticCodes.ExportUnsupported,
            ResourceDiagnosticSources.Export);
    }

    public static ResourceInspectionDiagnostic ExportCanvasCompressionUnsupported(
        WzImageCanvasCompressionKind compressionKind,
        string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            $"Canvas export does not support compression kind {compressionKind}.",
            selector,
            ResourceDiagnosticCodes.ExportCanvasCompressionUnsupported,
            ResourceDiagnosticSources.Export);
    }

    public static ResourceInspectionDiagnostic ExportCanvasFormatUnsupported(int format, string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            $"Canvas export does not support format {format}.",
            selector,
            ResourceDiagnosticCodes.ExportCanvasFormatUnsupported,
            ResourceDiagnosticSources.Export);
    }

    public static ResourceInspectionDiagnostic ExportCanvasDecodeFailed(string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            "Canvas export failed to decode payload.",
            selector,
            ResourceDiagnosticCodes.ExportCanvasDecodeFailed,
            ResourceDiagnosticSources.Export);
    }

    public static ResourceInspectionDiagnostic ExportBinaryOutRequired()
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            "Binary export requires --out <path>.",
            Code: ResourceDiagnosticCodes.ExportBinaryOutRequired,
            Source: ResourceDiagnosticSources.Export);
    }

    public static ResourceInspectionDiagnostic ExportValueRequired(ResourceExportKind kind, string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            $"{kind} export requires --value <path> unless the selected image root is directly exportable.",
            selector,
            ResourceDiagnosticCodes.ExportValueRequired,
            ResourceDiagnosticSources.Export);
    }

    public static ResourceInspectionDiagnostic ExportValueNotFound(string valueSelector, string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            $"Export value not found: {valueSelector}.",
            Combine(selector, valueSelector),
            ResourceDiagnosticCodes.ExportValueNotFound,
            ResourceDiagnosticSources.Export);
    }

    public static ResourceInspectionDiagnostic ExportValueUnsupported(ResourceExportKind kind, string valueSelector, string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            $"Selected export value is not supported for {kind} export: {valueSelector}.",
            Combine(selector, valueSelector),
            ResourceDiagnosticCodes.ExportValueUnsupported,
            ResourceDiagnosticSources.Export);
    }

    public static ResourceInspectionDiagnostic ExportValueAmbiguous(string valueSelector, string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            $"Export value selector is ambiguous: {valueSelector}.",
            Combine(selector, valueSelector),
            ResourceDiagnosticCodes.ExportValueAmbiguous,
            ResourceDiagnosticSources.Export);
    }

    public static ResourceInspectionDiagnostic CanvasPreviewCompressionUnsupported(
        WzImageCanvasCompressionKind compressionKind,
        string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            $"Canvas preview does not support compression kind {compressionKind}.",
            selector,
            ResourceDiagnosticCodes.CanvasPreviewCompressionUnsupported,
            ResourceDiagnosticSources.Viewer);
    }

    public static ResourceInspectionDiagnostic CanvasPreviewFormatUnsupported(int format, string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            $"Canvas preview currently supports formats 1 and 2 only; found format {format}.",
            selector,
            ResourceDiagnosticCodes.CanvasPreviewFormatUnsupported,
            ResourceDiagnosticSources.Viewer);
    }

    public static ResourceInspectionDiagnostic CanvasPreviewScaleUnsupported(int scale, string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            $"Canvas preview currently supports unscaled images only; found scale {scale}.",
            selector,
            ResourceDiagnosticCodes.CanvasPreviewScaleUnsupported,
            ResourceDiagnosticSources.Viewer);
    }

    public static ResourceInspectionDiagnostic CanvasPreviewDecodeFailed(string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            "Canvas preview failed to decode payload.",
            selector,
            ResourceDiagnosticCodes.CanvasPreviewDecodeFailed,
            ResourceDiagnosticSources.Viewer);
    }

    public static ResourceInspectionDiagnostic CanvasPreviewValueRequired(string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            "Canvas preview requires selecting a Canvas value.",
            selector,
            ResourceDiagnosticCodes.CanvasPreviewValueRequired,
            ResourceDiagnosticSources.Viewer);
    }

    public static ResourceInspectionDiagnostic CanvasPreviewValueNotFound(string valueSelector, string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            $"Canvas preview value not found: {valueSelector}.",
            Combine(selector, valueSelector),
            ResourceDiagnosticCodes.CanvasPreviewValueNotFound,
            ResourceDiagnosticSources.Viewer);
    }

    public static ResourceInspectionDiagnostic CanvasPreviewValueUnsupported(string valueSelector, string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            $"Selected preview value is not a Canvas: {valueSelector}.",
            Combine(selector, valueSelector),
            ResourceDiagnosticCodes.CanvasPreviewValueUnsupported,
            ResourceDiagnosticSources.Viewer);
    }

    public static ResourceInspectionDiagnostic CanvasPreviewValueAmbiguous(string valueSelector, string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            $"Canvas preview value selector is ambiguous: {valueSelector}.",
            Combine(selector, valueSelector),
            ResourceDiagnosticCodes.CanvasPreviewValueAmbiguous,
            ResourceDiagnosticSources.Viewer);
    }

    private static string? Combine(string? selector, string? valueSelector)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            return valueSelector;
        }

        if (string.IsNullOrWhiteSpace(valueSelector))
        {
            return selector;
        }

        return $"{selector}/{valueSelector}";
    }
}
