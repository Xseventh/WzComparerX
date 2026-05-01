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
        var diagnostics = new List<ResourceInspectionDiagnostic>();

        if (entry.Header.IsValid)
        {
            foreach (var shard in EnumerateNumberedShardCandidates(entry.Header.SourcePath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!File.Exists(shard.Path))
                {
                    if (shard.IsRequired)
                    {
                        diagnostics.Add(ResourceInspectionDiagnostics.PackageGroupShardMissing(shard.Path));
                    }

                    continue;
                }

                WzDirectoryInspection inspection;
                try
                {
                    inspection = await WzImageInspectionLoader.ReadDirectoryAsync(shard.Path, stringKey, cancellationToken);
                }
                catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException or UnauthorizedAccessException)
                {
                    diagnostics.Add(ResourceInspectionDiagnostics.PackageGroupShardInvalid(shard.Path));
                    continue;
                }

                if (inspection.Header.IsValid)
                {
                    members.Add(new WzPackageGroupMemberInspection(inspection, IsEntry: false));
                    continue;
                }

                diagnostics.Add(ResourceInspectionDiagnostics.PackageGroupShardInvalid(shard.Path));
            }
        }

        return new WzPackageGroupInspection(entry, members, diagnostics);
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
        return EnumerateNumberedShardCandidates(entryPath)
            .Where(candidate => !candidate.IsRequired || File.Exists(candidate.Path))
            .Select(candidate => candidate.Path);
    }

    private static IEnumerable<NumberedShardCandidate> EnumerateNumberedShardCandidates(string entryPath)
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
                yield return new NumberedShardCandidate(path, IsRequired: true);
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

            yield return new NumberedShardCandidate(path, IsRequired: false);
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
    IReadOnlyList<WzPackageGroupMemberInspection> Members,
    IReadOnlyList<ResourceInspectionDiagnostic> Diagnostics);

internal sealed record WzPackageGroupMemberInspection(
    WzDirectoryInspection Inspection,
    bool IsEntry)
{
    public string SourcePath => Inspection.Header.SourcePath;
}

internal sealed record NumberedShardCandidate(string Path, bool IsRequired);
