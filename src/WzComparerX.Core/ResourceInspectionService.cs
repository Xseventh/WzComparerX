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
        var inspection = await WzImageInspectionLoader.ReadDirectoryAsync(path, options.StringKey, cancellationToken);
        if (!inspection.Header.IsValid)
        {
            throw new InvalidDataException($"Invalid WZ package: {inspection.Header.SourcePath}.");
        }

        var root = await BuildDirectoryRootAsync(
            inspection,
            options,
            includeSplitPackageLinks: true,
            cancellationToken);
        return new ResourceInspectionDocument(
            inspection.Header.SourcePath,
            inspection.Header.Format.ToString().ToLowerInvariant(),
            root,
            options.IncludeDebugMetadata ? BuildDirectoryDocumentMetadata(inspection) : null);
    }

    private static async Task<ResourceInspectionDocument> InspectImageAsync(
        string path,
        string selector,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken)
    {
        var context = await WzImageInspectionLoader.LoadAsync(
            path,
            selector,
            options.StringKey,
            options.MaxPropertyDepth,
            cancellationToken);
        var directoryInspection = context.DirectoryInspection;
        var inspection = context.ImageInspection;

        var root = ProjectImageInspection(inspection, options.IncludeDebugMetadata);
        return new ResourceInspectionDocument(
            inspection.Header.SourcePath,
            inspection.Header.Format.ToString().ToLowerInvariant(),
            root,
            options.IncludeDebugMetadata ? BuildImageDocumentMetadata(directoryInspection, inspection, context.StringKey) : null,
            root.Diagnostics);
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

    private static async Task<ResourceInspectionNode> BuildDirectoryRootAsync(
        WzDirectoryInspection inspection,
        ResourceInspectionOptions options,
        bool includeSplitPackageLinks,
        CancellationToken cancellationToken,
        string? rootPath = null)
    {
        var rootName = Path.GetFileName(inspection.Header.SourcePath);
        if (string.IsNullOrWhiteSpace(rootName))
        {
            rootName = inspection.Header.SourcePath;
        }

        var builder = new InspectionNodeBuilder(
            rootName,
            "package",
            rootPath ?? rootName,
            inspection.Header.Format.ToString().ToLowerInvariant());
        foreach (var entry in inspection.Entries)
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
                options.IncludeDebugMetadata ? BuildDirectoryEntryMetadata(entry) : null);
        }

        if (includeSplitPackageLinks)
        {
            await AddSplitPackageLinksAsync(builder, inspection, options, cancellationToken);
        }

        return builder.ToNode();
    }

    private static ResourceInspectionNode ProjectImageInspection(WzImageInspection inspection, bool includeDebugMetadata)
    {
        var name = inspection.Entry?.Path ?? inspection.Entry?.Name ?? inspection.Selector;
        var displayValue = inspection.ObjectValue is WzImageTextInspection
            ? inspection.ObjectType
            : inspection.ObjectValue is not null
            ? FormatObject(inspection.ObjectValue)
            : inspection.ObjectType;
        var children = inspection.Properties?
            .Select(property => ProjectImageProperty(property, includeDebugMetadata))
            .ToArray() ?? Array.Empty<ResourceInspectionNode>();
        var diagnostics = includeDebugMetadata ? BuildValueDiagnostics(inspection.ObjectValue, name) : null;

        return new ResourceInspectionNode(
            name,
            "image",
            name,
            displayValue,
            children,
            includeDebugMetadata ? BuildImageRootMetadata(inspection) : null,
            diagnostics);
    }

    private static ResourceInspectionNode ProjectImageProperty(
        WzImagePropertyInspectionEntry property,
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

    private static IReadOnlyList<ResourceInspectionMetadata> BuildDirectoryDocumentMetadata(WzDirectoryInspection inspection)
    {
        var metadata = new List<ResourceInspectionMetadata>
        {
            new("entryCount", inspection.EntryCount),
            new("totalEntryCount", inspection.Entries.Count)
        };
        AddOptional(metadata, "stringKey", inspection.StringEncryptionKind?.ToString().ToLowerInvariant());
        AddOptional(metadata, "wzVersion", inspection.WzVersion);
        AddOptional(metadata, "hashVersion", inspection.HashVersion);
        return metadata;
    }

    private static IReadOnlyList<ResourceInspectionMetadata> BuildDirectoryEntryMetadata(WzDirectoryEntryInspection entry)
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

    private static async Task AddSplitPackageLinksAsync(
        InspectionNodeBuilder builder,
        WzDirectoryInspection inspection,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken)
    {
        foreach (var entry in inspection.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!CanResolveSplitPackageLink(builder, entry))
            {
                continue;
            }

            var entryPath = entry.Path ?? entry.Name;
            if (string.IsNullOrWhiteSpace(entryPath))
            {
                continue;
            }

            foreach (var packagePath in ResolveSplitPackagePaths(inspection.Header.SourcePath, entryPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var linkedNode = await TryBuildLinkedPackageNodeAsync(packagePath, options, cancellationToken);
                if (linkedNode is not null)
                {
                    builder.AddLinkedChild(
                        entryPath.Split('/', StringSplitOptions.RemoveEmptyEntries),
                        linkedNode);
                }
            }
        }
    }

    private static bool CanResolveSplitPackageLink(InspectionNodeBuilder builder, WzDirectoryEntryInspection entry)
    {
        if (entry.Kind != WzDirectoryEntryKind.Directory ||
            entry.DataSize != 0 ||
            string.IsNullOrWhiteSpace(entry.Path) ||
            builder.HasChildren(entry.Path.Split('/', StringSplitOptions.RemoveEmptyEntries)))
        {
            return false;
        }

        return entry.Path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .All(part => part.IndexOfAny(Path.GetInvalidFileNameChars()) < 0);
    }

    private static IEnumerable<string> ResolveSplitPackagePaths(string sourcePath, string entryPath)
    {
        var sourceDirectory = Path.GetDirectoryName(sourcePath);
        var workspaceDirectory = sourceDirectory is null ? null : Directory.GetParent(sourceDirectory)?.FullName;
        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            yield break;
        }

        var parts = entryPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            yield break;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entryDirectory in ResolveSplitPackageDirectories(sourceDirectory, workspaceDirectory, parts))
        {
            var packageStem = parts[^1];
            foreach (var candidate in EnumerateSplitPackageFiles(entryDirectory, packageStem))
            {
                if (!PathsEqual(candidate, sourcePath) && seen.Add(Path.GetFullPath(candidate)))
                {
                    yield return candidate;
                }
            }
        }
    }

    private static IEnumerable<string> ResolveSplitPackageDirectories(
        string sourceDirectory,
        string? workspaceDirectory,
        string[] parts)
    {
        var relativePath = Path.Combine(parts);
        var currentPackageRelativeDirectory = Path.Combine(sourceDirectory, relativePath);
        if (Directory.Exists(currentPackageRelativeDirectory))
        {
            yield return currentPackageRelativeDirectory;
        }

        if (!string.IsNullOrWhiteSpace(workspaceDirectory))
        {
            var workspaceRelativeDirectory = Path.Combine(workspaceDirectory, relativePath);
            if (Directory.Exists(workspaceRelativeDirectory) &&
                !PathsEqual(workspaceRelativeDirectory, currentPackageRelativeDirectory))
            {
                yield return workspaceRelativeDirectory;
            }
        }
    }

    private static IEnumerable<string> EnumerateSplitPackageFiles(string entryDirectory, string packageStem)
    {
        var primaryPackagePath = Path.Combine(entryDirectory, packageStem + ".wz");
        if (File.Exists(primaryPackagePath))
        {
            yield return primaryPackagePath;
        }

        foreach (var candidate in Directory.EnumerateFiles(entryDirectory, packageStem + "_*.wz").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            yield return candidate;
        }
    }

    private static async Task<ResourceInspectionNode?> TryBuildLinkedPackageNodeAsync(
        string path,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var inspection = await WzImageInspectionLoader.ReadDirectoryAsync(path, options.StringKey, cancellationToken);
            if (!inspection.Header.IsValid)
            {
                return null;
            }

            return await BuildDirectoryRootAsync(
                inspection,
                options,
                includeSplitPackageLinks: false,
                cancellationToken,
                rootPath: path);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(left),
                Path.GetFullPath(right),
                StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }
    }

    private static IReadOnlyList<ResourceInspectionMetadata> BuildImageDocumentMetadata(
        WzDirectoryInspection directoryInspection,
        WzImageInspection inspection,
        WzStringEncryptionKind stringKey)
    {
        var metadata = new List<ResourceInspectionMetadata>
        {
            new("selector", inspection.Selector),
            new("stringKey", stringKey.ToString().ToLowerInvariant())
        };
        AddOptional(metadata, "wzVersion", directoryInspection.WzVersion);
        AddOptional(metadata, "hashVersion", directoryInspection.HashVersion);
        return metadata;
    }

    private static IReadOnlyList<ResourceInspectionMetadata> BuildImageRootMetadata(WzImageInspection inspection)
    {
        var metadata = new List<ResourceInspectionMetadata>
        {
            new("selector", inspection.Selector)
        };
        AddEntryMetadata(metadata, inspection.Entry);
        AddOptional(metadata, "objectType", inspection.ObjectType);
        AddOptional(metadata, "propertyCount", inspection.PropertyCount);
        AddValueMetadata(metadata, inspection.ObjectValue);
        return metadata;
    }

    private static IReadOnlyList<ResourceInspectionMetadata> BuildImagePropertyMetadata(
        WzImagePropertyInspectionEntry property)
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

    private static void AddEntryMetadata(List<ResourceInspectionMetadata> metadata, WzDirectoryEntryInspection? entry)
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
            case WzImageCanvasInspection canvas:
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
            case WzImageRawDataInspection rawData:
                metadata.Add(new ResourceInspectionMetadata("valueType", "rawData"));
                metadata.Add(new ResourceInspectionMetadata("version", rawData.Version));
                metadata.Add(new ResourceInspectionMetadata("dataOffset", rawData.DataOffset));
                metadata.Add(new ResourceInspectionMetadata("dataLength", rawData.DataLength));
                break;
            case WzImageVideoInspection video:
                metadata.Add(new ResourceInspectionMetadata("valueType", "video"));
                metadata.Add(new ResourceInspectionMetadata("unknown", video.Unknown));
                metadata.Add(new ResourceInspectionMetadata("dataOffset", video.DataOffset));
                metadata.Add(new ResourceInspectionMetadata("dataLength", video.DataLength));
                break;
            case WzImageSoundInspection sound:
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
            case WzImageLuaInspection lua:
                metadata.Add(new ResourceInspectionMetadata("valueType", "lua"));
                metadata.Add(new ResourceInspectionMetadata("scriptLength", lua.ScriptLength));
                metadata.Add(new ResourceInspectionMetadata("snippet", lua.Snippet));
                break;
            case WzImageTextInspection text:
                metadata.Add(new ResourceInspectionMetadata("valueType", "textImage"));
                metadata.Add(new ResourceInspectionMetadata("textFormat", text.Format));
                metadata.Add(new ResourceInspectionMetadata("textLength", text.TextLength));
                break;
            case WzImageVectorInspection vector:
                metadata.Add(new ResourceInspectionMetadata("valueType", "vector"));
                metadata.Add(new ResourceInspectionMetadata("x", vector.X));
                metadata.Add(new ResourceInspectionMetadata("y", vector.Y));
                break;
            case WzImageConvexInspection convex:
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
        var diagnostic = value switch
        {
            WzImageCanvasInspection => ResourceInspectionDiagnostics.CanvasPixelDecodingUnsupported(path),
            WzImageRawDataInspection => ResourceInspectionDiagnostics.RawDataPayloadDecodingUnsupported(path),
            WzImageVideoInspection => ResourceInspectionDiagnostics.VideoPayloadDecodingUnsupported(path),
            WzImageSoundInspection => ResourceInspectionDiagnostics.AudioPayloadDecodingUnsupported(path),
            _ => null
        };

        return diagnostic is null
            ? null
            : [diagnostic];
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

        public bool HasChildren(string[] parts)
        {
            var node = Find(parts, 0);
            return node is not null && node.children.Count > 0;
        }

        public void AddLinkedChild(string[] parts, ResourceInspectionNode child)
        {
            var node = Find(parts, 0);
            node?.children.Add(FromNode(child));
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

        private InspectionNodeBuilder? Find(string[] parts, int index)
        {
            if (index >= parts.Length)
            {
                return this;
            }

            var child = children.FirstOrDefault(candidate => candidate.Name == parts[index]);
            return child?.Find(parts, index + 1);
        }

        private static InspectionNodeBuilder FromNode(ResourceInspectionNode node)
        {
            var builder = new InspectionNodeBuilder(node.Name, node.Kind, node.Path, node.DisplayValue)
            {
                DebugMetadata = node.DebugMetadata
            };
            foreach (var child in node.Children)
            {
                builder.children.Add(FromNode(child));
            }

            return builder;
        }
    }
}
