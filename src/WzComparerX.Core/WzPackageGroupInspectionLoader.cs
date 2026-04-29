using WzComparerX.WzLib;

namespace WzComparerX.Core;

internal static class WzPackageGroupInspectionLoader
{
    public static async Task<WzPackageGroupInspection> LoadAsync(
        string entryPath,
        WzStringEncryptionKind? stringKey,
        CancellationToken cancellationToken)
    {
        var entry = await WzImageInspectionLoader.ReadDirectoryAsync(entryPath, stringKey, cancellationToken);
        var members = new List<WzPackageGroupMemberInspection>
        {
            new(entry, IsEntry: true)
        };

        if (entry.Header.IsValid)
        {
            foreach (var shardPath in EnumerateNumberedShardPaths(entry.Header.SourcePath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var shard = await WzImageInspectionLoader.ReadDirectoryAsync(shardPath, stringKey, cancellationToken);
                if (shard.Header.IsValid)
                {
                    members.Add(new WzPackageGroupMemberInspection(shard, IsEntry: false));
                }
            }
        }

        return new WzPackageGroupInspection(entry, members);
    }

    public static IEnumerable<string> EnumeratePackageGroupEntryPaths(string folder, string packageStem)
    {
        var primaryPackagePath = Path.Combine(folder, packageStem + ".wz");
        if (File.Exists(primaryPackagePath))
        {
            yield return primaryPackagePath;
        }
    }

    public static IEnumerable<string> EnumerateNumberedShardPaths(string entryPath)
    {
        var directory = Path.GetDirectoryName(entryPath);
        var stem = Path.GetFileNameWithoutExtension(entryPath);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(stem))
        {
            yield break;
        }

        if (TryReadLastWzIndex(Path.Combine(directory, stem + ".ini"), out var lastWzIndex))
        {
            for (var i = 0; i <= lastWzIndex; i++)
            {
                var path = Path.Combine(directory, $"{stem}_{i:D3}.wz");
                if (File.Exists(path))
                {
                    yield return path;
                }
            }

            yield break;
        }

        for (var i = 0; ; i++)
        {
            var path = Path.Combine(directory, $"{stem}_{i:D3}.wz");
            if (!File.Exists(path))
            {
                yield break;
            }

            yield return path;
        }
    }

    private static bool TryReadLastWzIndex(string path, out int lastWzIndex)
    {
        lastWzIndex = -1;
        if (!File.Exists(path))
        {
            return false;
        }

        foreach (var line in File.ReadLines(path))
        {
            var columns = line.Split(['|', '='], 2, StringSplitOptions.TrimEntries);
            if (columns.Length != 2 ||
                !string.Equals(columns[0], "LastWzIndex", StringComparison.Ordinal) ||
                !int.TryParse(columns[1], out var parsed) ||
                parsed < 0)
            {
                continue;
            }

            lastWzIndex = parsed;
            return true;
        }

        return false;
    }
}

internal sealed record WzPackageGroupInspection(
    WzDirectoryInspection Entry,
    IReadOnlyList<WzPackageGroupMemberInspection> Members);

internal sealed record WzPackageGroupMemberInspection(
    WzDirectoryInspection Inspection,
    bool IsEntry)
{
    public string SourcePath => Inspection.Header.SourcePath;
}
