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
            new ResourceInspectionJsonFormatter().Format(inspection));
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
            throw new InvalidDataException($"Selected image is not a supported text IMG: {selector}.");
        }

        return new ResourceExportDocument(
            inspection.Header.SourcePath,
            ResourceExportKind.Text,
            "text/plain; charset=utf-8",
            text.Text);
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
            throw new InvalidDataException($"Selected image is not a supported Lua IMG: {selector}.");
        }

        return new ResourceExportDocument(
            inspection.Header.SourcePath,
            ResourceExportKind.Lua,
            "text/x-lua; charset=utf-8",
            string.Join(Environment.NewLine, scripts));
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
}
