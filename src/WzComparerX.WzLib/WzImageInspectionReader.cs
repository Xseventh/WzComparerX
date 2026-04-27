namespace WzComparerX.WzLib;

public sealed class WzImageInspectionReader
{
    public const int MaxPropertyInspectionDepth = 64;

    private readonly WzStringDecryptor stringDecryptor;
    private readonly int maxPropertyDepth;

    public WzImageInspectionReader(WzStringDecryptor? stringDecryptor = null, int maxPropertyDepth = 1)
    {
        if (maxPropertyDepth is < 0 or > MaxPropertyInspectionDepth)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxPropertyDepth),
                $"Property inspection depth must be between 0 and {MaxPropertyInspectionDepth}.");
        }

        this.stringDecryptor = stringDecryptor ?? new WzStringDecryptor();
        this.maxPropertyDepth = maxPropertyDepth;
    }

    public WzImageInspection Read(
        Stream stream,
        WzPackageHeader header,
        WzDirectoryEntryInspection entry,
        string selector)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        if (entry.Kind != WzDirectoryEntryKind.Image)
        {
            throw new InvalidDataException($"Selected entry is not an image: {entry.Path ?? entry.Name ?? entry.Index.ToString()}.");
        }

        if (entry.Offset is not long offset)
        {
            throw new InvalidDataException("Selected image entry does not have a calculated payload offset.");
        }

        if (offset < 0 || offset >= stream.Length)
        {
            throw new InvalidDataException($"Selected image offset is outside the file: {offset}.");
        }

        var imageEndOffset = offset + entry.DataSize;
        if (imageEndOffset > stream.Length)
        {
            throw new InvalidDataException($"Selected image extends past the file: {imageEndOffset}.");
        }

        stream.Position = offset;
        var textReader = new WzImageTextInspectionReader(maxPropertyDepth);
        if (textReader.TryRead(stream, header, entry, selector, imageEndOffset, out var textInspection))
        {
            return textInspection;
        }

        if (WzImageLuaInspectionReader.IsLuaEntry(entry))
        {
            return new WzImageLuaInspectionReader(stringDecryptor, maxPropertyDepth)
                .Read(stream, header, entry, selector, imageEndOffset);
        }

        return new WzImageBinaryInspectionReader(stringDecryptor, maxPropertyDepth)
            .Read(stream, header, entry, selector, offset, imageEndOffset);
    }
}
