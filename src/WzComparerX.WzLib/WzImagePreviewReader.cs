using System.Buffers.Binary;

namespace WzComparerX.WzLib;

public sealed class WzImagePreviewReader
{
    private readonly WzStringDecryptor stringDecryptor;

    public WzImagePreviewReader(WzStringDecryptor? stringDecryptor = null)
    {
        this.stringDecryptor = stringDecryptor ?? new WzStringDecryptor();
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

        stream.Position = offset;
        var objectType = ReadImageObjectTypeName(stream, offset);
        int? propertyCount = null;
        var properties = objectType == "Property" ? ReadPropertyEntries(stream, offset, out propertyCount) : null;
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
            properties.Add(ReadPropertyValue(stream, imageBaseOffset, i, name, type));
        }

        return properties;
    }

    private WzImagePropertyPreviewEntry ReadPropertyValue(
        Stream stream,
        long imageBaseOffset,
        int index,
        string? name,
        byte type)
    {
        return type switch
        {
            0x00 => new WzImagePropertyPreviewEntry(index, name, type, "null"),
            0x02 or 0x0b => new WzImagePropertyPreviewEntry(index, name, type, "int16", ReadInt16LittleEndian(stream)),
            0x03 or 0x13 => new WzImagePropertyPreviewEntry(index, name, type, "int32", ReadCompressedInt32(stream)),
            0x14 => new WzImagePropertyPreviewEntry(index, name, type, "int64", ReadCompressedInt64(stream)),
            0x04 => new WzImagePropertyPreviewEntry(index, name, type, "single", ReadCompressedSingle(stream)),
            0x05 => new WzImagePropertyPreviewEntry(index, name, type, "double", ReadDoubleLittleEndian(stream)),
            0x08 => new WzImagePropertyPreviewEntry(index, name, type, "string", ReadImageString(stream, imageBaseOffset)),
            0x09 => ReadObjectPropertyValue(stream, imageBaseOffset, index, name, type),
            _ => throw new InvalidDataException($"Unknown image property value type 0x{type:X2}.")
        };
    }

    private WzImagePropertyPreviewEntry ReadObjectPropertyValue(
        Stream stream,
        long imageBaseOffset,
        int index,
        string? name,
        byte type)
    {
        var objectDataLength = ReadInt32LittleEndian(stream);
        if (objectDataLength < 0)
        {
            throw new InvalidDataException($"Cannot read a negative object data length: {objectDataLength}.");
        }

        var endPosition = stream.Position + objectDataLength;
        if (endPosition > stream.Length)
        {
            throw new InvalidDataException($"Object data extends past the image stream: {endPosition}.");
        }

        var objectType = ReadImageObjectTypeName(stream, imageBaseOffset);
        stream.Position = endPosition;
        return new WzImagePropertyPreviewEntry(index, name, type, "object", objectType);
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
