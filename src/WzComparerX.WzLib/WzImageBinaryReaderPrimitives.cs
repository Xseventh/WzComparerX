using System.Buffers.Binary;

namespace WzComparerX.WzLib;

internal static class WzImageBinaryReaderPrimitives
{
    public static byte ReadByte(Stream stream)
    {
        var value = stream.ReadByte();
        if (value < 0)
        {
            throw new EndOfStreamException();
        }

        return (byte)value;
    }

    public static sbyte ReadSByte(Stream stream)
    {
        return unchecked((sbyte)ReadByte(stream));
    }

    public static short ReadInt16LittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(short)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadInt16LittleEndian(bytes);
    }

    public static int ReadInt32LittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadInt32LittleEndian(bytes);
    }

    public static long ReadInt64LittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadInt64LittleEndian(bytes);
    }

    public static int ReadCompressedInt32(Stream stream)
    {
        var value = ReadSByte(stream);
        return value == sbyte.MinValue ? ReadInt32LittleEndian(stream) : value;
    }

    public static long ReadCompressedInt64(Stream stream)
    {
        var value = ReadSByte(stream);
        return value == sbyte.MinValue ? ReadInt64LittleEndian(stream) : value;
    }

    public static float ReadCompressedSingle(Stream stream)
    {
        var value = ReadSByte(stream);
        return value == sbyte.MinValue ? ReadSingleLittleEndian(stream) : value;
    }

    public static float ReadSingleLittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(float)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadSingleLittleEndian(bytes);
    }

    public static double ReadDoubleLittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(double)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadDoubleLittleEndian(bytes);
    }

    public static byte[] ReadBytes(Stream stream, int count)
    {
        if (count < 0)
        {
            throw new InvalidDataException($"Cannot read a negative byte count: {count}.");
        }

        var bytes = new byte[count];
        stream.ReadExactly(bytes);
        return bytes;
    }

    public static void SkipBytes(Stream stream, int count)
    {
        if (count < 0)
        {
            throw new InvalidDataException($"Cannot skip a negative byte count: {count}.");
        }

        stream.Seek(count, SeekOrigin.Current);
    }
}
