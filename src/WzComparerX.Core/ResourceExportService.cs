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
        if (string.IsNullOrWhiteSpace(selector))
        {
            throw new InvalidDataException("Image export requires an image selector.");
        }

        ResourceCanvasImageDocument canvas;
        try
        {
            canvas = await new ResourceCanvasImageService().LoadForExportAsync(
                path,
                selector,
                options.ValueSelector,
                new ResourceInspectionOptions(
                    options.StringKey,
                    options.MaxPropertyDepth),
                cancellationToken);
        }
        catch (ResourceCanvasImageException ex)
        {
            throw new ResourceExportException(ex.Diagnostic);
        }

        return new ResourceExportDocument(
            canvas.SourcePath,
            ResourceExportKind.Canvas,
            "application/octet-stream",
            canvas.Pixels);
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
}
