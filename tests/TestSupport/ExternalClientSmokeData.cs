namespace WzComparerX.Tests;

internal static class ExternalClientSmokeData
{
    public const string ClientDataDirectoriesVariable = "WCX_CLIENT_DATA_DIRS";
    public const string GmsDataDirectoryVariable = "WCX_GMS_DATA_DIR";
    public const string KmsDataDirectoryVariable = "WCX_KMS_DATA_DIR";

    public static IReadOnlyList<ExternalClientDataDirectory> GetDataDirectories()
    {
        var directories = new List<ExternalClientDataDirectory>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddList(directories, seen, Environment.GetEnvironmentVariable(ClientDataDirectoriesVariable));
        AddSingle(directories, seen, "gms", Environment.GetEnvironmentVariable(GmsDataDirectoryVariable));
        AddSingle(directories, seen, "kms", Environment.GetEnvironmentVariable(KmsDataDirectoryVariable));

        return directories;
    }

    public static string? FindFirstFile(params string[] relativePathSegments)
    {
        foreach (var directory in GetDataDirectories())
        {
            var path = Path.Combine([directory.Path, .. relativePathSegments]);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    public static IReadOnlyList<string> FindFiles(string searchPattern, params string[] relativeDirectorySegments)
    {
        var paths = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in GetDataDirectories())
        {
            var root = Path.Combine([directory.Path, .. relativeDirectorySegments]);
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var path in Directory.GetFiles(root, searchPattern))
            {
                AddPath(paths, seen, path);
            }
        }

        return paths;
    }

    public static IReadOnlyList<string> FindRelativeFiles(params string[] relativePathSegments)
    {
        var paths = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in GetDataDirectories())
        {
            var path = Path.Combine([directory.Path, .. relativePathSegments]);
            if (File.Exists(path))
            {
                AddPath(paths, seen, path);
            }
        }

        return paths;
    }

    private static void AddList(
        List<ExternalClientDataDirectory> directories,
        HashSet<string> seen,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        foreach (var item in value.Split(Path.PathSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var (label, path) = ParseListItem(item);
            AddSingle(directories, seen, label, path);
        }
    }

    private static (string Label, string Path) ParseListItem(string item)
    {
        var separatorIndex = item.IndexOf('=');
        if (separatorIndex <= 0)
        {
            return ("client", item);
        }

        var label = item[..separatorIndex].Trim();
        var path = item[(separatorIndex + 1)..].Trim();
        return (string.IsNullOrWhiteSpace(label) ? "client" : label, path);
    }

    private static void AddSingle(
        List<ExternalClientDataDirectory> directories,
        HashSet<string> seen,
        string label,
        string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return;
        }

        var fullPath = Path.GetFullPath(path);
        if (!seen.Add(fullPath))
        {
            return;
        }

        directories.Add(new ExternalClientDataDirectory(label, fullPath));
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

internal sealed record ExternalClientDataDirectory(string Label, string Path);
