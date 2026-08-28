using System.Text;
using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class ResourceSpineExportService
{
    private static readonly string[] CanvasLinkPropertyOrder = ["source", "_inlink", "_outlink", "link"];

    public async Task<ResourceSpineExportDocument> LoadAsync(
        string path,
        string selector,
        string valueSelector,
        ResourceInspectionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueSelector);

        options ??= new ResourceInspectionOptions();
        await using var context = await ResourceImageInspectionContext.LoadAsync(
            path,
            selector,
            options,
            cancellationToken);

        var root = FindProperty(context.ImageInspection, valueSelector);
        if (root?.Children is null)
        {
            throw InvalidAsset(valueSelector, "The selected value is not a Spine resource object.");
        }

        var marker = FindChild(root, "spine")?.Value as string;
        var atlas = FindAtlas(root, marker);
        if (atlas?.Value is not string atlasText)
        {
            throw InvalidAsset(valueSelector, "A Spine atlas string was not found.");
        }

        var skeleton = FindSkeleton(root, marker);
        if (skeleton is null)
        {
            throw InvalidAsset(valueSelector, "A Spine skeleton payload was not found.");
        }

        var spineName = !string.IsNullOrWhiteSpace(marker)
            ? marker
            : Path.GetFileNameWithoutExtension(atlas.Name ?? string.Empty);
        if (string.IsNullOrWhiteSpace(spineName))
        {
            throw InvalidAsset(valueSelector, "The Spine resource name could not be inferred.");
        }

        var skeletonBytes = ReadSkeleton(context.SourceStream, skeleton);
        var skeletonFileName = NormalizeRelativePath(
            EnsureSkeletonExtension(skeleton.Name, spineName, skeleton.Value),
            valueSelector);
        var atlasFileName = NormalizeRelativePath(atlas.Name ?? $"{spineName}.atlas", valueSelector);
        var spineVersion = skeleton.Value is WzImageRawDataInspection
            ? WzSpineSkeletonVersionReader.ReadBinaryVersion(skeletonBytes)
            : ReadJsonVersion(skeletonBytes);

        var atlasPages = WzSpineAtlasReader.ReadPages(atlasText);
        if (atlasPages.Count == 0)
        {
            throw InvalidAsset(valueSelector, "The Spine atlas does not declare any texture pages.");
        }

        var files = new List<ResourceExportFile>
        {
            new(atlasFileName, "text/plain; charset=utf-8", Encoding.UTF8.GetBytes(atlasText)),
            new(skeletonFileName, SkeletonContentType(skeletonFileName), skeletonBytes)
        };
        var pages = new List<ResourceSpineExportPage>(atlasPages.Count);
        var fileNames = new HashSet<string>(files.Select(file => file.RelativePath), StringComparer.OrdinalIgnoreCase);
        var canvasService = new ResourceCanvasImageService();
        foreach (var atlasPage in atlasPages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = NormalizeRelativePath(atlasPage.Path, valueSelector);
            if (!fileNames.Add(relativePath))
            {
                throw InvalidAsset(valueSelector, $"The Spine export contains a duplicate file name: {relativePath}.");
            }

            var textureProperty = FindProperty(context.ImageInspection, CombinePath(root.Path, atlasPage.Path));
            if (textureProperty is null)
            {
                throw InvalidAsset(valueSelector, $"Atlas texture node was not found: {atlasPage.Path}.");
            }

            var canvasValueSelector = SelectCanvasValuePath(textureProperty);
            if (canvasValueSelector is null)
            {
                throw InvalidAsset(valueSelector, $"Atlas texture node is not a Canvas or Canvas link: {atlasPage.Path}.");
            }

            ResourceCanvasImageDocument canvas;
            try
            {
                canvas = await canvasService.LoadForExportAsync(
                    context.SourcePath,
                    context.ImageInspection.Selector,
                    canvasValueSelector,
                    options,
                    cancellationToken);
            }
            catch (ResourceCanvasImageException ex)
            {
                throw new ResourceExportException(
                    ResourceInspectionDiagnostics.ExportSpineTextureFailed(
                        atlasPage.Path,
                        valueSelector,
                        ex.Diagnostic.Message));
            }

            if ((atlasPage.Width is int expectedWidth && expectedWidth != canvas.Width) ||
                (atlasPage.Height is int expectedHeight && expectedHeight != canvas.Height))
            {
                throw InvalidAsset(
                    valueSelector,
                    $"Atlas texture dimensions do not match decoded Canvas {atlasPage.Path}: " +
                    $"atlas={atlasPage.Width}x{atlasPage.Height}, canvas={canvas.Width}x{canvas.Height}.");
            }

            var png = WzImageCanvasPngEncoder.EncodeBgra8888(canvas.Width, canvas.Height, canvas.Pixels);
            files.Add(new ResourceExportFile(relativePath, "image/png", png));
            pages.Add(new ResourceSpineExportPage(relativePath, canvas.Width, canvas.Height));
        }

        return new ResourceSpineExportDocument(
            context.SourcePath,
            context.ImageInspection.Selector,
            root.Path ?? valueSelector,
            spineName,
            atlasFileName,
            skeletonFileName,
            spineVersion,
            pages,
            files);
    }

    private static WzImagePropertyInspectionEntry? FindAtlas(
        WzImagePropertyInspectionEntry root,
        string? marker)
    {
        if (!string.IsNullOrWhiteSpace(marker))
        {
            var named = FindChild(root, $"{marker}.atlas");
            if (named is not null)
            {
                return named;
            }
        }

        var candidates = root.Children?
            .Where(child => child.Name?.EndsWith(".atlas", StringComparison.OrdinalIgnoreCase) == true)
            .Take(2)
            .ToArray() ?? [];
        return candidates.Length == 1 ? candidates[0] : null;
    }

    private static WzImagePropertyInspectionEntry? FindSkeleton(
        WzImagePropertyInspectionEntry root,
        string? marker)
    {
        if (!string.IsNullOrWhiteSpace(marker))
        {
            foreach (var name in new[] { $"{marker}.skel", $"{marker}.json", marker })
            {
                var named = FindChild(root, name);
                if (IsSkeletonValue(named?.Value))
                {
                    return named;
                }
            }
        }

        var candidates = root.Children?
            .Where(child =>
                (child.Name?.EndsWith(".skel", StringComparison.OrdinalIgnoreCase) == true ||
                 child.Name?.EndsWith(".json", StringComparison.OrdinalIgnoreCase) == true) &&
                IsSkeletonValue(child.Value))
            .Take(2)
            .ToArray() ?? [];
        return candidates.Length == 1 ? candidates[0] : null;
    }

    private static bool IsSkeletonValue(object? value)
    {
        return value is WzImageRawDataInspection or string;
    }

    private static byte[] ReadSkeleton(Stream stream, WzImagePropertyInspectionEntry skeleton)
    {
        return skeleton.Value switch
        {
            WzImageRawDataInspection rawData => WzImageRawDataPayloadReader.Read(stream, rawData),
            string json => Encoding.UTF8.GetBytes(json),
            _ => throw InvalidAsset(skeleton.Path, "The Spine skeleton value is not exportable.")
        };
    }

    private static string? ReadJsonVersion(byte[] skeletonBytes)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(skeletonBytes);
            return document.RootElement
                .GetProperty("skeleton")
                .GetProperty("spine")
                .GetString();
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    private static string EnsureSkeletonExtension(string? name, string spineName, object? value)
    {
        if (!string.IsNullOrWhiteSpace(name) && Path.HasExtension(name))
        {
            return name;
        }

        return value is string ? $"{spineName}.json" : $"{spineName}.skel";
    }

    private static string SkeletonContentType(string fileName)
    {
        return fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? "application/json"
            : "application/octet-stream";
    }

    private static string? SelectCanvasValuePath(WzImagePropertyInspectionEntry property)
    {
        if (ResourceInspectionLinkResolver.GetLinkKind(property) is not null)
        {
            return property.Path;
        }

        if (property.Value is not WzImageCanvasInspection)
        {
            return null;
        }

        foreach (var linkName in CanvasLinkPropertyOrder)
        {
            var link = property.Children?.FirstOrDefault(child =>
                string.Equals(child.Name, linkName, StringComparison.Ordinal) &&
                ResourceInspectionLinkResolver.GetLinkKind(child) is not null);
            if (link is not null)
            {
                return link.Path;
            }
        }

        return property.Path;
    }

    private static WzImagePropertyInspectionEntry? FindChild(
        WzImagePropertyInspectionEntry parent,
        string name)
    {
        return parent.Children?.FirstOrDefault(child =>
            string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static WzImagePropertyInspectionEntry? FindProperty(
        WzImageInspection inspection,
        string valuePath)
    {
        return inspection.Properties?
            .SelectMany(Flatten)
            .FirstOrDefault(property => string.Equals(property.Path, valuePath, StringComparison.Ordinal));
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

    private static string CombinePath(string? parent, string child)
    {
        var normalizedChild = child.Replace('\\', '/').Trim('/');
        return string.IsNullOrWhiteSpace(parent)
            ? normalizedChild
            : $"{parent.TrimEnd('/')}/{normalizedChild}";
    }

    private static string NormalizeRelativePath(string path, string? valueSelector)
    {
        var normalized = path.Replace('\\', '/').Trim();
        if (string.IsNullOrWhiteSpace(normalized) ||
            Path.IsPathRooted(normalized) ||
            normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(part => part is "." or "..") ||
            normalized.Contains(':', StringComparison.Ordinal))
        {
            throw InvalidAsset(valueSelector, $"Spine export contains an unsafe relative path: {path}.");
        }

        return string.Join('/', normalized.Split('/', StringSplitOptions.RemoveEmptyEntries));
    }

    private static ResourceExportException InvalidAsset(string? path, string message)
    {
        return new ResourceExportException(ResourceInspectionDiagnostics.ExportSpineAssetInvalid(path, message));
    }
}
