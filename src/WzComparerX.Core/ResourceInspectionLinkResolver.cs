using WzComparerX.WzLib;

namespace WzComparerX.Core;

public static class ResourceInspectionLinkResolver
{
    public static string? GetLinkKind(WzImagePropertyInspectionEntry property)
    {
        if (property.Kind == "uol")
        {
            return "uol";
        }

        if (property.Kind != "string")
        {
            return null;
        }

        return property.Name switch
        {
            "source" or "_inlink" or "_outlink" or "link" => property.Name,
            _ => null
        };
    }

    public static string? GetLinkedTarget(WzImagePropertyInspectionEntry property)
    {
        return GetLinkKind(property) is not null && property.Value is string value
            ? NormalizeLinkTarget(value)
            : null;
    }

    public static async Task<ResourceInspectionResolvedLinkTarget?> ResolveAsync(
        string currentPackagePath,
        WzImageInspection currentImage,
        WzImagePropertyInspectionEntry property,
        WzStringEncryptionKind? stringKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPackagePath);
        ArgumentNullException.ThrowIfNull(currentImage);
        ArgumentNullException.ThrowIfNull(property);

        var linkKind = GetLinkKind(property);
        var linkedTarget = GetLinkedTarget(property);
        if (string.IsNullOrWhiteSpace(linkKind) || string.IsNullOrWhiteSpace(linkedTarget))
        {
            return null;
        }

        return linkKind switch
        {
            "_inlink" => new ResourceInspectionResolvedLinkTarget(
                currentPackagePath,
                currentImage.Selector,
                linkedTarget),
            "uol" => ResolveLocalRelativeTarget(currentPackagePath, currentImage.Selector, property.Path, linkedTarget),
            "source" or "_outlink" or "link" => await ResolveLogicalImageValueAsync(
                currentPackagePath,
                linkedTarget,
                stringKey,
                cancellationToken),
            _ => null
        };
    }

    public static string? NormalizeLinkTarget(string value)
    {
        var normalized = value.Trim().Replace('\\', '/').Trim('/');
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    public static async Task<ResourceInspectionResolvedLinkTarget?> ResolveLogicalImageValueAsync(
        string currentPackagePath,
        string logicalPath,
        WzStringEncryptionKind? stringKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPackagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(logicalPath);

        var normalized = NormalizeLinkTarget(logicalPath);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var imageIndex = Array.FindIndex(parts, part => part.EndsWith(".img", StringComparison.OrdinalIgnoreCase));
        if (imageIndex < 0)
        {
            return null;
        }

        var imageName = parts[imageIndex];
        var valuePath = imageIndex + 1 < parts.Length ? string.Join('/', parts[(imageIndex + 1)..]) : null;
        var packageSegments = parts[..imageIndex];
        var logicalImageSelector = string.Join('/', parts[..(imageIndex + 1)]);
        if (MsMnContainerKind.IsPath(currentPackagePath))
        {
            var currentMsTarget = await TryResolveImageInMsContainerAsync(
                currentPackagePath,
                logicalImageSelector,
                valuePath,
                cancellationToken);
            if (currentMsTarget is not null)
            {
                return currentMsTarget;
            }
        }

        var dataRoot = FindDataRoot(currentPackagePath);
        if (dataRoot is null)
        {
            return null;
        }

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

        foreach (var msPath in EnumerateLogicalMsContainerCandidates(dataRoot, packageSegments))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = await TryResolveImageInMsContainerAsync(
                msPath,
                logicalImageSelector,
                valuePath,
                cancellationToken);
            if (target is not null)
            {
                return target;
            }
        }

        return null;
    }

    private static ResourceInspectionResolvedLinkTarget? ResolveLocalRelativeTarget(
        string packagePath,
        string selector,
        string? currentValuePath,
        string linkedTarget)
    {
        var targetParts = linkedTarget.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (targetParts.Length == 0)
        {
            return null;
        }

        var currentParts = (currentValuePath ?? string.Empty)
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stack = currentParts.Length > 0
            ? currentParts[..^1].ToList()
            : [];

        foreach (var part in targetParts)
        {
            if (part == ".")
            {
                continue;
            }

            if (part == "..")
            {
                if (stack.Count > 0)
                {
                    stack.RemoveAt(stack.Count - 1);
                }

                continue;
            }

            stack.Add(part);
        }

        return stack.Count == 0
            ? null
            : new ResourceInspectionResolvedLinkTarget(packagePath, selector, string.Join('/', stack));
    }

    private static async Task<ResourceInspectionResolvedLinkTarget?> TryResolveImageInPackageGroupAsync(
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
                    return new ResourceInspectionResolvedLinkTarget(member.SourcePath, selector, valuePath);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or EndOfStreamException or NotSupportedException or UnauthorizedAccessException)
        {
            return null;
        }

        return null;
    }

    private static async Task<ResourceInspectionResolvedLinkTarget?> TryResolveImageInMsContainerAsync(
        string containerPath,
        string selector,
        string? valuePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(containerPath))
        {
            return null;
        }

        try
        {
            var inspection = await new WzMsContainerInspectionReader().ReadAsync(containerPath, cancellationToken);
            var entry = WzMsImageInspectionLoader.TryFindImageEntry(inspection, selector);
            return entry is null
                ? null
                : new ResourceInspectionResolvedLinkTarget(inspection.Header.SourcePath, entry.Path, valuePath);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static IEnumerable<LogicalPackageCandidate> EnumerateLogicalPackageCandidates(
        string dataRoot,
        string[] packageSegments,
        string imageName)
    {
        if (packageSegments.Length == 0)
        {
            yield break;
        }

        var firstSegment = packageSegments[0];
        var rootPackagePath = Path.Combine(dataRoot, firstSegment, firstSegment + ".wz");
        var rootSelectorParts = packageSegments.Skip(1).Append(imageName).ToArray();
        yield return new LogicalPackageCandidate(rootPackagePath, string.Join('/', rootSelectorParts));

        var folder = Path.Combine([dataRoot, .. packageSegments]);
        var folderPackagePath = Path.Combine(folder, packageSegments[^1] + ".wz");
        yield return new LogicalPackageCandidate(folderPackagePath, imageName);
    }

    private static IEnumerable<string> EnumerateLogicalMsContainerCandidates(
        string dataRoot,
        string[] packageSegments)
    {
        if (packageSegments.Length == 0)
        {
            yield break;
        }

        var packsRoot = Path.Combine(dataRoot, "Packs");
        if (!Directory.Exists(packsRoot))
        {
            yield break;
        }

        var prefix = packageSegments[0];
        foreach (var path in Directory.EnumerateFiles(packsRoot)
                     .Where(MsMnContainerKind.IsPath)
                     .Where(path => IsLogicalMsContainerCandidate(path, prefix))
                     .Order(StringComparer.OrdinalIgnoreCase))
        {
            yield return path;
        }
    }

    private static bool IsLogicalMsContainerCandidate(string path, string prefix)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        return string.Equals(fileName, prefix, StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase);
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

    private sealed record LogicalPackageCandidate(string PackagePath, string Selector);
}
