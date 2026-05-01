namespace WzComparerX.Core;

internal static class WzSplitPackageLinkResolver
{
    public static IEnumerable<string> ResolvePaths(string sourcePath, string entryPath)
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
        var allowWorkspaceRelativeDirectory = CanResolveWorkspaceRelativeDirectory(sourcePath, parts);
        foreach (var entryDirectory in ResolveDirectories(
            sourceDirectory,
            workspaceDirectory,
            parts,
            allowWorkspaceRelativeDirectory))
        {
            var packageStem = parts[^1];
            foreach (var candidate in WzPackageGroupInspectionLoader.EnumeratePackageGroupEntryPaths(entryDirectory, packageStem))
            {
                if (!PathsEqual(candidate, sourcePath) && seen.Add(NormalizePath(candidate)))
                {
                    yield return candidate;
                }
            }
        }
    }

    public static HashSet<string> CreateAncestors(string sourcePath)
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            NormalizePath(sourcePath)
        };
    }

    public static HashSet<string> AddAncestor(IReadOnlySet<string>? ancestors, string path)
    {
        var next = ancestors is not null
            ? new HashSet<string>(ancestors, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        next.Add(path);
        return next;
    }

    public static string NormalizePath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return path;
        }
    }

    private static IEnumerable<string> ResolveDirectories(
        string sourceDirectory,
        string? workspaceDirectory,
        string[] parts,
        bool allowWorkspaceRelativeDirectory)
    {
        var relativePath = Path.Combine(parts);
        var currentPackageRelativeDirectory = Path.Combine(sourceDirectory, relativePath);
        if (Directory.Exists(currentPackageRelativeDirectory))
        {
            yield return currentPackageRelativeDirectory;
            yield break;
        }

        if (allowWorkspaceRelativeDirectory && !string.IsNullOrWhiteSpace(workspaceDirectory))
        {
            var workspaceRelativeDirectory = Path.Combine(workspaceDirectory, relativePath);
            if (Directory.Exists(workspaceRelativeDirectory) &&
                !PathsEqual(workspaceRelativeDirectory, currentPackageRelativeDirectory) &&
                !PathsEqual(workspaceRelativeDirectory, sourceDirectory))
            {
                yield return workspaceRelativeDirectory;
            }
        }
    }

    private static bool CanResolveWorkspaceRelativeDirectory(string sourcePath, string[] parts)
    {
        if (parts.Length != 1 ||
            !string.Equals(Path.GetFileName(sourcePath), "Base.wz", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var sourceDirectory = Path.GetDirectoryName(sourcePath);
        return string.Equals(Path.GetFileName(sourceDirectory), "Base", StringComparison.OrdinalIgnoreCase);
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
}
