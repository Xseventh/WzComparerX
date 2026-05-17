using WzComparerX.WzLib;

namespace WzComparerX.Core;

internal static class WzImageInspectionLoader
{
    public static async Task<WzImageInspectionContext> LoadAsync(
        string path,
        string selector,
        WzStringEncryptionKind? stringKey,
        int maxPropertyDepth,
        CancellationToken cancellationToken)
    {
        var directoryInspection = await ReadDirectoryAsync(path, stringKey, cancellationToken);
        if (!directoryInspection.Header.IsValid)
        {
            throw new InvalidDataException($"Invalid WZ package: {directoryInspection.Header.SourcePath}.");
        }

        var target = await FindImageTargetAsync(path, directoryInspection, selector, stringKey, cancellationToken);
        var selectedStringKey = target.DirectoryInspection.StringEncryptionKind ?? stringKey ?? WzStringEncryptionKind.None;
        await using var stream = File.OpenRead(target.DirectoryInspection.Header.SourcePath);
        var imageReader = new WzImageInspectionReader(new WzStringDecryptor(selectedStringKey), maxPropertyDepth);
        var inspection = imageReader.Read(stream, target.DirectoryInspection.Header, target.Entry, selector);
        if (!inspection.IsValid)
        {
            throw new InvalidDataException($"Invalid WZ image selection: {selector}.");
        }

        return new WzImageInspectionContext(target.DirectoryInspection, inspection, selectedStringKey);
    }

    public static Task<WzDirectoryInspection> ReadDirectoryAsync(
        string path,
        WzStringEncryptionKind? stringKey,
        CancellationToken cancellationToken)
    {
        if (stringKey is null)
        {
            return WzStringKeyAutoDetector.ReadDirectoryAsync(path, cancellationToken);
        }

        var reader = new WzDirectoryInspectionReader(stringDecryptor: new WzStringDecryptor(stringKey.Value));
        return reader.ReadAsync(path, cancellationToken);
    }

    public static WzDirectoryEntryInspection? TryFindImageEntry(WzDirectoryInspection inspection, string selector)
    {
        if (int.TryParse(selector, out var index))
        {
            var indexed = inspection.Entries.FirstOrDefault(entry => entry.Index == index);
            if (indexed is not null)
            {
                return indexed.Kind == WzDirectoryEntryKind.Image ? indexed : null;
            }
        }

        var named = inspection.Entries.FirstOrDefault(entry =>
            entry.Kind == WzDirectoryEntryKind.Image &&
            (string.Equals(entry.Path, selector, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(entry.Name, selector, StringComparison.OrdinalIgnoreCase)));
        if (named is not null)
        {
            return named;
        }

        return null;
    }

    private static async Task<WzImageInspectionTarget> FindImageTargetAsync(
        string path,
        WzDirectoryInspection directoryInspection,
        string selector,
        WzStringEncryptionKind? stringKey,
        CancellationToken cancellationToken)
    {
        var entry = TryFindImageEntry(directoryInspection, selector);
        if (entry is not null)
        {
            return new WzImageInspectionTarget(directoryInspection, entry);
        }

        var group = await WzPackageGroupInspectionLoader.LoadAsync(path, stringKey, cancellationToken);
        foreach (var member in group.Members.Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            entry = TryFindImageEntry(member.Inspection, selector);
            if (entry is not null)
            {
                return new WzImageInspectionTarget(member.Inspection, entry);
            }
        }

        throw new ResourceInspectionException(ResourceInspectionDiagnostics.ImageEntryNotFound(selector));
    }
}

internal sealed record WzImageInspectionTarget(
    WzDirectoryInspection DirectoryInspection,
    WzDirectoryEntryInspection Entry);
