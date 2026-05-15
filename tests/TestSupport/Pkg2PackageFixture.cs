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
    public const uint ModernHash1 = 0xfb6981e4;
    public const uint ModernHash2 = 0xd5862d30;
    public const int ModernHeaderSize = 0x44;
    public const int ModernWzVersion = 0;

    private const int StandardHeaderSize = 60;
    private const uint Pkg2StringKey = 0x5416c8fa;
    private const string Copyright = "Package file v2.0 Copyright 2002 Wizet, ZMS\0";

    private static readonly int[] ModernHash1Offsets = [0x43, 0x1A, 0x30, 0x10];
    private static readonly int[] ModernHash2Offsets = [0x2D, 0x07, 0x3F, 0x2E];
    private static readonly int[] ModernDataSizeOffsets = [0x15, 0x19, 0x39, 0x41];

    public static byte[] CreateKmst1200(params Entry[] entries)
    {
        return Create(
            entries,
            StandardHeaderSize,
            Hash1,
            Hash2,
            HashVersion,
            Pkg2StringKey,
            CreateStandardHeader,
            writesSeparateHashes: true);
    }

    public static byte[] CreateModernKms(params Entry[] entries)
    {
        return Create(
            entries,
            ModernHeaderSize,
            ModernHash1,
            ModernHash2,
            HashVersion,
            CalculatePkg2StringKey(ModernHash1, HashVersion),
            CreateModernHeader,
            writesSeparateHashes: false);
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

    private static byte[] Create(
        IReadOnlyList<Entry> entries,
        int headerSize,
        uint hash1,
        uint hash2,
        uint hashVersion,
        uint pkg2StringKey,
        Func<int, uint, uint, byte[]> createHeader,
        bool writesSeparateHashes)
    {
        var directory = new List<byte>();
        var encryptedEntryCount = EncryptEntryCount(entries.Count, hash1, hashVersion);
        WriteCompressedInt32(directory, encryptedEntryCount);

        for (var i = 0; i < entries.Count; i++)
        {
            directory.Add(0x04);
            if (i == 0)
            {
                WritePkg2String(directory, entries[i].Name, pkg2StringKey);
            }
            else
            {
                WriteWzStringNone(directory, entries[i].Name);
            }

            WriteCompressedInt32(directory, entries[i].Payload.Length);
            WriteCompressedInt32(directory, entries[i].Checksum);
        }

        WriteCompressedInt32(directory, encryptedEntryCount);
        var hashOffsetIndexes = new int[entries.Count];
        for (var i = 0; i < entries.Count; i++)
        {
            hashOffsetIndexes[i] = directory.Count;
            directory.AddRange(new byte[sizeof(uint)]);
        }

        var hashPrefixLength = writesSeparateHashes ? sizeof(uint) * 2 : 0;
        var fullDirectoryStart = headerSize + hashPrefixLength;
        var payloadOffset = fullDirectoryStart + directory.Count;
        for (var i = 0; i < entries.Count; i++)
        {
            var hashOffsetPosition = checked((uint)(fullDirectoryStart + hashOffsetIndexes[i]));
            var hashOffset = CalculateHashedOffset(
                hashOffsetPosition,
                checked((uint)payloadOffset),
                headerSize,
                hash1,
                hashVersion);
            BinaryPrimitives.WriteUInt32LittleEndian(directory.GetSpan(hashOffsetIndexes[i], sizeof(uint)), hashOffset);
            payloadOffset += entries[i].Payload.Length;
        }

        var payloadBytes = entries.SelectMany(entry => entry.Payload).ToArray();
        var dataSize = hashPrefixLength + directory.Count + payloadBytes.Length;
        var header = createHeader(dataSize, hash1, hash2);
        if (!writesSeparateHashes)
        {
            return [.. header, .. directory, .. payloadBytes];
        }

        var hashes = new byte[sizeof(uint) * 2];
        BinaryPrimitives.WriteUInt32LittleEndian(hashes.AsSpan(0, sizeof(uint)), hash1);
        BinaryPrimitives.WriteUInt32LittleEndian(hashes.AsSpan(sizeof(uint), sizeof(uint)), hash2);
        return [.. header, .. hashes, .. directory, .. payloadBytes];
    }

    private static byte[] CreateStandardHeader(int dataSize, uint hash1, uint hash2)
    {
        var copyrightBytes = Encoding.ASCII.GetBytes(Copyright);
        if (StandardHeaderSize != 4 + sizeof(long) + sizeof(int) + copyrightBytes.Length)
        {
            throw new InvalidOperationException("PKG2 fixture copyright length must keep the WC header size stable.");
        }

        var bytes = new byte[StandardHeaderSize];
        Encoding.ASCII.GetBytes("PKG2", bytes);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(4, sizeof(long)), dataSize);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12, sizeof(int)), StandardHeaderSize);
        copyrightBytes.CopyTo(bytes.AsSpan(16));
        return bytes;
    }

    private static byte[] CreateModernHeader(int dataSize, uint hash1, uint hash2)
    {
        var bytes = new byte[ModernHeaderSize];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(0x5d + (i * 0x25));
        }

        ScatterUInt32(bytes, ModernHash1Offsets, hash1);
        ScatterUInt32(bytes, ModernHash2Offsets, hash2);
        ScatterUInt32(bytes, ModernDataSizeOffsets, checked((uint)dataSize));
        return bytes;
    }

    private static void ScatterUInt32(byte[] bytes, int[] offsets, uint value)
    {
        bytes[offsets[0]] = (byte)value;
        bytes[offsets[1]] = (byte)(value >> 8);
        bytes[offsets[2]] = (byte)(value >> 16);
        bytes[offsets[3]] = (byte)(value >> 24);
    }

    private static int EncryptEntryCount(int entryCount, uint hash1, uint hashVersion)
    {
        unchecked
        {
            var mixedHash = CalculateMixedHash(hash1, hashVersion);
            return (int)((uint)entryCount ^
                ((hash1 << 16) + (mixedHash & 0x7fffffffu) - (0x21524111u * hashVersion)));
        }
    }

    private static uint CalculateHashedOffset(
        uint hashOffsetPosition,
        uint targetOffset,
        int headerSize,
        uint hash1,
        uint hashVersion)
    {
        unchecked
        {
            var preHash = hash1 ^ hashVersion;
            var mixedHash = CalculateMixedHash(hash1, hashVersion);
            var offset = hashOffsetPosition - (uint)headerSize;
            offset = ~offset;
            offset *= preHash + (mixedHash ^ 0xA7E3C093u);
            offset -= 0x581C3F6Du;
            offset ^= hash1 * 0x01010101u;
            offset ^= mixedHash * 0x9E3779B9u;
            offset = RotateLeft(offset, (int)((preHash ^ mixedHash) & 0x1f));
            return ~((targetOffset - (uint)headerSize) ^ offset);
        }
    }

    private static uint CalculatePkg2StringKey(uint hash1, uint hashVersion)
    {
        unchecked
        {
            return Mix(Mix(hash1 ^ hashVersion ^ 0x6D4C3B2Au) ^ 0x4F4CB34Au);
        }
    }

    private static uint CalculateMixedHash(uint hash1, uint hashVersion)
    {
        unchecked
        {
            var preHash = hash1 ^ hashVersion;
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

    private static void WritePkg2String(List<byte> bytes, string value, uint pkg2StringKey)
    {
        var text = Encoding.Unicode.GetBytes(value);
        bytes.Add(unchecked((byte)-value.Length));
        Span<byte> keyBytes =
        [
            (byte)(pkg2StringKey & 0xff),
            (byte)((pkg2StringKey >> 8) & 0xff),
            (byte)((pkg2StringKey >> 8) & 0xff),
            (byte)((pkg2StringKey >> 16) & 0xff),
            (byte)((pkg2StringKey >> 16) & 0xff),
            (byte)((pkg2StringKey >> 24) & 0xff),
            (byte)((pkg2StringKey >> 24) & 0xff),
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
