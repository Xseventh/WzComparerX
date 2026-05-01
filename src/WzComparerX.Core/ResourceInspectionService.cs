using System.Globalization;
using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class ResourceInspectionService
{
    public Task<ResourceInspectionDocument> InspectAsync(
        string path,
        string? selector = null,
        WzStringEncryptionKind? stringKey = WzStringEncryptionKind.None,
        int maxPropertyDepth = WzImageInspectionReader.FullPropertyInspectionDepth,
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
        var header = await new WzPackageHeaderReader().ReadAsync(path, cancellationToken);
        if (header is { IsValid: true, Format: WzPackageFormat.Pkg2 })
        {
            return BuildUnsupportedPkg2Document(header, options);
        }

        var group = await WzPackageGroupInspectionLoader.LoadAsync(path, options.StringKey, cancellationToken);
        var inspection = group.Entry;
        if (!inspection.Header.IsValid)
        {
            throw new InvalidDataException($"Invalid WZ package: {inspection.Header.SourcePath}.");
        }

        var root = await BuildPackageGroupRootAsync(
            group,
            options,
            includeSplitPackageLinks: true,
            cancellationToken,
            splitPackageAncestors: WzSplitPackageLinkResolver.CreateAncestors(inspection.Header.SourcePath));
        return new ResourceInspectionDocument(
            inspection.Header.SourcePath,
            inspection.Header.Format.ToString().ToLowerInvariant(),
            root,
            options.IncludeDebugMetadata ? BuildDirectoryDocumentMetadata(group) : null);
    }

    private static async Task<ResourceInspectionDocument> InspectImageAsync(
        string path,
        string selector,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken)
    {
        var header = await new WzPackageHeaderReader().ReadAsync(path, cancellationToken);
        if (header is { IsValid: true, Format: WzPackageFormat.Pkg2 })
        {
            throw new ResourceInspectionException(ResourceInspectionDiagnostics.Pkg2DirectoryInspectionUnsupported(header.SourcePath));
        }

        var context = await WzImageInspectionLoader.LoadAsync(
            path,
            selector,
            options.StringKey,
            options.MaxPropertyDepth,
            cancellationToken);
        var directoryInspection = context.DirectoryInspection;
        var inspection = context.ImageInspection;

        var root = await ProjectImageInspectionAsync(inspection, options, cancellationToken);
        return new ResourceInspectionDocument(
            inspection.Header.SourcePath,
            inspection.Header.Format.ToString().ToLowerInvariant(),
            root,
            options.IncludeDebugMetadata ? BuildImageDocumentMetadata(directoryInspection, inspection, context.StringKey) : null,
            root.Diagnostics);
    }

    private static ResourceInspectionDocument BuildUnsupportedPkg2Document(
        WzPackageHeader header,
        ResourceInspectionOptions options)
    {
        var rootName = Path.GetFileName(header.SourcePath);
        if (string.IsNullOrWhiteSpace(rootName))
        {
            rootName = header.SourcePath;
        }

        var diagnostic = ResourceInspectionDiagnostics.Pkg2DirectoryInspectionUnsupported(header.SourcePath);
        var root = new ResourceInspectionNode(
            rootName,
            "package",
            rootName,
            header.Format.ToString().ToLowerInvariant(),
            Identity: new ResourceInspectionIdentity(PackagePath: header.SourcePath));
        return new ResourceInspectionDocument(
            header.SourcePath,
            header.Format.ToString().ToLowerInvariant(),
            root,
            options.IncludeDebugMetadata ? BuildPackageHeaderMetadata(header) : null,
            [diagnostic]);
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
        IReadOnlySet<string>? splitPackageAncestors = null,
        string? rootPath = null)
    {
        var group = new WzPackageGroupInspection(
            inspection,
            [new WzPackageGroupMemberInspection(inspection, IsEntry: true)],
            []);
        return await BuildPackageGroupRootAsync(
            group,
            options,
            includeSplitPackageLinks,
            cancellationToken,
            splitPackageAncestors,
            rootPath);
    }

    private static async Task<ResourceInspectionNode> BuildPackageGroupRootAsync(
        WzPackageGroupInspection group,
        ResourceInspectionOptions options,
        bool includeSplitPackageLinks,
        CancellationToken cancellationToken,
        IReadOnlySet<string>? splitPackageAncestors = null,
        string? rootPath = null)
    {
        var inspection = group.Entry;
        var rootName = Path.GetFileName(inspection.Header.SourcePath);
        if (string.IsNullOrWhiteSpace(rootName))
        {
            rootName = inspection.Header.SourcePath;
        }

        var builder = new InspectionNodeBuilder(
            rootName,
            "package",
            rootPath ?? rootName,
            inspection.Header.Format.ToString().ToLowerInvariant(),
            new ResourceInspectionIdentity(PackagePath: inspection.Header.SourcePath),
            options.IncludeDebugMetadata ? group.Diagnostics : null);

        foreach (var member in group.Members)
        {
            foreach (var entry in member.Inspection.Entries)
            {
                var entryPath = entry.Path ?? entry.Name ?? entry.Index.ToString(CultureInfo.InvariantCulture);
                var parts = entryPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    parts = [entryPath];
                }

                var leafPath = !member.IsEntry && entry.Kind == WzDirectoryEntryKind.Image
                    ? CombinePackageSelector(member.SourcePath, entryPath)
                    : null;

                builder.AddPath(
                    parts,
                    0,
                    entry.Kind.ToString().ToLowerInvariant(),
                    options.IncludeDebugMetadata ? BuildDirectoryEntryMetadata(entry, member.IsEntry ? null : member.SourcePath) : null,
                    leafPath,
                    BuildDirectoryEntryIdentity(entry, member.SourcePath));
            }
        }

        if (includeSplitPackageLinks)
        {
            await AddSplitPackageLinksAsync(
                builder,
                inspection,
                options,
                cancellationToken,
                splitPackageAncestors);
        }

        return builder.ToNode();
    }

    private static async Task<ResourceInspectionNode> ProjectImageInspectionAsync(
        WzImageInspection inspection,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken)
    {
        var name = inspection.Entry?.Path ?? inspection.Entry?.Name ?? inspection.Selector;
        var displayValue = inspection.ObjectValue is WzImageTextInspection
            ? inspection.ObjectType
            : inspection.ObjectValue is not null
            ? FormatObject(inspection.ObjectValue)
            : inspection.ObjectType;
        var children = new List<ResourceInspectionNode>();
        foreach (var property in inspection.Properties ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();
            children.Add(await ProjectImagePropertyAsync(
                property,
                inspection,
                inspection.Header.SourcePath,
                inspection.Selector,
                options,
                cancellationToken));
        }

        var diagnostics = options.IncludeDebugMetadata ? BuildValueDiagnostics(inspection.ObjectValue, name) : null;

        return new ResourceInspectionNode(
            name,
            "image",
            name,
            displayValue,
            children,
            options.IncludeDebugMetadata ? BuildImageRootMetadata(inspection) : null,
            diagnostics,
            new ResourceInspectionIdentity(
                PackagePath: inspection.Header.SourcePath,
                ImageSelector: inspection.Selector));
    }

    private static async Task<ResourceInspectionNode> ProjectImagePropertyAsync(
        WzImagePropertyInspectionEntry property,
        WzImageInspection inspection,
        string packagePath,
        string imageSelector,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken)
    {
        var name = property.Name ?? property.Index.ToString(CultureInfo.InvariantCulture);
        var children = new List<ResourceInspectionNode>();
        foreach (var child in property.Children ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();
            children.Add(await ProjectImagePropertyAsync(
                child,
                inspection,
                packagePath,
                imageSelector,
                options,
                cancellationToken));
        }

        var value = property.Value is not null ? FormatObject(property.Value) : null;
        var linkedTarget = ResourceInspectionLinkResolver.GetLinkedTarget(property);
        var resolvedLinkedTarget = options.IncludeDebugMetadata && linkedTarget is not null
            ? await ResourceInspectionLinkResolver.ResolveAsync(
                packagePath,
                inspection,
                property,
                options.StringKey,
                cancellationToken)
            : null;

        return new ResourceInspectionNode(
            name,
            property.Kind,
            property.Path,
            value,
            children,
            options.IncludeDebugMetadata ? BuildImagePropertyMetadata(property, resolvedLinkedTarget) : null,
            options.IncludeDebugMetadata ? BuildValueDiagnostics(property.Value, property.Path) : null,
            new ResourceInspectionIdentity(
                PackagePath: packagePath,
                ImageSelector: imageSelector,
                ValuePath: property.Path,
                LinkedTarget: linkedTarget,
                ResolvedLinkedTarget: resolvedLinkedTarget));
    }

    private static IReadOnlyList<ResourceInspectionMetadata> BuildDirectoryDocumentMetadata(WzDirectoryInspection inspection)
    {
        return BuildDirectoryDocumentMetadata(
            new WzPackageGroupInspection(
                inspection,
                [new WzPackageGroupMemberInspection(inspection, IsEntry: true)],
                []));
    }

    private static IReadOnlyList<ResourceInspectionMetadata> BuildDirectoryDocumentMetadata(WzPackageGroupInspection group)
    {
        var inspection = group.Entry;
        var metadata = new List<ResourceInspectionMetadata>
        {
            new("entryCount", inspection.EntryCount),
            new("totalEntryCount", group.Members.Sum(member => member.Inspection.Entries.Count)),
            new("packageGroupCount", group.Members.Count)
        };
        AddOptional(metadata, "stringKey", inspection.StringEncryptionKind?.ToString().ToLowerInvariant());
        AddOptional(metadata, "wzVersion", inspection.WzVersion);
        AddOptional(metadata, "hashVersion", inspection.HashVersion);
        return metadata;
    }

    private static IReadOnlyList<ResourceInspectionMetadata> BuildPackageHeaderMetadata(WzPackageHeader header)
    {
        var metadata = new List<ResourceInspectionMetadata>
        {
            new("signature", header.Signature),
            new("valid", header.IsValid),
            new("headerSize", header.HeaderSize),
            new("dataSize", header.DataSize),
            new("fileSize", header.FileSize),
            new("directoryStartPosition", header.DirectoryStartPosition)
        };
        AddOptional(metadata, "hash1", header.Hash1);
        AddOptional(metadata, "hash2", header.Hash2);
        return metadata;
    }

    private static IReadOnlyList<ResourceInspectionMetadata> BuildDirectoryEntryMetadata(
        WzDirectoryEntryInspection entry,
        string? sourcePath = null)
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
        AddOptional(metadata, "sourcePath", sourcePath);
        return metadata;
    }

    private static ResourceInspectionIdentity BuildDirectoryEntryIdentity(
        WzDirectoryEntryInspection entry,
        string packagePath)
    {
        return new ResourceInspectionIdentity(
            PackagePath: packagePath,
            ImageSelector: entry.Kind == WzDirectoryEntryKind.Image
                ? entry.Path ?? entry.Name
                : null);
    }

    private static async Task AddSplitPackageLinksAsync(
        InspectionNodeBuilder builder,
        WzDirectoryInspection inspection,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken,
        IReadOnlySet<string>? splitPackageAncestors)
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

            var resolvedAny = false;
            var candidateCount = 0;
            foreach (var packagePath in WzSplitPackageLinkResolver.ResolvePaths(inspection.Header.SourcePath, entryPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                candidateCount++;
                var linkedNode = await TryBuildLinkedPackageNodeAsync(
                    packagePath,
                    options,
                    cancellationToken,
                    splitPackageAncestors);
                if (linkedNode is not null)
                {
                    resolvedAny = true;
                    builder.AddLinkedChild(
                        entryPath.Split('/', StringSplitOptions.RemoveEmptyEntries),
                        linkedNode);
                }
            }

            if (!resolvedAny && candidateCount > 0 && options.IncludeDebugMetadata)
            {
                builder.AddDiagnostic(
                    entryPath.Split('/', StringSplitOptions.RemoveEmptyEntries),
                    ResourceInspectionDiagnostics.SplitPackageLinkUnresolved(entryPath));
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

    private static async Task<ResourceInspectionNode?> TryBuildLinkedPackageNodeAsync(
        string path,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken,
        IReadOnlySet<string>? splitPackageAncestors)
    {
        var fullPath = WzSplitPackageLinkResolver.NormalizePath(path);
        if (splitPackageAncestors?.Contains(fullPath) == true)
        {
            return null;
        }

        try
        {
            var group = await WzPackageGroupInspectionLoader.LoadAsync(path, options.StringKey, cancellationToken);
            if (!group.Entry.Header.IsValid)
            {
                return null;
            }

            return await BuildPackageGroupRootAsync(
                group,
                options,
                includeSplitPackageLinks: true,
                cancellationToken,
                splitPackageAncestors: WzSplitPackageLinkResolver.AddAncestor(splitPackageAncestors, fullPath),
                rootPath: path);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException or UnauthorizedAccessException)
        {
            return null;
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
        WzImagePropertyInspectionEntry property,
        ResourceInspectionResolvedLinkTarget? resolvedLinkedTarget)
    {
        var metadata = new List<ResourceInspectionMetadata>
        {
            new("index", property.Index),
            new("type", $"0x{property.Type:X2}"),
            new("kind", property.Kind)
        };
        AddOptional(metadata, "childCount", property.ChildCount);
        AddOptional(metadata, "linkKind", GetLinkKind(property));
        AddOptional(metadata, "linkedTarget", GetLinkedTarget(property));
        AddResolvedLinkMetadata(metadata, resolvedLinkedTarget);
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

    private static void AddResolvedLinkMetadata(
        List<ResourceInspectionMetadata> metadata,
        ResourceInspectionResolvedLinkTarget? resolvedLinkedTarget)
    {
        if (resolvedLinkedTarget is null)
        {
            return;
        }

        metadata.Add(new ResourceInspectionMetadata("resolvedLinkedPackagePath", resolvedLinkedTarget.PackagePath));
        metadata.Add(new ResourceInspectionMetadata("resolvedLinkedImageSelector", resolvedLinkedTarget.ImageSelector));
        AddOptional(metadata, "resolvedLinkedValuePath", resolvedLinkedTarget.ValuePath);
    }

    private static IReadOnlyList<ResourceInspectionDiagnostic>? BuildValueDiagnostics(object? value, string? path)
    {
        var diagnostic = value switch
        {
            WzImageCanvasInspection => ResourceInspectionDiagnostics.CanvasPixelDecodingPartial(path),
            WzImageRawDataInspection => ResourceInspectionDiagnostics.RawDataPayloadDecodingUnsupported(path),
            WzImageVideoInspection => ResourceInspectionDiagnostics.VideoPayloadDecodingUnsupported(path),
            WzImageSoundInspection => ResourceInspectionDiagnostics.AudioPayloadDecodingUnsupported(path),
            _ => null
        };

        return diagnostic is null
            ? null
            : [diagnostic];
    }

    private static string? GetLinkKind(WzImagePropertyInspectionEntry property)
    {
        return ResourceInspectionLinkResolver.GetLinkKind(property);
    }

    private static string? GetLinkedTarget(WzImagePropertyInspectionEntry property)
    {
        return ResourceInspectionLinkResolver.GetLinkedTarget(property);
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

    private static string CombinePackageSelector(string packagePath, string selector)
    {
        return $"{packagePath}/{selector.Replace('\\', '/')}";
    }

    private sealed class InspectionNodeBuilder
    {
        private readonly List<InspectionNodeBuilder> children = [];

        public InspectionNodeBuilder(string name, string kind, string? path, string? displayValue = null)
            : this(name, kind, path, displayValue, identity: null)
        {
        }

        public InspectionNodeBuilder(
            string name,
            string kind,
            string? path,
            string? displayValue,
            ResourceInspectionIdentity? identity,
            IReadOnlyList<ResourceInspectionDiagnostic>? diagnostics = null)
        {
            Name = name;
            Kind = kind;
            Path = path;
            DisplayValue = displayValue;
            Identity = identity;
            Diagnostics = diagnostics;
        }

        public string Name { get; }

        public string Kind { get; private set; }

        public string? Path { get; private set; }

        public string? DisplayValue { get; }

        public IReadOnlyList<ResourceInspectionMetadata>? DebugMetadata { get; private set; }

        public ResourceInspectionIdentity? Identity { get; private set; }

        public IReadOnlyList<ResourceInspectionDiagnostic>? Diagnostics { get; private set; }

        public void AddPath(
            string[] parts,
            int index,
            string leafKind,
            IReadOnlyList<ResourceInspectionMetadata>? debugMetadata,
            string? leafPath = null,
            ResourceInspectionIdentity? identity = null)
        {
            var name = parts[index];
            var child = children.FirstOrDefault(candidate => candidate.Name == name);
            if (child is null)
            {
                var path = index == parts.Length - 1 && leafPath is not null
                    ? leafPath
                    : string.IsNullOrEmpty(Path) ? name : $"{Path}/{name}";
                var kind = index == parts.Length - 1 ? leafKind : "directory";
                child = new InspectionNodeBuilder(name, kind, path);
                children.Add(child);
            }

            if (index == parts.Length - 1)
            {
                child.Kind = leafKind;
                child.DebugMetadata = debugMetadata;
                child.Identity = identity;
                if (leafPath is not null)
                {
                    child.Path = leafPath;
                }
                return;
            }

            child.AddPath(parts, index + 1, leafKind, debugMetadata, leafPath, identity);
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

        public void AddDiagnostic(string[] parts, ResourceInspectionDiagnostic diagnostic)
        {
            var node = Find(parts, 0);
            if (node is null)
            {
                return;
            }

            var diagnostics = node.Diagnostics?.ToList() ?? [];
            diagnostics.Add(diagnostic);
            node.Diagnostics = diagnostics;
        }

        public ResourceInspectionNode ToNode()
        {
            return new ResourceInspectionNode(
                Name,
                Kind,
                Path,
                DisplayValue,
                children.Select(child => child.ToNode()).ToArray(),
                DebugMetadata,
                Diagnostics,
                Identity: Identity);
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
                DebugMetadata = node.DebugMetadata,
                Identity = node.Identity,
                Diagnostics = node.Diagnostics
            };
            foreach (var child in node.Children)
            {
                builder.children.Add(FromNode(child));
            }

            return builder;
        }
    }
}
