using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class WzImagePreviewService
{
    private readonly WzDirectoryPreviewReader directoryReader;
    private readonly WzImagePreviewReader imageReader;

    public WzImagePreviewService(
        WzDirectoryPreviewReader? directoryReader = null,
        WzImagePreviewReader? imageReader = null)
    {
        this.directoryReader = directoryReader ?? new WzDirectoryPreviewReader();
        this.imageReader = imageReader ?? new WzImagePreviewReader();
    }

    public WzImagePreviewService(WzStringEncryptionKind stringEncryptionKind, int maxPropertyDepth = 1)
        : this(
            new WzDirectoryPreviewReader(stringDecryptor: new WzStringDecryptor(stringEncryptionKind)),
            new WzImagePreviewReader(new WzStringDecryptor(stringEncryptionKind), maxPropertyDepth))
    {
    }

    public async Task<WzImagePreview> ReadAsync(
        string path,
        string selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        var directoryPreview = await directoryReader.ReadAsync(path, cancellationToken);
        if (!directoryPreview.Header.IsValid)
        {
            return new WzImagePreview(directoryPreview.Header, selector, Entry: null, ObjectType: null);
        }

        var entry = FindImageEntry(directoryPreview, selector);
        await using var stream = File.OpenRead(path);
        return imageReader.Read(stream, directoryPreview.Header, entry, selector);
    }

    private static WzDirectoryEntryPreview FindImageEntry(WzDirectoryPreview preview, string selector)
    {
        if (int.TryParse(selector, out var index))
        {
            var indexed = preview.Entries.FirstOrDefault(entry => entry.Index == index);
            if (indexed is not null)
            {
                return EnsureImage(indexed, selector);
            }
        }

        var named = preview.Entries.FirstOrDefault(entry =>
            entry.Kind == WzDirectoryEntryKind.Image &&
            (string.Equals(entry.Path, selector, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(entry.Name, selector, StringComparison.OrdinalIgnoreCase)));
        if (named is not null)
        {
            return named;
        }

        throw new InvalidDataException($"Image entry not found: {selector}.");
    }

    private static WzDirectoryEntryPreview EnsureImage(WzDirectoryEntryPreview entry, string selector)
    {
        if (entry.Kind != WzDirectoryEntryKind.Image)
        {
            throw new InvalidDataException($"Selected entry is not an image: {selector}.");
        }

        return entry;
    }
}
