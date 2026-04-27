using System.Buffers.Binary;

namespace WzComparerX.WzLib;

public sealed class WzImagePreviewReader
{
    public const int MaxPropertyPreviewDepth = 64;

    private readonly WzStringDecryptor stringDecryptor;
    private readonly int maxPropertyDepth;

    public WzImagePreviewReader(WzStringDecryptor? stringDecryptor = null, int maxPropertyDepth = 1)
    {
        if (maxPropertyDepth is < 0 or > MaxPropertyPreviewDepth)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxPropertyDepth),
                $"Property preview depth must be between 0 and {MaxPropertyPreviewDepth}.");
        }

        this.stringDecryptor = stringDecryptor ?? new WzStringDecryptor();
        this.maxPropertyDepth = maxPropertyDepth;
    }

    public WzImagePreview Read(
        Stream stream,
        WzPackageHeader header,
        WzDirectoryEntryPreview entry,
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
        var objectType = ReadImageObjectTypeName(stream, offset);
        int? propertyCount = null;
        var properties = objectType == "Property" && maxPropertyDepth > 0
            ? ReadPropertyEntries(stream, offset, imageEndOffset, depth: 0, parentPath: string.Empty, out propertyCount)
            : null;
        return new WzImagePreview(header, selector, entry, objectType, propertyCount, properties);
    }

    private string ReadImageObjectTypeName(Stream stream, long imageBaseOffset)
    {
        var flag = ReadByte(stream);
        return flag switch
        {
            0x73 => ReadString(stream),
            0x1b => ReadStringAt(stream, imageBaseOffset + ReadInt32LittleEndian(stream)),
            _ => throw new InvalidDataException($"Unexpected image object type flag 0x{flag:X2}.")
        };
    }

    private List<WzImagePropertyPreviewEntry>? ReadPropertyEntries(
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
        var properties = new List<WzImagePropertyPreviewEntry>(Math.Max(count, 0));

        for (var i = 0; i < count; i++)
        {
            var name = ReadImageString(stream, imageBaseOffset);
            var type = ReadByte(stream);
            var path = CombinePath(parentPath, name);
            properties.Add(ReadPropertyValue(stream, imageBaseOffset, imageEndOffset, i, name, type, depth, path));
        }

        return properties;
    }

    private WzImagePropertyPreviewEntry ReadPropertyValue(
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
            0x00 => new WzImagePropertyPreviewEntry(index, name, type, "null", Depth: depth, Path: path),
            0x02 or 0x0b => new WzImagePropertyPreviewEntry(index, name, type, "int16", ReadInt16LittleEndian(stream), depth, path),
            0x03 or 0x13 => new WzImagePropertyPreviewEntry(index, name, type, "int32", ReadCompressedInt32(stream), depth, path),
            0x14 => new WzImagePropertyPreviewEntry(index, name, type, "int64", ReadCompressedInt64(stream), depth, path),
            0x04 => new WzImagePropertyPreviewEntry(index, name, type, "single", ReadCompressedSingle(stream), depth, path),
            0x05 => new WzImagePropertyPreviewEntry(index, name, type, "double", ReadDoubleLittleEndian(stream), depth, path),
            0x08 => new WzImagePropertyPreviewEntry(index, name, type, "string", ReadImageString(stream, imageBaseOffset), depth, path),
            0x09 => ReadObjectPropertyValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            _ => throw new InvalidDataException($"Unknown image property value type 0x{type:X2}.")
        };
    }

    private WzImagePropertyPreviewEntry ReadObjectPropertyValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        var objectDataLength = ReadInt32LittleEndian(stream);
        if (objectDataLength < 0)
        {
            throw new InvalidDataException($"Cannot read a negative object data length: {objectDataLength}.");
        }

        var endPosition = stream.Position + objectDataLength;
        if (endPosition > imageEndOffset)
        {
            throw new InvalidDataException($"Object data extends past the image stream: {endPosition}.");
        }

        var objectType = ReadImageObjectTypeName(stream, imageBaseOffset);
        var property = objectType switch
        {
            "Property" => ReadNestedPropertyObjectValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            "Shape2D#Vector2D" => ReadVectorObjectValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            "Canvas" => ReadCanvasObjectValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            "Shape2D#Convex2D" => ReadConvexObjectValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            "UOL" => ReadUolObjectValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            _ => new WzImagePropertyPreviewEntry(index, name, type, "object", objectType, depth, path)
        };

        if (stream.Position > endPosition)
        {
            throw new InvalidDataException($"Object data parser moved past the object boundary: {stream.Position}.");
        }

        stream.Position = endPosition;
        return property;
    }

    private WzImagePropertyPreviewEntry ReadNestedPropertyObjectValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        int? childCount = null;
        List<WzImagePropertyPreviewEntry>? children = null;
        if (depth + 1 < maxPropertyDepth)
        {
            children = ReadPropertyEntries(stream, imageBaseOffset, imageEndOffset, depth + 1, path ?? string.Empty, out childCount);
        }

        return new WzImagePropertyPreviewEntry(index, name, type, "object", "Property", depth, path, childCount, children);
    }

    private static WzImagePropertyPreviewEntry ReadVectorObjectValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        _ = imageBaseOffset;
        _ = imageEndOffset;
        return new WzImagePropertyPreviewEntry(index, name, type, "vector", ReadVectorPreview(stream), depth, path);
    }

    private WzImagePropertyPreviewEntry ReadCanvasObjectValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        SkipBytes(stream, 1);
        int? childCount = null;
        List<WzImagePropertyPreviewEntry>? children = null;
        if (ReadByte(stream) == 0x01)
        {
            var parsedChildren = ReadPropertyEntries(stream, imageBaseOffset, imageEndOffset, depth + 1, path ?? string.Empty, out childCount);
            if (depth + 1 < maxPropertyDepth)
            {
                children = parsedChildren;
            }
        }

        var width = ReadCompressedInt32(stream);
        var height = ReadCompressedInt32(stream);
        var format = ReadCompressedInt32(stream);
        var scale = ReadByte(stream);
        var pages = ReadCompressedInt32(stream);
        var unknown1 = ReadCompressedInt32(stream);
        SkipBytes(stream, 2);
        var dataLength = ReadInt32LittleEndian(stream);
        var dataOffset = stream.Position;
        if (dataLength < 0)
        {
            throw new InvalidDataException($"Cannot read a negative canvas data length: {dataLength}.");
        }

        if (dataOffset + dataLength > imageEndOffset)
        {
            throw new InvalidDataException($"Canvas data extends past the image stream: {dataOffset + dataLength}.");
        }

        SkipBytes(stream, dataLength);
        var canvas = new WzImageCanvasPreview(width, height, format, scale, pages, unknown1, dataOffset, dataLength);
        return new WzImagePropertyPreviewEntry(index, name, type, "canvas", canvas, depth, path, childCount, children);
    }

    private WzImagePropertyPreviewEntry ReadConvexObjectValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        _ = imageEndOffset;
        var pointCount = ReadCompressedInt32(stream);
        if (pointCount < 0)
        {
            throw new InvalidDataException($"Cannot read a negative convex point count: {pointCount}.");
        }

        var points = new List<WzImageVectorPreview>(pointCount);
        for (var i = 0; i < pointCount; i++)
        {
            var objectType = ReadImageObjectTypeName(stream, imageBaseOffset);
            if (objectType != "Shape2D#Vector2D")
            {
                throw new InvalidDataException($"Convex2D point {i} is not a vector: {objectType}.");
            }

            points.Add(ReadVectorPreview(stream));
        }

        return new WzImagePropertyPreviewEntry(index, name, type, "convex", new WzImageConvexPreview(points), depth, path);
    }

    private WzImagePropertyPreviewEntry ReadUolObjectValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        _ = imageEndOffset;
        SkipBytes(stream, 1);
        return new WzImagePropertyPreviewEntry(index, name, type, "uol", ReadImageString(stream, imageBaseOffset), depth, path);
    }

    private static WzImageVectorPreview ReadVectorPreview(Stream stream)
    {
        return new WzImageVectorPreview(ReadCompressedInt32(stream), ReadCompressedInt32(stream));
    }

    private static string? CombinePath(string parentPath, string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.IsNullOrEmpty(parentPath) ? null : parentPath;
        }

        return string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";
    }

    private string? ReadImageString(Stream stream, long imageBaseOffset)
    {
        var flag = ReadByte(stream);
        return flag switch
        {
            0x00 => ReadString(stream),
            0x01 => ReadStringAt(stream, imageBaseOffset + ReadInt32LittleEndian(stream)),
            0x04 => SkipNullImageString(stream),
            _ => throw new InvalidDataException($"Unexpected image string flag 0x{flag:X2}.")
        };
    }

    private string ReadString(Stream stream)
    {
        var size = ReadSByte(stream);
        if (size < 0)
        {
            var byteCount = size == sbyte.MinValue ? ReadInt32LittleEndian(stream) : -size;
            return stringDecryptor.Decode(ReadBytes(stream, byteCount), unicode: false);
        }

        if (size > 0)
        {
            var charCount = size == sbyte.MaxValue ? ReadInt32LittleEndian(stream) : size;
            return stringDecryptor.Decode(ReadBytes(stream, charCount * sizeof(char)), unicode: true);
        }

        return string.Empty;
    }

    private static string? SkipNullImageString(Stream stream)
    {
        SkipBytes(stream, 8);
        return null;
    }

    private string ReadStringAt(Stream stream, long offset)
    {
        if (offset < 0)
        {
            throw new InvalidDataException($"Cannot read a string from a negative offset: {offset}.");
        }

        var position = stream.Position;
        stream.Position = offset;
        var value = ReadString(stream);
        stream.Position = position;
        return value;
    }

    private static byte ReadByte(Stream stream)
    {
        var value = stream.ReadByte();
        if (value < 0)
        {
            throw new EndOfStreamException();
        }

        return (byte)value;
    }

    private static sbyte ReadSByte(Stream stream)
    {
        return unchecked((sbyte)ReadByte(stream));
    }

    private static short ReadInt16LittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(short)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadInt16LittleEndian(bytes);
    }

    private static int ReadInt32LittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadInt32LittleEndian(bytes);
    }

    private static long ReadInt64LittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadInt64LittleEndian(bytes);
    }

    private static int ReadCompressedInt32(Stream stream)
    {
        var value = ReadSByte(stream);
        return value == sbyte.MinValue ? ReadInt32LittleEndian(stream) : value;
    }

    private static long ReadCompressedInt64(Stream stream)
    {
        var value = ReadSByte(stream);
        return value == sbyte.MinValue ? ReadInt64LittleEndian(stream) : value;
    }

    private static float ReadCompressedSingle(Stream stream)
    {
        var value = ReadSByte(stream);
        return value == sbyte.MinValue ? ReadSingleLittleEndian(stream) : value;
    }

    private static float ReadSingleLittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(float)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadSingleLittleEndian(bytes);
    }

    private static double ReadDoubleLittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(double)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadDoubleLittleEndian(bytes);
    }

    private static byte[] ReadBytes(Stream stream, int count)
    {
        if (count < 0)
        {
            throw new InvalidDataException($"Cannot read a negative byte count: {count}.");
        }

        var bytes = new byte[count];
        stream.ReadExactly(bytes);
        return bytes;
    }

    private static void SkipBytes(Stream stream, int count)
    {
        if (count < 0)
        {
            throw new InvalidDataException($"Cannot skip a negative byte count: {count}.");
        }

        stream.Seek(count, SeekOrigin.Current);
    }
}
