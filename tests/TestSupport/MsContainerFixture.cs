using System.Buffers.Binary;
using System.IO.Compression;
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

        var entryTableChunks = new List<byte[]>();
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            AddStringChunks(entryTableChunks, entry.Path);
            entryTableChunks.Add(BitConverter.GetBytes(100 + i));
            entryTableChunks.Add(BitConverter.GetBytes(entry.Flags));
            entryTableChunks.Add(BitConverter.GetBytes(entry.RelativeBlock));
            entryTableChunks.Add(BitConverter.GetBytes(entry.Size));
            entryTableChunks.Add(BitConverter.GetBytes(entry.SizeAligned));
            entryTableChunks.Add(BitConverter.GetBytes(entry.Unknown1));
            entryTableChunks.Add(BitConverter.GetBytes(entry.Unknown2));
            entryTableChunks.Add(CreateEntryKey(i));
            entryTableChunks.Add(BitConverter.GetBytes(entry.Unknown3));
            entryTableChunks.Add(BitConverter.GetBytes(entry.Unknown4));
        }

        bytes.AddRange(EncryptV4EntryTable(fileName, entryTableChunks));

        while ((bytes.Count & (Alignment - 1)) != 0)
        {
            bytes.Add(0);
        }

        var maxEnd = entries
            .Select(entry => entry.RelativeBlock * Alignment + entry.SizeAligned)
            .DefaultIfEmpty(0)
            .Max();
        var payloads = new byte[maxEnd];
        for (var i = 0; i < entries.Length; i++)
        {
            WritePayload(
                payloads,
                entries[i],
                EncryptV4Payload(fileName, entries[i].Path, CreateEntryKey(i), entries[i].Payload));
        }

        bytes.AddRange(payloads);
        return bytes.ToArray();
    }

    private static byte[] EncryptV4EntryTable(string fileName, IReadOnlyList<byte[]> chunks)
    {
        Span<byte> entryKey = stackalloc byte[WzMsChaCha20.KeyLength];
        BuildEntryTableKey(fileName, entryKey);
        var key = entryKey.ToArray();
        var encryptedEntryTable = new List<byte>();
        var plainBlock = new byte[WzMsChaCha20.BlockLength];
        var encryptedBlock = new byte[WzMsChaCha20.BlockLength];
        var position = 0;
        uint counter = 0;

        foreach (var chunk in chunks)
        {
            WriteChunk(chunk);
        }

        if (position > 0)
        {
            FlushBlock();
        }

        return encryptedEntryTable.ToArray();

        void WriteChunk(ReadOnlySpan<byte> chunk)
        {
            while (!chunk.IsEmpty)
            {
                var count = Math.Min(chunk.Length, plainBlock.Length - position);
                chunk[..count].CopyTo(plainBlock.AsSpan(position));
                position += count;
                chunk = chunk[count..];

                if (position >= plainBlock.Length)
                {
                    FlushBlock();
                }
            }

            if (position == 0)
            {
                counter = 0;
            }
        }

        void FlushBlock()
        {
            WzMsChaCha20.XorBlock(plainBlock, encryptedBlock, key, counter);
            encryptedEntryTable.AddRange(encryptedBlock);
            Array.Clear(plainBlock);
            position = 0;
            counter++;
        }
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
            entryTable.AddRange(CreateEntryKey(i));
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
        var payloads = new byte[maxEnd];
        for (var i = 0; i < entries.Length; i++)
        {
            WritePayload(
                payloads,
                entries[i],
                EncryptV2Payload(entries[i].Path, CreateEntryKey(i), entries[i].Payload));
        }

        bytes.AddRange(payloads);
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
        int Unknown4 = 0,
        byte[]? Payload = null);

    public static byte[] CreatePropertyImage(params byte[][] entries)
    {
        var bytes = new List<byte>(CreateImage("Property"));
        bytes.Add(0x00);
        bytes.Add(0x00);
        bytes.Add((byte)entries.Length);
        foreach (var entry in entries)
        {
            bytes.AddRange(entry);
        }

        return bytes.ToArray();
    }

    public static byte[] CreateScalarProperty(string name, int value)
    {
        var bytes = new List<byte>();
        bytes.AddRange(CreateImageString(name));
        bytes.Add(0x03);
        bytes.Add((byte)value);
        return bytes.ToArray();
    }

    public static byte[] CreateCanvasPropertyImage(
        string propertyName,
        byte[] pixels,
        int width = 1,
        int height = 1,
        int format = 2)
    {
        return CreatePropertyImage(CreateObjectProperty(
            propertyName,
            CreateCanvasObjectValue(pixels, width, height, format)));
    }

    public static byte[] CreateSpinePropertyImage(
        string propertyName,
        string spineName,
        string atlasText,
        byte[] skeleton,
        params (string Name, byte[] Pixels, int Width, int Height)[] pages)
    {
        var children = new List<byte[]>
        {
            CreateStringProperty($"{spineName}.atlas", atlasText)
        };
        children.AddRange(pages.Select(page =>
            CreateObjectProperty(
                page.Name,
                CreateCanvasObjectValue(page.Pixels, page.Width, page.Height, format: 2))));
        children.Add(CreateRawDataProperty($"{spineName}.skel", skeleton));
        children.Add(CreateStringProperty("spine", spineName));

        return CreatePropertyImage(
            CreateObjectProperty(
                propertyName,
                CreateObjectValue(
                    "Property",
                    0x00,
                    0x00,
                    children.Count,
                    children.SelectMany(static child => child).ToArray())));
    }

    public static byte[] CreateLinkedCanvasPropertyImage(
        string propertyName,
        string linkName,
        string linkValue)
    {
        byte[] pixels = [0x00, 0x00, 0x00, 0x00];
        var payload = CreateDirectZlibPayload(pixels);
        return CreatePropertyImage(CreateObjectProperty(
            propertyName,
            CreateObjectValue(
                "Canvas",
                0x00,
                0x01,
                0x00,
                0x00,
                1,
                CreateImageString(linkName),
                0x08,
                CreateImageString(linkValue),
                1,
                1,
                2,
                0x00,
                1,
                0,
                (byte)0x00,
                (byte)0x00,
                BitConverter.GetBytes(payload.Length),
                payload)));
    }

    public static byte[] CreateUolToLinkedCanvasPropertyImage(
        string uolParentName,
        string uolPropertyName,
        string uolTarget,
        string canvasParentName,
        string canvasPropertyName,
        string linkName,
        string linkValue)
    {
        return CreatePropertyImage(
            CreateObjectProperty(
                uolParentName,
                CreateObjectValue(
                    "Property",
                    0x00,
                    0x00,
                    1,
                    CreateObjectProperty(
                        uolPropertyName,
                        CreateObjectValue(
                            "UOL",
                            0x00,
                            CreateImageString(uolTarget))))),
            CreateObjectProperty(
                canvasParentName,
                CreateObjectValue(
                    "Property",
                    0x00,
                    0x00,
                    1,
                    CreateLinkedCanvasProperty(
                        canvasPropertyName,
                        linkName,
                        linkValue))));
    }

    private static void AddString(List<byte> bytes, string value)
    {
        bytes.AddRange(BitConverter.GetBytes(value.Length));
        bytes.AddRange(Encoding.Unicode.GetBytes(value));
    }

    private static void AddStringChunks(List<byte[]> chunks, string value)
    {
        chunks.Add(BitConverter.GetBytes(value.Length));
        chunks.Add(Encoding.Unicode.GetBytes(value));
    }

    private static byte[] CreateObjectProperty(string name, byte[] objectValue)
    {
        var bytes = new List<byte>();
        bytes.AddRange(CreateImageString(name));
        bytes.Add(0x09);
        bytes.AddRange(BitConverter.GetBytes(objectValue.Length));
        bytes.AddRange(objectValue);
        return bytes.ToArray();
    }

    private static byte[] CreateStringProperty(string name, string value)
    {
        var bytes = new List<byte>();
        bytes.AddRange(CreateImageString(name));
        bytes.Add(0x08);
        bytes.AddRange(CreateImageString(value));
        return bytes.ToArray();
    }

    private static byte[] CreateRawDataProperty(string name, byte[] payload)
    {
        return CreateObjectProperty(
            name,
            CreateObjectValue(
                "RawData",
                0x00,
                CreateCompressedInt32(payload.Length),
                payload));
    }

    private static byte[] CreateLinkedCanvasProperty(string name, string linkName, string linkValue)
    {
        byte[] pixels = [0x00, 0x00, 0x00, 0x00];
        var payload = CreateDirectZlibPayload(pixels);
        return CreateObjectProperty(
            name,
            CreateObjectValue(
                "Canvas",
                0x00,
                0x01,
                0x00,
                0x00,
                1,
                CreateImageString(linkName),
                0x08,
                CreateImageString(linkValue),
                1,
                1,
                2,
                0x00,
                1,
                0,
                (byte)0x00,
                (byte)0x00,
                BitConverter.GetBytes(payload.Length),
                payload));
    }

    private static byte[] CreateCanvasObjectValue(
        byte[] pixels,
        int width,
        int height,
        int format)
    {
        var payload = CreateDirectZlibPayload(pixels);
        return CreateObjectValue(
            "Canvas",
            0x00,
            0x00,
            width,
            height,
            format,
            0x00,
            1,
            0,
            (byte)0x00,
            (byte)0x00,
            BitConverter.GetBytes(payload.Length),
            payload);
    }

    private static byte[] CreateObjectValue(string objectType, params object[] payloadParts)
    {
        var bytes = new List<byte>();
        AddImageObjectName(bytes, objectType);
        AddPayloadParts(bytes, payloadParts);
        return bytes.ToArray();
    }

    private static byte[] CreateDirectZlibPayload(byte[] pixels)
    {
        using var output = new MemoryStream();
        output.WriteByte(0x00);
        using (var zlib = new ZLibStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            zlib.Write(pixels);
        }

        return output.ToArray();
    }

    private static byte[] CreateCompressedInt32(int value)
    {
        return value is >= sbyte.MinValue and <= sbyte.MaxValue
            ? [(byte)value]
            : [0x80, .. BitConverter.GetBytes(value)];
    }

    private static void AddPayloadParts(List<byte> bytes, params object[] payloadParts)
    {
        foreach (var part in payloadParts)
        {
            switch (part)
            {
                case byte value:
                    bytes.Add(value);
                    break;
                case int value:
                    bytes.Add((byte)value);
                    break;
                case byte[] value:
                    bytes.AddRange(value);
                    break;
                default:
                    throw new ArgumentException($"Unsupported payload part type: {part.GetType()}.");
            }
        }
    }

    private static void AddImageObjectName(List<byte> bytes, string value)
    {
        bytes.Add(0x73);
        AddWzString(bytes, value);
    }

    private static byte[] CreateEntryKey(int index)
    {
        return Enumerable.Repeat((byte)index, 16).ToArray();
    }

    private static void WritePayload(byte[] payloads, Entry entry, byte[] encryptedPayload)
    {
        if (encryptedPayload.Length == 0)
        {
            return;
        }

        encryptedPayload.CopyTo(payloads.AsSpan(entry.RelativeBlock * Alignment));
    }

    private static byte[] EncryptV2Payload(string entryName, byte[] entryKey, byte[]? payload)
    {
        if (payload is null)
        {
            return [];
        }

        var encrypted = new byte[((payload.Length + 3) / 4) * 4];
        payload.CopyTo(encrypted, 0);
        var key = BuildImageKey(string.Empty, entryName, entryKey, chaCha20: false);
        var firstLength = AlignToSnowBlock(Math.Min(payload.Length, 1024));
        if (firstLength > 0)
        {
            TransformSnow(encrypted.AsSpan(0, firstLength), key, encrypting: true);
        }

        if (encrypted.Length > 0)
        {
            TransformSnow(encrypted, key, encrypting: true);
        }

        return encrypted;
    }

    private static byte[] EncryptV4Payload(string fileName, string entryName, byte[] entryKey, byte[]? payload)
    {
        _ = fileName;
        if (payload is null)
        {
            return [];
        }

        var encrypted = payload.ToArray();
        var initialLength = Math.Min(encrypted.Length, 1024);
        if (initialLength > 0)
        {
            var key = BuildImageKey(string.Empty, entryName, entryKey, chaCha20: true);
            BuildChaCha20ImageNonce(string.Empty, out var nonce, out var counter);
            WzMsChaCha20.Xor(payload.AsSpan(0, initialLength), encrypted.AsSpan(0, initialLength), key, nonce, counter);
        }

        return encrypted;
    }

    private static byte[] BuildImageKey(string keySalt, string entryName, byte[] entryKey, bool chaCha20)
    {
        uint keyHash = 0x811C9DC5;
        foreach (var value in keySalt)
        {
            keyHash = (keyHash ^ value) * 0x1000193;
        }

        var keyHashDigits = keyHash.ToString().Select(static value => (byte)(value - '0')).ToArray();
        var key = new byte[chaCha20 ? WzMsChaCha20.KeyLength : 16];
        for (var i = 0; i < key.Length; i++)
        {
            key[i] = (byte)(i + entryName[i % entryName.Length] * (
                keyHashDigits[i % keyHashDigits.Length] % 2 +
                entryKey[(keyHashDigits[(i + 2) % keyHashDigits.Length] + i) % entryKey.Length] +
                (keyHashDigits[(i + 1) % keyHashDigits.Length] + i) % 5));
        }

        if (chaCha20)
        {
            for (var i = 0; i < key.Length; i++)
            {
                key[i] ^= ChaCha20KeyObscure[i];
            }
        }

        return key;
    }

    private static void BuildChaCha20ImageNonce(string keySalt, out byte[] nonce, out uint counter)
    {
        uint keyHash = 0x811C9DC5;
        foreach (var value in keySalt)
        {
            keyHash = (keyHash ^ value) * 0x1000193;
        }

        var keyHash2 = keyHash >> 1;
        var keyHash3 = keyHash2 ^ 0x6C;
        Span<byte> keyHashData = stackalloc byte[12];
        BinaryPrimitives.WriteUInt32LittleEndian(keyHashData[..4], keyHash);
        BinaryPrimitives.WriteUInt32LittleEndian(keyHashData.Slice(4, 4), keyHash2);
        BinaryPrimitives.WriteUInt32LittleEndian(keyHashData.Slice(8, 4), keyHash3);
        for (uint i = 0, a = 0, b = 0, c = 90, d = 0; i < 12; ++i)
        {
            keyHashData[(int)i] ^= (byte)(d + 11 * (i / 11) + (c ^ (i >> 2)) + (a ^ b));
            d--;
            a += 8;
            b += 17;
            c += 43;
        }

        nonce = new byte[WzMsChaCha20.NonceLength];
        keyHashData[..8].CopyTo(nonce.AsSpan(4));
        counter = BinaryPrimitives.ReadUInt32LittleEndian(keyHashData.Slice(8, 4));
    }

    private static int AlignToSnowBlock(int length)
    {
        return (length & 3) == 0 ? length : length - (length & 3) + 4;
    }

    private static void TransformSnow(Span<byte> buffer, byte[] key, bool encrypting)
    {
        if (buffer.IsEmpty)
        {
            return;
        }

        using var transform = new WzSnow2CryptoTransform(key, [], encrypting);
        var input = buffer.ToArray();
        transform.TransformBlock(input, 0, input.Length, input, 0);
        input.CopyTo(buffer);
    }

    private static byte[] CreateImage(string objectType)
    {
        var bytes = new List<byte>();
        bytes.Add(0x73);
        AddWzString(bytes, objectType);
        return bytes.ToArray();
    }

    private static byte[] CreateImageString(string value)
    {
        var bytes = new List<byte> { 0x00 };
        AddWzString(bytes, value);
        return bytes.ToArray();
    }

    private static void AddWzString(List<byte> bytes, string value)
    {
        bytes.Add(unchecked((byte)(sbyte)-value.Length));
        for (var i = 0; i < value.Length; i++)
        {
            bytes.Add((byte)(value[i] ^ (byte)(0xAA + i)));
        }
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
