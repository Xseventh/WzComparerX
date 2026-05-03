using System.Buffers.Binary;
using System.Text;
using WzComparerX.WzLib;

namespace WzComparerX.Tests;

internal static class MsContainerFixture
{
    private const int Alignment = 1024;

    private static readonly byte[] ChaCha20KeyObscure =
    [
        0x7B, 0x2F, 0x35, 0x48, 0x43, 0x95, 0x02, 0xB9,
        0xAE, 0x91, 0xA6, 0xE1, 0xD8, 0xD6, 0x24, 0xB4,
        0x33, 0x10, 0x1D, 0x3D, 0xC1, 0xBB, 0xC6, 0xF4,
        0xA5, 0xFE, 0xB3, 0x69, 0x6B, 0x56, 0xE4, 0x75
    ];

    public static byte[] CreateV4(string fileName, params Entry[] entries)
    {
        fileName = fileName.ToLowerInvariant();
        var randomByteCount = CalculateRandomByteCount(fileName);
        var bytes = new List<byte>();
        bytes.AddRange(Enumerable.Repeat((byte)0x08, randomByteCount));

        const byte shiftedRandomByte = 0x04;
        bytes.Add((byte)(4 ^ shiftedRandomByte));
        bytes.AddRange(BitConverter.GetBytes((int)shiftedRandomByte));

        Span<byte> headerKey = stackalloc byte[WzMsChaCha20.KeyLength];
        BuildHeaderKey(fileName, headerKey);
        Span<byte> plainHeader = stackalloc byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(plainHeader[..4], 0x12345678);
        BinaryPrimitives.WriteInt32LittleEndian(plainHeader[4..], entries.Length);
        Span<byte> encryptedHeader = stackalloc byte[8];
        WzMsChaCha20.XorBlock(plainHeader, encryptedHeader, headerKey);
        bytes.AddRange(encryptedHeader.ToArray());

        var padding = CalculateEntryPadding(fileName) + 64;
        bytes.AddRange(Enumerable.Repeat((byte)0, padding));

        var entryTable = new List<byte>();
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            AddString(entryTable, entry.Path);
            entryTable.AddRange(BitConverter.GetBytes(100 + i));
            entryTable.AddRange(BitConverter.GetBytes(entry.Flags));
            entryTable.AddRange(BitConverter.GetBytes(entry.RelativeBlock));
            entryTable.AddRange(BitConverter.GetBytes(entry.Size));
            entryTable.AddRange(BitConverter.GetBytes(entry.SizeAligned));
            entryTable.AddRange(BitConverter.GetBytes(entry.Unknown1));
            entryTable.AddRange(BitConverter.GetBytes(entry.Unknown2));
            entryTable.AddRange(Enumerable.Repeat((byte)i, 16));
            entryTable.AddRange(BitConverter.GetBytes(entry.Unknown3));
            entryTable.AddRange(BitConverter.GetBytes(entry.Unknown4));
        }

        while ((entryTable.Count & (WzMsChaCha20.BlockLength - 1)) != 0)
        {
            entryTable.Add(0);
        }

        Span<byte> entryKey = stackalloc byte[WzMsChaCha20.KeyLength];
        BuildEntryTableKey(fileName, entryKey);
        var plainEntryTable = entryTable.ToArray();
        var encryptedBlock = new byte[WzMsChaCha20.BlockLength];
        for (var offset = 0; offset < entryTable.Count; offset += WzMsChaCha20.BlockLength)
        {
            WzMsChaCha20.XorBlock(
                plainEntryTable.AsSpan(offset, WzMsChaCha20.BlockLength),
                encryptedBlock,
                entryKey);
            bytes.AddRange(encryptedBlock);
        }

        while ((bytes.Count & (Alignment - 1)) != 0)
        {
            bytes.Add(0);
        }

        var maxEnd = entries
            .Select(entry => entry.RelativeBlock * Alignment + entry.SizeAligned)
            .DefaultIfEmpty(0)
            .Max();
        bytes.AddRange(Enumerable.Repeat((byte)0, maxEnd));
        return bytes.ToArray();
    }

    public static byte[] CreateV2(string fileName, params Entry[] entries)
    {
        fileName = fileName.ToLowerInvariant();
        var randomByteCount = CalculateRandomByteCount(fileName);
        var bytes = new List<byte>();
        bytes.AddRange(Enumerable.Repeat((byte)0x08, randomByteCount));
        bytes.AddRange(BitConverter.GetBytes(0x08));

        Span<byte> headerKey = stackalloc byte[16];
        BuildSnowHeaderKey(fileName, headerKey);
        var plainHeader = new byte[12];
        var headerHash = 0x08 + 2 + entries.Length;
        BinaryPrimitives.WriteInt32LittleEndian(plainHeader.AsSpan(0, 4), headerHash);
        plainHeader[4] = 2;
        BinaryPrimitives.WriteInt32LittleEndian(plainHeader.AsSpan(5, 4), entries.Length);
        var encryptedHeader = new byte[12];
        using (var snow = new WzSnow2CryptoTransform(headerKey, [], encrypting: true))
        {
            snow.TransformBlock(plainHeader, 0, plainHeader.Length, encryptedHeader, 0);
        }

        bytes.AddRange(encryptedHeader);

        var padding = CalculateEntryPadding(fileName) + 30;
        bytes.AddRange(Enumerable.Repeat((byte)0, padding));

        var entryTable = new List<byte>();
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            AddString(entryTable, entry.Path);
            entryTable.AddRange(BitConverter.GetBytes(100 + i));
            entryTable.AddRange(BitConverter.GetBytes(entry.Flags));
            entryTable.AddRange(BitConverter.GetBytes(entry.RelativeBlock));
            entryTable.AddRange(BitConverter.GetBytes(entry.Size));
            entryTable.AddRange(BitConverter.GetBytes(entry.SizeAligned));
            entryTable.AddRange(BitConverter.GetBytes(entry.Unknown1));
            entryTable.AddRange(BitConverter.GetBytes(entry.Unknown2));
            entryTable.AddRange(Enumerable.Repeat((byte)i, 16));
        }

        while ((entryTable.Count & 3) != 0)
        {
            entryTable.Add(0);
        }

        Span<byte> entryKey = stackalloc byte[16];
        BuildSnowEntryTableKey(fileName, entryKey);
        var plainEntryTable = entryTable.ToArray();
        var encryptedEntryTable = new byte[plainEntryTable.Length];
        using (var snow = new WzSnow2CryptoTransform(entryKey, [], encrypting: true))
        {
            snow.TransformBlock(plainEntryTable, 0, plainEntryTable.Length, encryptedEntryTable, 0);
        }

        bytes.AddRange(encryptedEntryTable);

        while ((bytes.Count & (Alignment - 1)) != 0)
        {
            bytes.Add(0);
        }

        var maxEnd = entries
            .Select(entry => entry.RelativeBlock * Alignment + entry.SizeAligned)
            .DefaultIfEmpty(0)
            .Max();
        bytes.AddRange(Enumerable.Repeat((byte)0, maxEnd));
        return bytes.ToArray();
    }

    public sealed record Entry(
        string Path,
        int RelativeBlock,
        int Size,
        int SizeAligned,
        int Flags = 0,
        int Unknown1 = 0,
        int Unknown2 = 0,
        int Unknown3 = 0,
        int Unknown4 = 0);

    private static void AddString(List<byte> bytes, string value)
    {
        bytes.AddRange(BitConverter.GetBytes(value.Length));
        bytes.AddRange(Encoding.Unicode.GetBytes(value));
    }

    private static void BuildHeaderKey(string fileNameWithSalt, Span<byte> key)
    {
        for (var i = 0; i < key.Length; i++)
        {
            key[i] = (byte)(fileNameWithSalt[i % fileNameWithSalt.Length] + i);
            key[i] ^= ChaCha20KeyObscure[i];
        }
    }

    private static void BuildEntryTableKey(string fileNameWithSalt, Span<byte> key)
    {
        for (var i = 0; i < key.Length; i++)
        {
            key[i] = (byte)(i + (i % 3 + 2) * fileNameWithSalt[fileNameWithSalt.Length - 1 - i % fileNameWithSalt.Length]);
            key[i] ^= ChaCha20KeyObscure[i];
        }
    }

    private static void BuildSnowHeaderKey(string fileNameWithSalt, Span<byte> key)
    {
        for (var i = 0; i < key.Length; i++)
        {
            key[i] = (byte)(fileNameWithSalt[i % fileNameWithSalt.Length] + i);
        }
    }

    private static void BuildSnowEntryTableKey(string fileNameWithSalt, Span<byte> key)
    {
        for (var i = 0; i < key.Length; i++)
        {
            key[i] = (byte)(i + (i % 3 + 2) * fileNameWithSalt[fileNameWithSalt.Length - 1 - i % fileNameWithSalt.Length]);
        }
    }

    private static int CalculateRandomByteCount(string fileName)
    {
        return fileName.Sum(static c => c) % 312 + 30;
    }

    private static int CalculateEntryPadding(string fileName)
    {
        return fileName.Sum(static c => c * 3) % 212;
    }
}
