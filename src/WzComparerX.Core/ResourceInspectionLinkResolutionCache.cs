using WzComparerX.WzLib;

namespace WzComparerX.Core;

internal sealed class ResourceInspectionLinkResolutionCache
{
    private readonly Dictionary<string, Task<WzPackageGroupInspection?>> packageGroups = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Task<WzMsContainerInspection?>> msContainers = new(StringComparer.OrdinalIgnoreCase);

    public Task<WzPackageGroupInspection?> GetPackageGroupAsync(
        string path,
        WzStringEncryptionKind? stringKey,
        CancellationToken cancellationToken)
    {
        var key = $"{Path.GetFullPath(path)}\0{stringKey?.ToString() ?? "auto"}";
        if (!packageGroups.TryGetValue(key, out var task))
        {
            task = LoadPackageGroupAsync(path, stringKey, cancellationToken);
            packageGroups.Add(key, task);
        }

        return task;
    }

    public Task<WzMsContainerInspection?> GetMsContainerAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var key = Path.GetFullPath(path);
        if (!msContainers.TryGetValue(key, out var task))
        {
            task = LoadMsContainerAsync(path, cancellationToken);
            msContainers.Add(key, task);
        }

        return task;
    }

    private static async Task<WzPackageGroupInspection?> LoadPackageGroupAsync(
        string path,
        WzStringEncryptionKind? stringKey,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return await WzPackageGroupInspectionLoader.LoadAsync(path, stringKey, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or EndOfStreamException or NotSupportedException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static async Task<WzMsContainerInspection?> LoadMsContainerAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return await new WzMsContainerInspectionReader().ReadAsync(path, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
