using static WzComparerX.WzLib.WzImageBinaryReaderPrimitives;

namespace WzComparerX.WzLib;

internal sealed class WzImageBinaryPropertyInspectionReader(
    WzImageBinaryStringReader stringReader,
    int maxPropertyDepth,
    WzImageBinaryPropertyInspectionReader.ObjectValueReader readObjectValue)
{
    public delegate WzImagePropertyInspectionEntry ObjectValueReader(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path);

    public List<WzImagePropertyInspectionEntry>? ReadPropertyEntries(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int depth,
        string parentPath,
        out int? propertyCount)
    {
        SkipBytes(stream, 2);
        var count = ReadCompressedInt32(stream);
        propertyCount = count;
        var properties = new List<WzImagePropertyInspectionEntry>(Math.Max(count, 0));

        for (var i = 0; i < count; i++)
        {
            var name = stringReader.ReadImageString(stream, imageBaseOffset);
            var type = ReadByte(stream);
            var path = CombinePath(parentPath, name);
            properties.Add(ReadPropertyValue(stream, imageBaseOffset, imageEndOffset, i, name, type, depth, path));
        }

        return properties;
    }

    public WzImageNestedPropertyInspection ReadNestedObjectProperties(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int depth,
        string? path)
    {
        if (depth + 1 >= maxPropertyDepth)
        {
            return new WzImageNestedPropertyInspection(null, null);
        }

        return ReadMiniProperties(stream, imageBaseOffset, imageEndOffset, depth, path);
    }

    public WzImageNestedPropertyInspection ReadMiniProperties(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int depth,
        string? path)
    {
        var parsedChildren = ReadPropertyEntries(
            stream,
            imageBaseOffset,
            imageEndOffset,
            depth + 1,
            path ?? string.Empty,
            out var childCount);
        var children = depth + 1 < maxPropertyDepth ? parsedChildren : null;
        return new WzImageNestedPropertyInspection(childCount, children);
    }

    private WzImagePropertyInspectionEntry ReadPropertyValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        return type switch
        {
            0x00 => new WzImagePropertyInspectionEntry(index, name, type, "null", Depth: depth, Path: path),
            0x02 or 0x0b => new WzImagePropertyInspectionEntry(index, name, type, "int16", ReadInt16LittleEndian(stream), depth, path),
            0x03 or 0x13 => new WzImagePropertyInspectionEntry(index, name, type, "int32", ReadCompressedInt32(stream), depth, path),
            0x14 => new WzImagePropertyInspectionEntry(index, name, type, "int64", ReadCompressedInt64(stream), depth, path),
            0x04 => new WzImagePropertyInspectionEntry(index, name, type, "single", ReadCompressedSingle(stream), depth, path),
            0x05 => new WzImagePropertyInspectionEntry(index, name, type, "double", ReadDoubleLittleEndian(stream), depth, path),
            0x08 => new WzImagePropertyInspectionEntry(index, name, type, "string", stringReader.ReadImageString(stream, imageBaseOffset), depth, path),
            0x09 => readObjectValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            _ => throw new InvalidDataException($"Unknown image property value type 0x{type:X2}.")
        };
    }

    private static string? CombinePath(string parentPath, string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.IsNullOrEmpty(parentPath) ? null : parentPath;
        }

        return string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";
    }
}
