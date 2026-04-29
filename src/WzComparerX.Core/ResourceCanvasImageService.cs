using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class ResourceCanvasImageService
{
    public async Task<ResourceCanvasImageDocument> LoadAsync(
        string path,
        string selector,
        string? valueSelector,
        ResourceInspectionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await LoadCoreAsync(
            path,
            selector,
            valueSelector,
            useFirstCanvasFallback: false,
            options,
            cancellationToken);
    }

    public async Task<ResourceCanvasImageDocument> LoadFirstAsync(
        string path,
        string selector,
        ResourceInspectionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await LoadCoreAsync(
            path,
            selector,
            valueSelector: null,
            useFirstCanvasFallback: true,
            options,
            cancellationToken);
    }

    private static async Task<ResourceCanvasImageDocument> LoadCoreAsync(
        string path,
        string selector,
        string? valueSelector,
        bool useFirstCanvasFallback,
        ResourceInspectionOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        options ??= new ResourceInspectionOptions();
        var context = await WzImageInspectionLoader.LoadAsync(
            path,
            selector,
            options.StringKey,
            options.MaxPropertyDepth,
            cancellationToken);
        var target = await SelectCanvasAsync(
            path,
            context.ImageInspection,
            selector,
            valueSelector,
            useFirstCanvasFallback,
            options,
            cancellationToken);
        ValidateCanvas(target.Value, target.Path);

        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = File.OpenRead(target.SourcePath);
        WzImageCanvasBitmap bitmap;
        try
        {
            bitmap = new WzImageCanvasPayloadDecoder().Decode(stream, target.Value);
        }
        catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException or NotSupportedException)
        {
            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewDecodeFailed(target.Path));
        }

        var pixels = ConvertToBgra8888(bitmap, target.Path);
        return new ResourceCanvasImageDocument(
            target.SourcePath,
            target.Selector,
            target.Path,
            bitmap.Width,
            bitmap.Height,
            bitmap.Format,
            "bgra8888",
            pixels);
    }

    private static void ValidateCanvas(WzImageCanvasInspection canvas, string? path)
    {
        if (canvas.CompressionKind != WzImageCanvasCompressionKind.Zlib)
        {
            throw new ResourceCanvasImageException(
                ResourceInspectionDiagnostics.CanvasPreviewCompressionUnsupported(canvas.CompressionKind, path));
        }

        if (canvas.Format is not 1 and not 2)
        {
            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewFormatUnsupported(canvas.Format, path));
        }

        if (canvas.ActualScale != 1)
        {
            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewScaleUnsupported(canvas.Scale, path));
        }
    }

    private static byte[] ConvertToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        return bitmap.Format switch
        {
            1 => ConvertBgra4444ToBgra8888(bitmap, path),
            2 => TrimOrCopy(bitmap.Pixels, checked(bitmap.Width * bitmap.Height * 4), path),
            _ => throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewFormatUnsupported(bitmap.Format, path))
        };
    }

    private static byte[] ConvertBgra4444ToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var source = TrimOrCopy(bitmap.Pixels, checked(bitmap.Width * bitmap.Height * 2), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        for (var i = 0; i < source.Length; i++)
        {
            var value = source[i];
            var low = value & 0x0f;
            var high = value & 0xf0;
            destination[i * 2] = (byte)(low | (low << 4));
            destination[(i * 2) + 1] = (byte)(high | (high >> 4));
        }

        return destination;
    }

    private static byte[] TrimOrCopy(byte[] source, int expectedLength, string? path)
    {
        if (source.Length < expectedLength)
        {
            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewDecodeFailed(path));
        }

        return source.Length == expectedLength ? source : source[..expectedLength];
    }

    private static async Task<CanvasImageTarget> SelectCanvasAsync(
        string sourcePath,
        WzImageInspection inspection,
        string? selector,
        string? valueSelector,
        bool useFirstCanvasFallback,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(valueSelector))
        {
            if (inspection.ObjectValue is WzImageCanvasInspection rootCanvas)
            {
                return new CanvasImageTarget(sourcePath, inspection.Selector, rootCanvas, null);
            }

            if (useFirstCanvasFallback)
            {
                var firstCanvas = inspection.Properties?
                    .SelectMany(Flatten)
                    .FirstOrDefault(property => property.Value is WzImageCanvasInspection);
                if (firstCanvas?.Value is WzImageCanvasInspection firstCanvasValue)
                {
                    return new CanvasImageTarget(sourcePath, inspection.Selector, firstCanvasValue, firstCanvas.Path);
                }
            }

            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewValueRequired(selector));
        }

        var matches = inspection.Properties?
            .SelectMany(Flatten)
            .Where(property => string.Equals(property.Path, valueSelector, StringComparison.Ordinal))
            .ToArray() ?? [];

        if (matches.Length == 0)
        {
            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewValueNotFound(valueSelector, selector));
        }

        if (matches.Length > 1)
        {
            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewValueAmbiguous(valueSelector, selector));
        }

        var match = matches[0];
        if (match.Value is WzImageCanvasInspection canvas)
        {
            return new CanvasImageTarget(sourcePath, inspection.Selector, canvas, match.Path ?? valueSelector);
        }

        if (match.Value is string linkValue && IsCanvasLinkProperty(match))
        {
            var linkedTarget = await ResolveCanvasLinkAsync(
                sourcePath,
                inspection,
                match,
                linkValue,
                options,
                cancellationToken);
            if (linkedTarget is not null)
            {
                return linkedTarget;
            }
        }

        throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewValueUnsupported(valueSelector, selector));
    }

    private static async Task<CanvasImageTarget?> ResolveCanvasLinkAsync(
        string sourcePath,
        WzImageInspection inspection,
        WzImagePropertyInspectionEntry linkProperty,
        string linkValue,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken)
    {
        var normalizedValue = NormalizePropertyPath(linkValue);
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return null;
        }

        if (string.Equals(linkProperty.Name, "_inlink", StringComparison.Ordinal))
        {
            var inlinkCanvas = FindCanvasProperty(inspection, normalizedValue);
            return inlinkCanvas?.Value is WzImageCanvasInspection canvas
                ? new CanvasImageTarget(sourcePath, inspection.Selector, canvas, inlinkCanvas.Path ?? normalizedValue)
                : null;
        }

        var logicalTarget = await ResolveLogicalImageValueAsync(
            sourcePath,
            normalizedValue,
            options.StringKey,
            cancellationToken);
        if (logicalTarget is null)
        {
            return null;
        }

        var context = await WzImageInspectionLoader.LoadAsync(
            logicalTarget.PackagePath,
            logicalTarget.Selector,
            options.StringKey,
            options.MaxPropertyDepth,
            cancellationToken);
        var linkedCanvasProperty = string.IsNullOrWhiteSpace(logicalTarget.ValuePath)
            ? null
            : FindCanvasProperty(context.ImageInspection, logicalTarget.ValuePath);
        if (linkedCanvasProperty?.Value is WzImageCanvasInspection linkedCanvas)
        {
            return new CanvasImageTarget(
                logicalTarget.PackagePath,
                logicalTarget.Selector,
                linkedCanvas,
                linkedCanvasProperty.Path ?? logicalTarget.ValuePath);
        }

        if (string.IsNullOrWhiteSpace(logicalTarget.ValuePath) &&
            context.ImageInspection.ObjectValue is WzImageCanvasInspection rootCanvas)
        {
            return new CanvasImageTarget(logicalTarget.PackagePath, logicalTarget.Selector, rootCanvas, null);
        }

        return null;
    }

    private static async Task<LogicalImageValueTarget?> ResolveLogicalImageValueAsync(
        string currentPackagePath,
        string logicalPath,
        WzStringEncryptionKind? stringKey,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizePropertyPath(logicalPath);
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var imageIndex = Array.FindIndex(parts, part => part.EndsWith(".img", StringComparison.OrdinalIgnoreCase));
        if (imageIndex < 0)
        {
            return null;
        }

        var dataRoot = FindDataRoot(currentPackagePath);
        if (dataRoot is null)
        {
            return null;
        }

        var imageName = parts[imageIndex];
        var valuePath = imageIndex + 1 < parts.Length ? string.Join('/', parts[(imageIndex + 1)..]) : null;
        var packageSegments = parts[..imageIndex];
        foreach (var candidate in EnumerateLogicalPackageCandidates(dataRoot, packageSegments, imageName))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = await TryResolveImageInPackageGroupAsync(
                candidate.PackagePath,
                candidate.Selector,
                valuePath,
                stringKey,
                cancellationToken);
            if (target is not null)
            {
                return target;
            }
        }

        return null;
    }

    private static async Task<LogicalImageValueTarget?> TryResolveImageInPackageGroupAsync(
        string packagePath,
        string selector,
        string? valuePath,
        WzStringEncryptionKind? stringKey,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(packagePath))
        {
            return null;
        }

        try
        {
            var group = await WzPackageGroupInspectionLoader.LoadAsync(packagePath, stringKey, cancellationToken);
            foreach (var member in group.Members)
            {
                if (WzImageInspectionLoader.TryFindImageEntry(member.Inspection, selector) is not null)
                {
                    return new LogicalImageValueTarget(member.SourcePath, selector, valuePath);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException or UnauthorizedAccessException)
        {
            return null;
        }

        return null;
    }

    private static IEnumerable<LogicalPackageCandidate> EnumerateLogicalPackageCandidates(
        string dataRoot,
        string[] packageSegments,
        string imageName)
    {
        if (packageSegments.Length > 0)
        {
            var firstSegment = packageSegments[0];
            var rootPackagePath = Path.Combine(dataRoot, firstSegment, firstSegment + ".wz");
            var rootSelectorParts = packageSegments.Skip(1).Append(imageName).ToArray();
            yield return new LogicalPackageCandidate(rootPackagePath, string.Join('/', rootSelectorParts));

            var folder = Path.Combine([dataRoot, .. packageSegments]);
            var folderPackagePath = Path.Combine(folder, packageSegments[^1] + ".wz");
            yield return new LogicalPackageCandidate(folderPackagePath, imageName);
        }
    }

    private static WzImagePropertyInspectionEntry? FindCanvasProperty(WzImageInspection inspection, string valuePath)
    {
        return inspection.Properties?
            .SelectMany(Flatten)
            .FirstOrDefault(property =>
                property.Value is WzImageCanvasInspection &&
                string.Equals(property.Path, valuePath, StringComparison.Ordinal)) ?? null;
    }

    private static bool IsCanvasLinkProperty(WzImagePropertyInspectionEntry property)
    {
        return property.Kind == "string" &&
            (string.Equals(property.Name, "source", StringComparison.Ordinal) ||
             string.Equals(property.Name, "_inlink", StringComparison.Ordinal) ||
             string.Equals(property.Name, "_outlink", StringComparison.Ordinal));
    }

    private static string NormalizePropertyPath(string value)
    {
        return value.Trim().Replace('\\', '/').Trim('/');
    }

    private static string? FindDataRoot(string path)
    {
        var directory = Path.GetDirectoryName(path);
        while (!string.IsNullOrWhiteSpace(directory))
        {
            if (string.Equals(Path.GetFileName(directory), "Data", StringComparison.OrdinalIgnoreCase))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        return null;
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

    private sealed record CanvasImageTarget(
        string SourcePath,
        string Selector,
        WzImageCanvasInspection Value,
        string? Path);

    private sealed record LogicalPackageCandidate(string PackagePath, string Selector);

    private sealed record LogicalImageValueTarget(string PackagePath, string Selector, string? ValuePath);
}
