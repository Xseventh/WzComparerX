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
        return new WzImagePreview(header, selector, entry, objectType);
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

    private static int ReadInt32LittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadInt32LittleEndian(bytes);
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
}
