using static WzComparerX.WzLib.WzImageBinaryReaderPrimitives;

namespace WzComparerX.WzLib;

internal sealed class WzImageBinaryStringReader(WzStringDecryptor stringDecryptor)
{
    public string ReadObjectTypeName(Stream stream, long imageBaseOffset)
    {
        var flag = ReadByte(stream);
        return flag switch
        {
            0x73 => ReadString(stream),
            0x1b => ReadStringAt(stream, imageBaseOffset + ReadInt32LittleEndian(stream)),
            _ => throw new InvalidDataException($"Unexpected image object type flag 0x{flag:X2}.")
        };
    }

    public string? ReadImageString(Stream stream, long imageBaseOffset)
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
}
