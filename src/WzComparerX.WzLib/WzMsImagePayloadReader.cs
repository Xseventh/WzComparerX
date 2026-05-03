using System.Buffers.Binary;

namespace WzComparerX.WzLib;

public sealed class WzMsImagePayloadReader
{
    private const int InitialEncryptedLength = 1024;

    public async Task<MemoryStream> ReadAsync(
        string path,
        WzMsContainerInspection inspection,
        WzMsContainerEntryInspection entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(inspection);
        ArgumentNullException.ThrowIfNull(entry);

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 4096,
            FileOptions.SequentialScan);
        return Read(stream, inspection.Header, entry, cancellationToken);
    }

    public MemoryStream Read(
        Stream stream,
        WzMsContainerHeaderInspection header,
        WzMsContainerEntryInspection entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(entry);
        if (!stream.CanRead || !stream.CanSeek)
        {
            throw new ArgumentException("MS image payload extraction requires a readable and seekable stream.", nameof(stream));
        }

        ValidateEntryBounds(stream, entry);
        return header.EncryptionKind switch
        {
            WzMsContainerEncryptionKind.Snow => ReadSnow(stream, header, entry, cancellationToken),
            WzMsContainerEncryptionKind.ChaCha20 => ReadChaCha20(stream, header, entry, cancellationToken),
            _ => throw new NotSupportedException("MS image payload encryption kind is not supported.")
        };
    }

    private static MemoryStream ReadSnow(
        Stream stream,
        WzMsContainerHeaderInspection header,
        WzMsContainerEntryInspection entry,
        CancellationToken cancellationToken)
    {
        var encrypted = ReadPayloadBlock(stream, entry);
        var key = BuildImageKey(header.KeySalt, entry.Path, entry.Key, WzMsContainerEncryptionKind.Snow);
        var singlePassLength = AlignToSnowBlock(entry.Size);
        if (singlePassLength > entry.SizeAligned)
        {
            throw new InvalidDataException($"MS image entry aligned size is outside the payload block: {entry.Size}/{entry.SizeAligned}.");
        }

        if (singlePassLength > 0)
        {
            TransformSnow(encrypted.AsSpan(0, singlePassLength), key);
        }

        var firstLength = AlignToSnowBlock(Math.Min(entry.Size, InitialEncryptedLength));
        if (firstLength > 0)
        {
            TransformSnow(encrypted.AsSpan(0, firstLength), key);
        }

        cancellationToken.ThrowIfCancellationRequested();

        return new MemoryStream(encrypted.AsSpan(0, entry.Size).ToArray(), writable: false);
    }

    private static MemoryStream ReadChaCha20(
        Stream stream,
        WzMsContainerHeaderInspection header,
        WzMsContainerEntryInspection entry,
        CancellationToken cancellationToken)
    {
        var output = new byte[entry.Size];
        var initialLength = Math.Min(entry.Size, InitialEncryptedLength);
        if (initialLength > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var encryptedInitial = new byte[initialLength];
            stream.Position = entry.Offset;
            stream.ReadExactly(encryptedInitial);
            var key = BuildImageKey(header.KeySalt, entry.Path, entry.Key, WzMsContainerEncryptionKind.ChaCha20);
            BuildChaCha20ImageNonce(header.KeySalt, out var nonce, out var counter);
            WzMsChaCha20.Xor(encryptedInitial, output.AsSpan(0, initialLength), key, nonce, counter);
        }

        if (entry.Size > InitialEncryptedLength)
        {
            cancellationToken.ThrowIfCancellationRequested();
            stream.Position = entry.Offset + InitialEncryptedLength;
            stream.ReadExactly(output.AsSpan(InitialEncryptedLength));
        }

        return new MemoryStream(output, writable: false);
    }

    private static byte[] ReadPayloadBlock(Stream stream, WzMsContainerEntryInspection entry)
    {
        var encrypted = new byte[entry.SizeAligned];
        stream.Position = entry.Offset;
        stream.ReadExactly(encrypted);
        return encrypted;
    }

    private static void ValidateEntryBounds(Stream stream, WzMsContainerEntryInspection entry)
    {
        if (entry.Size < 0 || entry.SizeAligned < entry.Size)
        {
            throw new InvalidDataException($"MS image entry has invalid size values: {entry.Size}/{entry.SizeAligned}.");
        }

        if (entry.Offset < 0 || entry.Offset >= stream.Length)
        {
            throw new InvalidDataException($"MS image entry offset is outside the file: {entry.Offset}.");
        }

        var endOffset = entry.Offset + entry.SizeAligned;
        if (endOffset > stream.Length)
        {
            throw new InvalidDataException($"MS image entry extends past the file: {endOffset}.");
        }
    }

    private static byte[] BuildImageKey(
        string keySalt,
        string entryName,
        IReadOnlyList<byte> entryKey,
        WzMsContainerEncryptionKind encryptionKind)
    {
        if (string.IsNullOrEmpty(entryName))
        {
            throw new InvalidDataException("MS image entry name is empty.");
        }

        if (entryKey.Count == 0)
        {
            throw new InvalidDataException("MS image entry key is empty.");
        }

        var keyHash = CalculateKeyHash(keySalt);
        var keyHashDigits = keyHash.ToString().Select(static value => (byte)(value - '0')).ToArray();
        var keyLength = encryptionKind == WzMsContainerEncryptionKind.ChaCha20
            ? WzMsChaCha20.KeyLength
            : 16;
        var key = new byte[keyLength];
        for (var i = 0; i < key.Length; i++)
        {
            key[i] = (byte)(i + entryName[i % entryName.Length] * (
                keyHashDigits[i % keyHashDigits.Length] % 2 +
                entryKey[(keyHashDigits[(i + 2) % keyHashDigits.Length] + i) % entryKey.Count] +
                (keyHashDigits[(i + 1) % keyHashDigits.Length] + i) % 5));
        }

        if (encryptionKind == WzMsContainerEncryptionKind.ChaCha20)
        {
            for (var i = 0; i < key.Length; i++)
            {
                key[i] ^= ChaCha20KeyObscure[i];
            }
        }

        return key;
    }

    private static void BuildChaCha20ImageNonce(
        string keySalt,
        out byte[] nonce,
        out uint counter)
    {
        var keyHash = CalculateKeyHash(keySalt);
        var keyHash2 = keyHash >> 1;
        var keyHash3 = keyHash2 ^ 0x6C;
        Span<byte> keyHashData = stackalloc byte[12];
        BinaryPrimitives.WriteUInt32LittleEndian(keyHashData[..4], keyHash);
        BinaryPrimitives.WriteUInt32LittleEndian(keyHashData.Slice(4, 4), keyHash2);
        BinaryPrimitives.WriteUInt32LittleEndian(keyHashData.Slice(8, 4), keyHash3);
        for (uint i = 0, a = 0, b = 0, c = 90, d = 0; i < 12; i++)
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

    private static uint CalculateKeyHash(string keySalt)
    {
        uint keyHash = 0x811C9DC5;
        foreach (var value in keySalt)
        {
            keyHash = (keyHash ^ value) * 0x1000193;
        }

        return keyHash;
    }

    private static int AlignToSnowBlock(int length)
    {
        return (length & 3) == 0
            ? length
            : length - (length & 3) + 4;
    }

    private static void TransformSnow(Span<byte> buffer, ReadOnlySpan<byte> key)
    {
        if (buffer.IsEmpty)
        {
            return;
        }

        using var transform = new WzSnow2CryptoTransform(key, [], encrypting: false);
        var input = buffer.ToArray();
        transform.TransformBlock(input, 0, input.Length, input, 0);
        input.CopyTo(buffer);
    }

    private static readonly byte[] ChaCha20KeyObscure =
    [
        0x7B, 0x2F, 0x35, 0x48, 0x43, 0x95, 0x02, 0xB9,
        0xAE, 0x91, 0xA6, 0xE1, 0xD8, 0xD6, 0x24, 0xB4,
        0x33, 0x10, 0x1D, 0x3D, 0xC1, 0xBB, 0xC6, 0xF4,
        0xA5, 0xFE, 0xB3, 0x69, 0x6B, 0x56, 0xE4, 0x75
    ];
}
