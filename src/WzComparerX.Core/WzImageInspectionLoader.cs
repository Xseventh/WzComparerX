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

        var entry = FindImageEntry(directoryInspection, selector);
        var selectedStringKey = directoryInspection.StringEncryptionKind ?? stringKey ?? WzStringEncryptionKind.None;
        await using var stream = File.OpenRead(path);
        var imageReader = new WzImageInspectionReader(new WzStringDecryptor(selectedStringKey), maxPropertyDepth);
        var inspection = imageReader.Read(stream, directoryInspection.Header, entry, selector);
        if (!inspection.IsValid)
        {
            throw new InvalidDataException($"Invalid WZ image selection: {selector}.");
        }

        return new WzImageInspectionContext(directoryInspection, inspection, selectedStringKey);
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

    private static WzDirectoryEntryInspection FindImageEntry(WzDirectoryInspection inspection, string selector)
    {
        var entry = TryFindImageEntry(inspection, selector);
        if (entry is not null)
        {
            return entry;
        }

        throw new InvalidDataException($"Image entry not found: {selector}.");
    }
}
