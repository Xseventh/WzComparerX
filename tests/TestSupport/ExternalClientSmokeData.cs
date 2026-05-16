namespace WzComparerX.Tests;

internal static class ExternalClientSmokeData
{
    public const string ClientDataDirectoryVariable = "WCX_CLIENT_DATA_DIR";

    public static string? GetDataDirectory()
    {
        return ParseDataDirectory(Environment.GetEnvironmentVariable(ClientDataDirectoryVariable));
    }

    public static string? ParseDataDirectory(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || !Directory.Exists(value)
            ? null
            : Path.GetFullPath(value);
    }

    public static string? FindFirstFile(params string[] relativePathSegments)
    {
        var directory = GetDataDirectory();
        if (directory is null)
        {
            return null;
        }

        var path = Path.Combine([directory, .. relativePathSegments]);
        return File.Exists(path) ? path : null;
    }

    public static IReadOnlyList<string> FindFiles(string searchPattern, params string[] relativeDirectorySegments)
    {
        var directory = GetDataDirectory();
        if (directory is null)
        {
            return [];
        }

        var paths = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var root = Path.Combine([directory, .. relativeDirectorySegments]);
        if (!Directory.Exists(root))
        {
            return [];
        }

        foreach (var path in Directory.GetFiles(root, searchPattern))
        {
            AddPath(paths, seen, path);
        }

        return paths;
    }

    public static IReadOnlyList<string> FindRelativeFiles(params string[] relativePathSegments)
    {
        var path = FindFirstFile(relativePathSegments);
        if (path is null)
        {
            return [];
        }

        return [Path.GetFullPath(path)];
    }

    private static void AddPath(List<string> paths, HashSet<string> seen, string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (seen.Add(fullPath))
        {
            paths.Add(fullPath);
        }
    }
}
