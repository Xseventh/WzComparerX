using WzComparerX.WzLib;

namespace WzComparerX.Core;

public static class ResourceInspectionDiagnostics
{
    public static ResourceInspectionDiagnostic CanvasPixelDecodingUnsupported(string? path)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Info,
            "Canvas pixel decoding is not implemented.",
            path,
            ResourceDiagnosticCodes.CanvasPixelDecodingUnsupported,
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

    public static ResourceInspectionDiagnostic ExportCanvasDecodeFailed(string message, string? selector)
    {
        return new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Error,
            $"Canvas export failed to decode payload: {message}",
            selector,
            ResourceDiagnosticCodes.ExportCanvasDecodeFailed,
            ResourceDiagnosticSources.Export);
    }
}
