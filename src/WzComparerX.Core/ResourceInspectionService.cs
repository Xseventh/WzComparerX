using System.Globalization;
using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class ResourceInspectionService
{
    public async Task<ResourceInspectionDocument> InspectAsync(
        string path,
        string? selector = null,
        WzStringEncryptionKind? stringKey = WzStringEncryptionKind.None,
        int maxPropertyDepth = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (selector is not null)
        {
            return await InspectImageAsync(path, selector, stringKey, maxPropertyDepth, cancellationToken);
        }

        if (string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return await InspectSyntheticAsync(path, cancellationToken);
        }

        return await InspectDirectoryAsync(path, stringKey, cancellationToken);
    }

    private static async Task<ResourceInspectionDocument> InspectSyntheticAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var document = await new ResourceDocumentService().OpenAsync(path, cancellationToken);
        return new ResourceInspectionDocument(
            document.SourcePath,
            "synthetic",
            ProjectRawNode(document.Root, parentPath: null));
    }

    private static async Task<ResourceInspectionDocument> InspectDirectoryAsync(
        string path,
        WzStringEncryptionKind? stringKey,
        CancellationToken cancellationToken)
    {
        var preview = stringKey is null
            ? await WzDirectoryPreviewService.ReadAutoAsync(path, cancellationToken)
            : await new WzDirectoryPreviewService(stringKey.Value).ReadAsync(path, cancellationToken);
        if (!preview.Header.IsValid)
        {
            throw new InvalidDataException($"Invalid WZ package: {preview.Header.SourcePath}.");
        }

        var root = BuildDirectoryRoot(preview);
        return new ResourceInspectionDocument(
            preview.Header.SourcePath,
            preview.Header.Format.ToString().ToLowerInvariant(),
            root);
    }

    private static async Task<ResourceInspectionDocument> InspectImageAsync(
        string path,
        string selector,
        WzStringEncryptionKind? stringKey,
        int maxPropertyDepth,
        CancellationToken cancellationToken)
    {
        var preview = stringKey is null
            ? await WzImagePreviewService.ReadAutoAsync(path, selector, maxPropertyDepth, cancellationToken)
            : await new WzImagePreviewService(stringKey.Value, maxPropertyDepth).ReadAsync(path, selector, cancellationToken);
        if (!preview.IsValid)
        {
            throw new InvalidDataException($"Invalid WZ image selection: {selector}.");
        }

        var root = ProjectImagePreview(preview);
        return new ResourceInspectionDocument(
            preview.Header.SourcePath,
            preview.Header.Format.ToString().ToLowerInvariant(),
            root);
    }

    private static ResourceInspectionNode ProjectRawNode(RawResourceNode node, string? parentPath)
    {
        var path = CombinePath(parentPath, node.Name);
        var value = node.Kind == RawResourceNodeKind.Value
            ? FormatValue(node.ValueKind, node.Value)
            : null;
        return new ResourceInspectionNode(
            node.Name,
            node.Kind.ToString().ToLowerInvariant(),
            path,
            value,
            node.Children.Select(child => ProjectRawNode(child, path)).ToArray());
    }

    private static ResourceInspectionNode BuildDirectoryRoot(WzDirectoryPreview preview)
    {
        var rootName = Path.GetFileName(preview.Header.SourcePath);
        if (string.IsNullOrWhiteSpace(rootName))
        {
            rootName = preview.Header.SourcePath;
        }

        var builder = new InspectionNodeBuilder(rootName, "package", rootName, preview.Header.Format.ToString().ToLowerInvariant());
        foreach (var entry in preview.Entries)
        {
            var entryPath = entry.Path ?? entry.Name ?? entry.Index.ToString(CultureInfo.InvariantCulture);
            var parts = entryPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                parts = [entryPath];
            }

            builder.AddPath(parts, 0, entry.Kind.ToString().ToLowerInvariant());
        }

        return builder.ToNode();
    }

    private static ResourceInspectionNode ProjectImagePreview(WzImagePreview preview)
    {
        var name = preview.Entry?.Path ?? preview.Entry?.Name ?? preview.Selector;
        var displayValue = preview.ObjectValue is not null
            ? FormatObject(preview.ObjectValue)
            : preview.ObjectType;
        var children = preview.Properties?
            .Select(property => ProjectImageProperty(property))
            .ToArray() ?? Array.Empty<ResourceInspectionNode>();

        return new ResourceInspectionNode(
            name,
            "image",
            name,
            displayValue,
            children);
    }

    private static ResourceInspectionNode ProjectImageProperty(WzImagePropertyPreviewEntry property)
    {
        var name = property.Name ?? property.Index.ToString(CultureInfo.InvariantCulture);
        var children = property.Children?
            .Select(ProjectImageProperty)
            .ToArray() ?? Array.Empty<ResourceInspectionNode>();
        var value = property.Value is not null ? FormatObject(property.Value) : null;
        return new ResourceInspectionNode(name, property.Kind, property.Path, value, children);
    }

    private static string? FormatValue(string? valueKind, string? value)
    {
        if (string.IsNullOrWhiteSpace(valueKind))
        {
            return value;
        }

        return string.IsNullOrWhiteSpace(value) ? valueKind : $"{valueKind} = {value}";
    }

    private static string FormatObject(object value)
    {
        return value switch
        {
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string CombinePath(string? parentPath, string name)
    {
        return string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";
    }

    private sealed class InspectionNodeBuilder
    {
        private readonly List<InspectionNodeBuilder> children = [];

        public InspectionNodeBuilder(string name, string kind, string? path, string? displayValue = null)
        {
            Name = name;
            Kind = kind;
            Path = path;
            DisplayValue = displayValue;
        }

        public string Name { get; }

        public string Kind { get; private set; }

        public string? Path { get; }

        public string? DisplayValue { get; }

        public void AddPath(string[] parts, int index, string leafKind)
        {
            var name = parts[index];
            var child = children.FirstOrDefault(candidate => candidate.Name == name);
            if (child is null)
            {
                var path = string.IsNullOrEmpty(Path) ? name : $"{Path}/{name}";
                var kind = index == parts.Length - 1 ? leafKind : "directory";
                child = new InspectionNodeBuilder(name, kind, path);
                children.Add(child);
            }

            if (index == parts.Length - 1)
            {
                child.Kind = leafKind;
                return;
            }

            child.AddPath(parts, index + 1, leafKind);
        }

        public ResourceInspectionNode ToNode()
        {
            return new ResourceInspectionNode(
                Name,
                Kind,
                Path,
                DisplayValue,
                children.Select(child => child.ToNode()).ToArray());
        }
    }
}
