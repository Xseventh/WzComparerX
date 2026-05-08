using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace WzComparerX.Tests;

internal static class Pkg2PackageFixture
{
    public const uint Hash1 = 0xb5b60cfc;
    public const uint Hash2 = 0x48f17f97;
    public const uint HashVersion = 0xb0da16f2;
    public const int WzVersion = 1200;

    private const int HeaderSize = 60;
    private const uint Pkg2StringKey = 0x5416c8fa;
    private const uint OffsetMagic = 0x1A2B3C4D;
    private const string Copyright = "Package file v2.0 Copyright 2002 Wizet, ZMS\0";

    public static byte[] CreateKmst1200(params Entry[] entries)
    {
        var directory = new List<byte>();
        var encryptedEntryCount = EncryptEntryCount(entries.Length);
        WriteCompressedInt32(directory, encryptedEntryCount);

        for (var i = 0; i < entries.Length; i++)
        {
            directory.Add(0x04);
            if (i == 0)
            {
                WritePkg2String(directory, entries[i].Name);
            }
            else
            {
                WriteWzStringNone(directory, entries[i].Name);
            }

            WriteCompressedInt32(directory, entries[i].Payload.Length);
            WriteCompressedInt32(directory, entries[i].Checksum);
        }

        WriteCompressedInt32(directory, encryptedEntryCount);
        var hashOffsetIndexes = new int[entries.Length];
        for (var i = 0; i < entries.Length; i++)
        {
            hashOffsetIndexes[i] = directory.Count;
            directory.AddRange(new byte[sizeof(uint)]);
        }

        var fullDirectoryStart = HeaderSize + sizeof(uint) * 2;
        var payloadOffset = fullDirectoryStart + directory.Count;
        for (var i = 0; i < entries.Length; i++)
        {
            var hashOffsetPosition = checked((uint)(fullDirectoryStart + hashOffsetIndexes[i]));
            var hashOffset = CalculateHashedOffset(hashOffsetPosition, checked((uint)payloadOffset));
            BinaryPrimitives.WriteUInt32LittleEndian(directory.GetSpan(hashOffsetIndexes[i], sizeof(uint)), hashOffset);
            payloadOffset += entries[i].Payload.Length;
        }

        var payloadBytes = entries.SelectMany(entry => entry.Payload).ToArray();
        var dataSize = sizeof(uint) * 2 + directory.Count + payloadBytes.Length;
        var header = CreateHeader(dataSize);
        var hashes = new byte[sizeof(uint) * 2];
        BinaryPrimitives.WriteUInt32LittleEndian(hashes.AsSpan(0, sizeof(uint)), Hash1);
        BinaryPrimitives.WriteUInt32LittleEndian(hashes.AsSpan(sizeof(uint), sizeof(uint)), Hash2);
        return [.. header, .. hashes, .. directory, .. payloadBytes];
    }

    public static byte[] CreateTextImage(params (string Name, string Value)[] values)
    {
        var builder = new StringBuilder();
        builder.AppendLine("#Property");
        foreach (var (name, value) in values)
        {
            builder.Append(name);
            builder.Append(" = ");
            builder.AppendLine(value);
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    public sealed record Entry(string Name, byte[] Payload, int Checksum = 0);

    private static byte[] CreateHeader(int dataSize)
    {
        var copyrightBytes = Encoding.ASCII.GetBytes(Copyright);
        if (HeaderSize != 4 + sizeof(long) + sizeof(int) + copyrightBytes.Length)
        {
            throw new InvalidOperationException("PKG2 fixture copyright length must keep the WC header size stable.");
        }

        var bytes = new byte[HeaderSize];
        Encoding.ASCII.GetBytes("PKG2", bytes);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(4, sizeof(long)), dataSize);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12, sizeof(int)), HeaderSize);
        copyrightBytes.CopyTo(bytes.AsSpan(16));
        return bytes;
    }

    private static int EncryptEntryCount(int entryCount)
    {
        unchecked
        {
            var mixedHash = CalculateMixedHash();
            return (int)((uint)entryCount ^
                ((Hash1 << 16) + (mixedHash & 0x7fffffffu) - (0x21524111u * HashVersion)));
        }
    }

    private static uint CalculateHashedOffset(uint hashOffsetPosition, uint targetOffset)
    {
        unchecked
        {
            var preHash = Hash1 ^ HashVersion;
            var mixedHash = CalculateMixedHash();
            var offset = hashOffsetPosition - HeaderSize;
            offset = ~offset;
            offset *= preHash + (mixedHash ^ 0xA7E3C093u);
            offset -= 0x581C3F6Du;
            offset ^= Hash1 * 0x01010101u;
            offset ^= mixedHash * 0x9E3779B9u;
            offset = RotateLeft(offset, (int)((preHash ^ mixedHash) & 0x1f));
            return ~((targetOffset - HeaderSize) ^ offset);
        }
    }

    private static uint CalculateMixedHash()
    {
        unchecked
        {
            var preHash = Hash1 ^ HashVersion;
            return Mix(preHash ^ 0x6D4C3B2Au) ^ 0x91E10DA5u;
        }
    }

    private static uint Mix(uint value)
    {
        unchecked
        {
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value;
        }
    }

    private static void WritePkg2String(List<byte> bytes, string value)
    {
        var text = Encoding.Unicode.GetBytes(value);
        bytes.Add(unchecked((byte)-value.Length));
        Span<byte> keyBytes =
        [
            (byte)(Pkg2StringKey & 0xff),
            (byte)((Pkg2StringKey >> 8) & 0xff),
            (byte)((Pkg2StringKey >> 8) & 0xff),
            (byte)((Pkg2StringKey >> 16) & 0xff),
            (byte)((Pkg2StringKey >> 16) & 0xff),
            (byte)((Pkg2StringKey >> 24) & 0xff),
            (byte)((Pkg2StringKey >> 24) & 0xff),
            0x00
        ];

        for (var i = 0; i < text.Length; i++)
        {
            bytes.Add((byte)(text[i] ^ keyBytes[i & 7]));
        }
    }

    private static void WriteWzStringNone(List<byte> bytes, string value)
    {
        var text = Encoding.Latin1.GetBytes(value);
        bytes.Add(unchecked((byte)-text.Length));
        for (var i = 0; i < text.Length; i++)
        {
            bytes.Add((byte)(text[i] ^ (byte)(0xAA + i)));
        }
    }

    private static void WriteCompressedInt32(List<byte> bytes, int value)
    {
        if (value is >= sbyte.MinValue and <= sbyte.MaxValue and not sbyte.MinValue)
        {
            bytes.Add(unchecked((byte)(sbyte)value));
            return;
        }

        bytes.Add(unchecked((byte)sbyte.MinValue));
        bytes.AddRange(BitConverter.GetBytes(value));
    }

    private static uint RotateLeft(uint value, int distance)
    {
        return (value << distance) | (value >> (32 - distance));
    }

    private static Span<byte> GetSpan(this List<byte> bytes, int start, int length)
    {
        return CollectionsMarshal.AsSpan(bytes).Slice(start, length);
    }
}
