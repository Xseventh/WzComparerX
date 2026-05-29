using System.Text;
using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class ResourceExportService
{
    public async Task<ResourceExportDocument> ExportAsync(
        string path,
        string? selector = null,
        ResourceExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        options ??= new ResourceExportOptions();
        return options.Kind switch
        {
            ResourceExportKind.Metadata => await ExportMetadataAsync(path, selector, options, cancellationToken),
            ResourceExportKind.Text => await ExportTextImageAsync(path, selector, options, cancellationToken),
            ResourceExportKind.Lua => await ExportLuaAsync(path, selector, options, cancellationToken),
            ResourceExportKind.Canvas => await ExportCanvasAsync(path, selector, options, cancellationToken),
            _ => throw new InvalidDataException($"Unsupported export type: {options.Kind}.")
        };
    }

    private static async Task<ResourceExportDocument> ExportMetadataAsync(
        string path,
        string? selector,
        ResourceExportOptions options,
        CancellationToken cancellationToken)
    {
        var inspection = await new ResourceInspectionService().InspectAsync(
            path,
            selector,
            new ResourceInspectionOptions(
                options.StringKey,
                options.MaxPropertyDepth,
                IncludeDebugMetadata: true),
            cancellationToken);
        return new ResourceExportDocument(
            inspection.SourcePath,
            ResourceExportKind.Metadata,
            "application/json",
            Encoding.UTF8.GetBytes(new ResourceInspectionJsonFormatter().Format(inspection)),
            inspection.Diagnostics);
    }

    private static async Task<ResourceExportDocument> ExportTextImageAsync(
        string path,
        string? selector,
        ResourceExportOptions options,
        CancellationToken cancellationToken)
    {
        var inspection = await ReadImageInspectionAsync(path, selector, options, cancellationToken);
        if (inspection.ObjectValue is not WzImageTextInspection text)
        {
            throw Unsupported(ResourceExportKind.Text, selector);
        }

        return new ResourceExportDocument(
            inspection.Header.SourcePath,
            ResourceExportKind.Text,
            "text/plain; charset=utf-8",
            Encoding.UTF8.GetBytes(text.Text));
    }

    private static async Task<ResourceExportDocument> ExportLuaAsync(
        string path,
        string? selector,
        ResourceExportOptions options,
        CancellationToken cancellationToken)
    {
        var inspection = await ReadImageInspectionAsync(path, selector, options, cancellationToken);
        var scripts = ExtractLuaScripts(inspection).ToArray();
        if (scripts.Length == 0)
        {
            throw Unsupported(ResourceExportKind.Lua, selector);
        }

        var diagnostics = scripts.Length > 1
            ? new[]
            {
                ResourceInspectionDiagnostics.ExportLuaMultipleBlocks(scripts.Length, selector)
            }
            : null;
        return new ResourceExportDocument(
            inspection.Header.SourcePath,
            ResourceExportKind.Lua,
            "text/x-lua; charset=utf-8",
            Encoding.UTF8.GetBytes(string.Concat(scripts)),
            diagnostics);
    }

    private static async Task<ResourceExportDocument> ExportCanvasAsync(
        string path,
        string? selector,
        ResourceExportOptions options,
        CancellationToken cancellationToken)
    {
        var inspection = await ReadImageInspectionAsync(path, selector, options, cancellationToken);
        var canvas = SelectCanvas(inspection, selector, options.ValueSelector);

        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = File.OpenRead(inspection.Header.SourcePath);
        WzImageCanvasBgraBitmap bitmap;
        try
        {
            bitmap = new WzImageCanvasBitmapDecoder().DecodeToBgra8888(
                stream,
                canvas.Value,
                canvas.Path);
        }
        catch (WzImageCanvasBitmapDecodeException ex)
        {
            throw new ResourceExportException(ToExportDiagnostic(ex));
        }

        return new ResourceExportDocument(
            inspection.Header.SourcePath,
            ResourceExportKind.Canvas,
            "application/octet-stream",
            bitmap.Pixels);
    }

    private static ResourceInspectionDiagnostic ToExportDiagnostic(WzImageCanvasBitmapDecodeException exception)
    {
        return exception.Kind switch
        {
            WzImageCanvasBitmapDecodeFailureKind.CompressionUnsupported =>
                ResourceInspectionDiagnostics.ExportCanvasCompressionUnsupported(exception.CompressionKind!.Value, exception.Path),
            WzImageCanvasBitmapDecodeFailureKind.FormatUnsupported =>
                ResourceInspectionDiagnostics.ExportCanvasFormatUnsupported(exception.Format!.Value, exception.Path),
            WzImageCanvasBitmapDecodeFailureKind.ScaleUnsupported =>
                ResourceInspectionDiagnostics.ExportCanvasScaleUnsupported(exception.Scale!.Value, exception.Path),
            _ => ResourceInspectionDiagnostics.ExportCanvasDecodeFailed(exception.Path)
        };
    }

    private static IEnumerable<string> ExtractLuaScripts(WzImageInspection inspection)
    {
        if (inspection.ObjectValue is WzImageLuaInspection lua)
        {
            yield return lua.Script;
            yield break;
        }

        if (inspection.Properties is null)
        {
            yield break;
        }

        foreach (var property in inspection.Properties)
        {
            if (property.Value is WzImageLuaInspection entry)
            {
                yield return entry.Script;
            }
        }
    }

    private static CanvasExportTarget SelectCanvas(
        WzImageInspection inspection,
        string? selector,
        string? valueSelector)
    {
        if (string.IsNullOrWhiteSpace(valueSelector))
        {
            if (inspection.ObjectValue is WzImageCanvasInspection rootCanvas)
            {
                return new CanvasExportTarget(rootCanvas, selector);
            }

            throw new ResourceExportException(ResourceInspectionDiagnostics.ExportValueRequired(ResourceExportKind.Canvas, selector));
        }

        var matches = inspection.Properties?
            .SelectMany(Flatten)
            .Where(property => string.Equals(property.Path, valueSelector, StringComparison.Ordinal))
            .ToArray() ?? [];

        if (matches.Length == 0)
        {
            throw new ResourceExportException(ResourceInspectionDiagnostics.ExportValueNotFound(valueSelector, selector));
        }

        if (matches.Length > 1)
        {
            throw new ResourceExportException(ResourceInspectionDiagnostics.ExportValueAmbiguous(valueSelector, selector));
        }

        var match = matches[0];
        return match.Value is WzImageCanvasInspection canvas
            ? new CanvasExportTarget(canvas, match.Path ?? valueSelector)
            : throw new ResourceExportException(ResourceInspectionDiagnostics.ExportValueUnsupported(ResourceExportKind.Canvas, valueSelector, selector));
    }

    private static IEnumerable<WzImagePropertyInspectionEntry> Flatten(WzImagePropertyInspectionEntry property)
    {
        yield return property;
        if (property.Children is null)
        {
            yield break;
        }

        foreach (var child in property.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }

    private static async Task<WzImageInspection> ReadImageInspectionAsync(
        string path,
        string? selector,
        ResourceExportOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            throw new InvalidDataException("Image export requires an image selector.");
        }

        var context = await WzImageInspectionLoader.LoadAsync(
            path,
            selector,
            options.StringKey,
            options.MaxPropertyDepth,
            cancellationToken);
        return context.ImageInspection;
    }

    private static ResourceExportException Unsupported(ResourceExportKind kind, string? selector)
    {
        return new ResourceExportException(ResourceInspectionDiagnostics.ExportUnsupported(kind, selector));
    }

    private sealed record CanvasExportTarget(WzImageCanvasInspection Value, string? Path);
}
