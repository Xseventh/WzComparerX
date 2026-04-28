namespace WzComparerX.Core;

public static class ResourceInspectionDiagnostics
{
    private const string InfoSeverity = "info";
    private const string ErrorSeverity = "error";
    private const string ParserSource = "parser";
    private const string ExportSource = "export";

    public static ResourceInspectionDiagnostic CanvasPixelDecodingUnsupported(string? path)
    {
        return new ResourceInspectionDiagnostic(
            InfoSeverity,
            "Canvas pixel decoding is not implemented.",
            path,
            ResourceDiagnosticCodes.CanvasPixelDecodingUnsupported,
            ParserSource);
    }

    public static ResourceInspectionDiagnostic RawDataPayloadDecodingUnsupported(string? path)
    {
        return new ResourceInspectionDiagnostic(
            InfoSeverity,
            "RawData payload decoding is not implemented.",
            path,
            ResourceDiagnosticCodes.RawDataPayloadDecodingUnsupported,
            ParserSource);
    }

    public static ResourceInspectionDiagnostic VideoPayloadDecodingUnsupported(string? path)
    {
        return new ResourceInspectionDiagnostic(
            InfoSeverity,
            "Video payload decoding is not implemented.",
            path,
            ResourceDiagnosticCodes.VideoPayloadDecodingUnsupported,
            ParserSource);
    }

    public static ResourceInspectionDiagnostic AudioPayloadDecodingUnsupported(string? path)
    {
        return new ResourceInspectionDiagnostic(
            InfoSeverity,
            "Audio payload decoding is not implemented.",
            path,
            ResourceDiagnosticCodes.AudioPayloadDecodingUnsupported,
            ParserSource);
    }

    public static ResourceInspectionDiagnostic ExportLuaMultipleBlocks(int blockCount, string? selector)
    {
        return new ResourceInspectionDiagnostic(
            InfoSeverity,
            $"Exported {blockCount} Lua blocks in stream order.",
            selector,
            ResourceDiagnosticCodes.ExportLuaMultipleBlocks,
            ExportSource);
    }

    public static ResourceInspectionDiagnostic ExportUnsupported(ResourceExportKind kind, string? selector)
    {
        var message = kind switch
        {
            ResourceExportKind.Text => $"Selected image is not a supported text IMG: {selector}.",
            ResourceExportKind.Lua => $"Selected image is not a supported Lua IMG: {selector}.",
            _ => $"Unsupported export type: {kind}."
        };

        return new ResourceInspectionDiagnostic(
            ErrorSeverity,
            message,
            selector,
            ResourceDiagnosticCodes.ExportUnsupported,
            ExportSource);
    }
}
