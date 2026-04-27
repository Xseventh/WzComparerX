using System.Globalization;
using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class ResourceInspectionService
{
    public Task<ResourceInspectionDocument> InspectAsync(
        string path,
        string? selector = null,
        WzStringEncryptionKind? stringKey = WzStringEncryptionKind.None,
        int maxPropertyDepth = 1,
        CancellationToken cancellationToken = default)
    {
        return InspectAsync(
            path,
            selector,
            new ResourceInspectionOptions(stringKey, maxPropertyDepth),
            cancellationToken);
    }

    public async Task<ResourceInspectionDocument> InspectAsync(
        string path,
        string? selector,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(options);

        if (selector is not null)
        {
            return await InspectImageAsync(path, selector, options, cancellationToken);
        }

        if (string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return await InspectSyntheticAsync(path, options, cancellationToken);
        }

        return await InspectDirectoryAsync(path, options, cancellationToken);
    }

    private static async Task<ResourceInspectionDocument> InspectSyntheticAsync(
        string path,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken)
    {
        var document = await new ResourceDocumentService().OpenAsync(path, cancellationToken);
        return new ResourceInspectionDocument(
            document.SourcePath,
            "synthetic",
            ProjectRawNode(document.Root, parentPath: null),
            options.IncludeDebugMetadata ? [new ResourceInspectionMetadata("sourceKind", "synthetic")] : null);
    }

    private static async Task<ResourceInspectionDocument> InspectDirectoryAsync(
        string path,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken)
    {
        var preview = await ReadDirectoryAsync(path, options.StringKey, cancellationToken);
        if (!preview.Header.IsValid)
        {
            throw new InvalidDataException($"Invalid WZ package: {preview.Header.SourcePath}.");
        }

        var root = BuildDirectoryRoot(preview, options.IncludeDebugMetadata);
        return new ResourceInspectionDocument(
            preview.Header.SourcePath,
            preview.Header.Format.ToString().ToLowerInvariant(),
            root,
            options.IncludeDebugMetadata ? BuildDirectoryDocumentMetadata(preview) : null);
    }

    private static async Task<ResourceInspectionDocument> InspectImageAsync(
        string path,
        string selector,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken)
    {
        var directoryPreview = await ReadDirectoryAsync(path, options.StringKey, cancellationToken);
        if (!directoryPreview.Header.IsValid)
        {
            throw new InvalidDataException($"Invalid WZ package: {directoryPreview.Header.SourcePath}.");
        }

        var entry = FindImageEntry(directoryPreview, selector);
        var stringKey = directoryPreview.StringEncryptionKind ?? options.StringKey ?? WzStringEncryptionKind.None;
        await using var stream = File.OpenRead(path);
        var imageReader = new WzImagePreviewReader(new WzStringDecryptor(stringKey), options.MaxPropertyDepth);
        var preview = imageReader.Read(stream, directoryPreview.Header, entry, selector);
        if (!preview.IsValid)
        {
            throw new InvalidDataException($"Invalid WZ image selection: {selector}.");
        }

        var root = ProjectImagePreview(preview, options.IncludeDebugMetadata);
        return new ResourceInspectionDocument(
            preview.Header.SourcePath,
            preview.Header.Format.ToString().ToLowerInvariant(),
            root,
            options.IncludeDebugMetadata ? BuildImageDocumentMetadata(directoryPreview, preview, stringKey) : null,
            root.Diagnostics);
    }

    private static Task<WzDirectoryPreview> ReadDirectoryAsync(
        string path,
        WzStringEncryptionKind? stringKey,
        CancellationToken cancellationToken)
    {
        if (stringKey is null)
        {
            return WzStringKeyAutoDetector.ReadDirectoryAsync(path, cancellationToken);
        }

        var reader = new WzDirectoryPreviewReader(stringDecryptor: new WzStringDecryptor(stringKey.Value));
        return reader.ReadAsync(path, cancellationToken);
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

    private static ResourceInspectionNode BuildDirectoryRoot(WzDirectoryPreview preview, bool includeDebugMetadata)
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

            builder.AddPath(
                parts,
                0,
                entry.Kind.ToString().ToLowerInvariant(),
                includeDebugMetadata ? BuildDirectoryEntryMetadata(entry) : null);
        }

        return builder.ToNode();
    }

    private static ResourceInspectionNode ProjectImagePreview(WzImagePreview preview, bool includeDebugMetadata)
    {
        var name = preview.Entry?.Path ?? preview.Entry?.Name ?? preview.Selector;
        var displayValue = preview.ObjectValue is not null
            ? FormatObject(preview.ObjectValue)
            : preview.ObjectType;
        var children = preview.Properties?
            .Select(property => ProjectImageProperty(property, includeDebugMetadata))
            .ToArray() ?? Array.Empty<ResourceInspectionNode>();
        var diagnostics = includeDebugMetadata ? BuildValueDiagnostics(preview.ObjectValue, name) : null;

        return new ResourceInspectionNode(
            name,
            "image",
            name,
            displayValue,
            children,
            includeDebugMetadata ? BuildImageRootMetadata(preview) : null,
            diagnostics);
    }

    private static ResourceInspectionNode ProjectImageProperty(
        WzImagePropertyPreviewEntry property,
        bool includeDebugMetadata)
    {
        var name = property.Name ?? property.Index.ToString(CultureInfo.InvariantCulture);
        var children = property.Children?
            .Select(child => ProjectImageProperty(child, includeDebugMetadata))
            .ToArray() ?? Array.Empty<ResourceInspectionNode>();
        var value = property.Value is not null ? FormatObject(property.Value) : null;
        return new ResourceInspectionNode(
            name,
            property.Kind,
            property.Path,
            value,
            children,
            includeDebugMetadata ? BuildImagePropertyMetadata(property) : null,
            includeDebugMetadata ? BuildValueDiagnostics(property.Value, property.Path) : null);
    }

    private static WzDirectoryEntryPreview FindImageEntry(WzDirectoryPreview preview, string selector)
    {
        if (int.TryParse(selector, out var index))
        {
            var indexed = preview.Entries.FirstOrDefault(entry => entry.Index == index);
            if (indexed is not null)
            {
                return EnsureImage(indexed, selector);
            }
        }

        var named = preview.Entries.FirstOrDefault(entry =>
            entry.Kind == WzDirectoryEntryKind.Image &&
            (string.Equals(entry.Path, selector, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(entry.Name, selector, StringComparison.OrdinalIgnoreCase)));
        if (named is not null)
        {
            return named;
        }

        throw new InvalidDataException($"Image entry not found: {selector}.");
    }

    private static WzDirectoryEntryPreview EnsureImage(WzDirectoryEntryPreview entry, string selector)
    {
        if (entry.Kind != WzDirectoryEntryKind.Image)
        {
            throw new InvalidDataException($"Selected entry is not an image: {selector}.");
        }

        return entry;
    }

    private static IReadOnlyList<ResourceInspectionMetadata> BuildDirectoryDocumentMetadata(WzDirectoryPreview preview)
    {
        var metadata = new List<ResourceInspectionMetadata>
        {
            new("entryCount", preview.EntryCount),
            new("totalEntryCount", preview.Entries.Count)
        };
        AddOptional(metadata, "stringKey", preview.StringEncryptionKind?.ToString().ToLowerInvariant());
        AddOptional(metadata, "wzVersion", preview.WzVersion);
        AddOptional(metadata, "hashVersion", preview.HashVersion);
        return metadata;
    }

    private static IReadOnlyList<ResourceInspectionMetadata> BuildDirectoryEntryMetadata(WzDirectoryEntryPreview entry)
    {
        var metadata = new List<ResourceInspectionMetadata>
        {
            new("index", entry.Index),
            new("nodeType", $"0x{entry.NodeType:X2}"),
            new("dataSize", entry.DataSize),
            new("checksum", entry.Checksum),
            new("hashOffsetPosition", entry.HashOffsetPosition),
            new("hashOffset", entry.HashOffset)
        };
        AddOptional(metadata, "offset", entry.Offset);
        return metadata;
    }

    private static IReadOnlyList<ResourceInspectionMetadata> BuildImageDocumentMetadata(
        WzDirectoryPreview directoryPreview,
        WzImagePreview preview,
        WzStringEncryptionKind stringKey)
    {
        var metadata = new List<ResourceInspectionMetadata>
        {
            new("selector", preview.Selector),
            new("stringKey", stringKey.ToString().ToLowerInvariant())
        };
        AddOptional(metadata, "wzVersion", directoryPreview.WzVersion);
        AddOptional(metadata, "hashVersion", directoryPreview.HashVersion);
        return metadata;
    }

    private static IReadOnlyList<ResourceInspectionMetadata> BuildImageRootMetadata(WzImagePreview preview)
    {
        var metadata = new List<ResourceInspectionMetadata>
        {
            new("selector", preview.Selector)
        };
        AddEntryMetadata(metadata, preview.Entry);
        AddOptional(metadata, "objectType", preview.ObjectType);
        AddOptional(metadata, "propertyCount", preview.PropertyCount);
        AddValueMetadata(metadata, preview.ObjectValue);
        return metadata;
    }

    private static IReadOnlyList<ResourceInspectionMetadata> BuildImagePropertyMetadata(
        WzImagePropertyPreviewEntry property)
    {
        var metadata = new List<ResourceInspectionMetadata>
        {
            new("index", property.Index),
            new("type", $"0x{property.Type:X2}"),
            new("kind", property.Kind)
        };
        AddOptional(metadata, "childCount", property.ChildCount);
        AddValueMetadata(metadata, property.Value);
        return metadata;
    }

    private static void AddEntryMetadata(List<ResourceInspectionMetadata> metadata, WzDirectoryEntryPreview? entry)
    {
        if (entry is null)
        {
            return;
        }

        metadata.Add(new ResourceInspectionMetadata("entryIndex", entry.Index));
        metadata.Add(new ResourceInspectionMetadata("entryNodeType", $"0x{entry.NodeType:X2}"));
        AddOptional(metadata, "entryName", entry.Name);
        AddOptional(metadata, "entryPath", entry.Path);
        metadata.Add(new ResourceInspectionMetadata("entryDataSize", entry.DataSize));
        metadata.Add(new ResourceInspectionMetadata("entryChecksum", entry.Checksum));
        metadata.Add(new ResourceInspectionMetadata("entryHashOffsetPosition", entry.HashOffsetPosition));
        metadata.Add(new ResourceInspectionMetadata("entryHashOffset", entry.HashOffset));
        AddOptional(metadata, "entryOffset", entry.Offset);
    }

    private static void AddValueMetadata(List<ResourceInspectionMetadata> metadata, object? value)
    {
        switch (value)
        {
            case WzImageCanvasPreview canvas:
                metadata.Add(new ResourceInspectionMetadata("valueType", "canvas"));
                metadata.Add(new ResourceInspectionMetadata("width", canvas.Width));
                metadata.Add(new ResourceInspectionMetadata("height", canvas.Height));
                metadata.Add(new ResourceInspectionMetadata("format", canvas.Format));
                metadata.Add(new ResourceInspectionMetadata("scale", canvas.Scale));
                metadata.Add(new ResourceInspectionMetadata("actualScale", canvas.ActualScale));
                metadata.Add(new ResourceInspectionMetadata("pages", canvas.Pages));
                metadata.Add(new ResourceInspectionMetadata("actualPages", canvas.ActualPages));
                metadata.Add(new ResourceInspectionMetadata("unknown1", canvas.Unknown1));
                metadata.Add(new ResourceInspectionMetadata("dataOffset", canvas.DataOffset));
                metadata.Add(new ResourceInspectionMetadata("dataLength", canvas.DataLength));
                metadata.Add(new ResourceInspectionMetadata("compressionKind", canvas.CompressionKind.ToString()));
                AddOptional(metadata, "uncompressedDataLength", canvas.UncompressedDataLength);
                break;
            case WzImageRawDataPreview rawData:
                metadata.Add(new ResourceInspectionMetadata("valueType", "rawData"));
                metadata.Add(new ResourceInspectionMetadata("version", rawData.Version));
                metadata.Add(new ResourceInspectionMetadata("dataOffset", rawData.DataOffset));
                metadata.Add(new ResourceInspectionMetadata("dataLength", rawData.DataLength));
                break;
            case WzImageVideoPreview video:
                metadata.Add(new ResourceInspectionMetadata("valueType", "video"));
                metadata.Add(new ResourceInspectionMetadata("unknown", video.Unknown));
                metadata.Add(new ResourceInspectionMetadata("dataOffset", video.DataOffset));
                metadata.Add(new ResourceInspectionMetadata("dataLength", video.DataLength));
                break;
            case WzImageSoundPreview sound:
                metadata.Add(new ResourceInspectionMetadata("valueType", "sound"));
                metadata.Add(new ResourceInspectionMetadata("version", sound.Version));
                metadata.Add(new ResourceInspectionMetadata("duration", sound.Duration));
                metadata.Add(new ResourceInspectionMetadata("soundDeclaration", sound.SoundDeclaration));
                metadata.Add(new ResourceInspectionMetadata("majorType", sound.MajorType));
                metadata.Add(new ResourceInspectionMetadata("subType", sound.SubType));
                metadata.Add(new ResourceInspectionMetadata("fixedSizeSamples", sound.FixedSizeSamples));
                metadata.Add(new ResourceInspectionMetadata("temporalCompression", sound.TemporalCompression));
                metadata.Add(new ResourceInspectionMetadata("formatType", sound.FormatType));
                AddOptional(metadata, "formatExtraLength", sound.FormatExtraLength);
                metadata.Add(new ResourceInspectionMetadata("dataOffset", sound.DataOffset));
                metadata.Add(new ResourceInspectionMetadata("dataLength", sound.DataLength));
                break;
            case WzImageLuaPreview lua:
                metadata.Add(new ResourceInspectionMetadata("valueType", "lua"));
                metadata.Add(new ResourceInspectionMetadata("scriptLength", lua.ScriptLength));
                metadata.Add(new ResourceInspectionMetadata("preview", lua.Preview));
                break;
            case WzImageVectorPreview vector:
                metadata.Add(new ResourceInspectionMetadata("valueType", "vector"));
                metadata.Add(new ResourceInspectionMetadata("x", vector.X));
                metadata.Add(new ResourceInspectionMetadata("y", vector.Y));
                break;
            case WzImageConvexPreview convex:
                metadata.Add(new ResourceInspectionMetadata("valueType", "convex"));
                metadata.Add(new ResourceInspectionMetadata("pointCount", convex.Points.Count));
                break;
            case not null:
                metadata.Add(new ResourceInspectionMetadata("valueType", value.GetType().Name));
                break;
        }
    }

    private static IReadOnlyList<ResourceInspectionDiagnostic>? BuildValueDiagnostics(object? value, string? path)
    {
        var message = value switch
        {
            WzImageCanvasPreview => "Canvas pixel decoding is not implemented.",
            WzImageRawDataPreview => "RawData payload decoding is not implemented.",
            WzImageVideoPreview => "Video payload decoding is not implemented.",
            WzImageSoundPreview => "Audio payload decoding is not implemented.",
            WzImageLuaPreview => "Full Lua script export is not implemented.",
            _ => null
        };

        return message is null
            ? null
            : [new ResourceInspectionDiagnostic("info", message, path)];
    }

    private static void AddOptional(List<ResourceInspectionMetadata> metadata, string name, object? value)
    {
        if (value is not null)
        {
            metadata.Add(new ResourceInspectionMetadata(name, value));
        }
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

        public IReadOnlyList<ResourceInspectionMetadata>? DebugMetadata { get; private set; }

        public void AddPath(
            string[] parts,
            int index,
            string leafKind,
            IReadOnlyList<ResourceInspectionMetadata>? debugMetadata)
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
                child.DebugMetadata = debugMetadata;
                return;
            }

            child.AddPath(parts, index + 1, leafKind, debugMetadata);
        }

        public ResourceInspectionNode ToNode()
        {
            return new ResourceInspectionNode(
                Name,
                Kind,
                Path,
                DisplayValue,
                children.Select(child => child.ToNode()).ToArray(),
                DebugMetadata);
        }
    }
}
