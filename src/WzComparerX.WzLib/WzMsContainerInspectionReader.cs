using System.Buffers.Binary;
using System.Text;

namespace WzComparerX.WzLib;

public sealed class WzMsContainerInspectionReader
{
    private const int SnowVersion = 2;
    private const int ChaCha20Version = 4;
    private const int Alignment = 1024;
    private const int MaxEntryCount = 1_000_000;
    private const int MaxEntryNameLength = 16_384;

    private static readonly byte[] ChaCha20KeyObscure =
    [
        0x7B, 0x2F, 0x35, 0x48, 0x43, 0x95, 0x02, 0xB9,
        0xAE, 0x91, 0xA6, 0xE1, 0xD8, 0xD6, 0x24, 0xB4,
        0x33, 0x10, 0x1D, 0x3D, 0xC1, 0xBB, 0xC6, 0xF4,
        0xA5, 0xFE, 0xB3, 0x69, 0x6B, 0x56, 0xE4, 0x75
    ];

    public async Task<WzMsContainerInspection> ReadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 4096,
            FileOptions.SequentialScan);
        return Read(stream, path, cancellationToken);
    }

    public WzMsContainerInspection Read(
        Stream stream,
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        if (!stream.CanRead || !stream.CanSeek)
        {
            throw new ArgumentException("MS inspection requires a readable and seekable stream.", nameof(stream));
        }

        var context = ReadHeader(stream, sourcePath);
        var entries = ReadEntries(stream, context, cancellationToken);
        return new WzMsContainerInspection(context.Header with
        {
            DataStartPosition = entries.DataStartPosition
        }, entries.Items);
    }

    private static MsContainerReadContext ReadHeader(Stream stream, string sourcePath)
    {
        var firstFailure = default(Exception);
        try
        {
            return ReadSnowHeader(stream, sourcePath);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException or EndOfStreamException)
        {
            firstFailure = ex;
        }

        try
        {
            return ReadChaCha20Header(stream, sourcePath);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException or EndOfStreamException)
        {
            throw new NotSupportedException(
                "MS container header is not supported by the implemented Snow or ChaCha20 readers.",
                new AggregateException(firstFailure!, ex));
        }
    }

    private static MsContainerReadContext ReadSnowHeader(Stream stream, string sourcePath)
    {
        var fileName = Path.GetFileName(sourcePath).ToLowerInvariant();
        if (string.IsNullOrEmpty(fileName))
        {
            throw new InvalidDataException("MS container path does not contain a file name.");
        }

        stream.Position = 0;
        var randomByteCount = CalculateRandomByteCount(fileName);
        var randomBytes = ReadBytes(stream, randomByteCount);
        var hashedSaltLength = ReadInt32(stream);
        var saltLength = ((byte)hashedSaltLength) ^ randomBytes[0];
        if (saltLength > randomBytes.Length)
        {
            throw new InvalidDataException(
                $"MS salt length {saltLength} exceeds the random-byte prefix length {randomBytes.Length}.");
        }

        var saltBytes = ReadBytes(stream, checked(saltLength * 2));
        var saltChars = new char[saltLength];
        for (var i = 0; i < saltLength; i++)
        {
            saltChars[i] = (char)(randomBytes[i] ^ saltBytes[i * 2]);
        }

        var fileNameWithSalt = fileName + new string(saltChars);
        var headerStartPosition = stream.Position;
        Span<byte> headerKey = stackalloc byte[16];
        BuildSnowHeaderKey(fileNameWithSalt, headerKey);

        Span<byte> encryptedHeader = stackalloc byte[12];
        ReadExactly(stream, encryptedHeader);
        var encryptedHeaderArray = encryptedHeader.ToArray();
        var decryptedHeaderArray = new byte[12];
        using (var snow = new WzSnow2CryptoTransform(headerKey, [], encrypting: false))
        {
            snow.TransformBlock(encryptedHeaderArray, 0, encryptedHeaderArray.Length, decryptedHeaderArray, 0);
        }

        var headerHash = BinaryPrimitives.ReadInt32LittleEndian(decryptedHeaderArray.AsSpan(0, 4));
        var version = decryptedHeaderArray[4];
        if (version != SnowVersion)
        {
            throw new NotSupportedException(
                $"MS container version {version} is not supported by the Snow reader; expected {SnowVersion}.");
        }

        var entryCount = BinaryPrimitives.ReadInt32LittleEndian(decryptedHeaderArray.AsSpan(5, 4));
        ValidateEntryCount(entryCount);
        ValidateSnowHeaderHash(headerHash, hashedSaltLength, saltBytes, version, entryCount);

        var entryStartPosition = headerStartPosition + 9 + CalculateEntryPadding(fileName) + 33;
        ValidateEntryStart(stream, entryStartPosition);
        return new MsContainerReadContext(
            new WzMsContainerHeaderInspection(
                Path.GetFullPath(sourcePath),
                version,
                headerHash,
                entryCount,
                headerStartPosition,
                entryStartPosition,
                DataStartPosition: -1,
                randomByteCount,
                saltLength,
                stream.Length)
            {
                EncryptionKind = WzMsContainerEncryptionKind.Snow,
                KeySalt = new string(saltChars),
                FileNameWithSalt = fileNameWithSalt
            },
            fileNameWithSalt,
            WzMsContainerEncryptionKind.Snow);
    }

    private static MsContainerReadContext ReadChaCha20Header(Stream stream, string sourcePath)
    {
        var fileName = Path.GetFileName(sourcePath).ToLowerInvariant();
        if (string.IsNullOrEmpty(fileName))
        {
            throw new InvalidDataException("MS container path does not contain a file name.");
        }

        stream.Position = 0;
        var randomByteCount = CalculateRandomByteCount(fileName);
        var randomBytes = ReadBytes(stream, randomByteCount);
        for (var i = 0; i < randomBytes.Length; i++)
        {
            randomBytes[i] = (byte)((sbyte)randomBytes[i] >> 1);
        }

        var encodedVersion = ReadByte(stream);
        var version = encodedVersion ^ randomBytes[0];
        if (version != ChaCha20Version)
        {
            throw new NotSupportedException(
                $"MS container version {version} is not supported by the ChaCha20 reader; expected {ChaCha20Version}.");
        }

        var hashedSaltLength = ReadInt32(stream);
        var saltLength = ((byte)hashedSaltLength) ^ randomBytes[0];
        if (saltLength > randomBytes.Length)
        {
            throw new InvalidDataException(
                $"MS salt length {saltLength} exceeds the random-byte prefix length {randomBytes.Length}.");
        }

        var saltBytes = ReadBytes(stream, checked(saltLength * 2));
        var saltChars = new char[saltLength];
        for (var i = 0; i < saltLength; i++)
        {
            var decoded = randomBytes[i] ^ saltBytes[i * 2];
            saltChars[i] = (char)(((decoded | 0x4B) << 1) - decoded - 75);
        }

        var salt = new string(saltChars);
        var fileNameWithSalt = fileName + salt;
        var headerStartPosition = stream.Position;
        Span<byte> headerKey = stackalloc byte[WzMsChaCha20.KeyLength];
        BuildChaCha20HeaderKey(fileNameWithSalt, headerKey);

        Span<byte> encryptedHeader = stackalloc byte[8];
        ReadExactly(stream, encryptedHeader);
        Span<byte> decryptedHeader = stackalloc byte[8];
        WzMsChaCha20.XorBlock(encryptedHeader, decryptedHeader, headerKey);
        var headerHash = BinaryPrimitives.ReadInt32LittleEndian(decryptedHeader.Slice(0, 4));
        var entryCount = BinaryPrimitives.ReadInt32LittleEndian(decryptedHeader.Slice(4, 4));
        ValidateEntryCount(entryCount);

        var entryStartPosition = headerStartPosition + 8 + CalculateEntryPadding(fileName) + 64;
        ValidateEntryStart(stream, entryStartPosition);

        return new MsContainerReadContext(
            new WzMsContainerHeaderInspection(
                Path.GetFullPath(sourcePath),
                version,
                headerHash,
                entryCount,
                headerStartPosition,
                entryStartPosition,
                DataStartPosition: -1,
                randomByteCount,
                saltLength,
                stream.Length)
            {
                EncryptionKind = WzMsContainerEncryptionKind.ChaCha20,
                KeySalt = salt,
                FileNameWithSalt = fileNameWithSalt
            },
            fileNameWithSalt,
            WzMsContainerEncryptionKind.ChaCha20);
    }

    private static (IReadOnlyList<WzMsContainerEntryInspection> Items, long DataStartPosition) ReadEntries(
        Stream stream,
        MsContainerReadContext context,
        CancellationToken cancellationToken)
    {
        var header = context.Header;
        if (header.EntryCount == 0)
        {
            return (Array.Empty<WzMsContainerEntryInspection>(), Align(header.EntryStartPosition));
        }

        stream.Position = header.EntryStartPosition;
        using var reader = CreateEntryReader(stream, context);
        var entries = new List<WzMsContainerEntryInspection>(header.EntryCount);
        for (var i = 0; i < header.EntryCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entryName = reader.ReadString(MaxEntryNameLength);
            var checkSum = reader.ReadInt32();
            var flags = reader.ReadInt32();
            var relativeBlock = reader.ReadInt32();
            var size = reader.ReadInt32();
            var sizeAligned = reader.ReadInt32();
            var unknown1 = reader.ReadInt32();
            var unknown2 = reader.ReadInt32();
            var entryKey = reader.ReadBytes(16);
            var unknown3 = context.EncryptionKind == WzMsContainerEncryptionKind.ChaCha20
                ? reader.ReadInt32()
                : 0;
            var unknown4 = context.EncryptionKind == WzMsContainerEncryptionKind.ChaCha20
                ? reader.ReadInt32()
                : 0;

            entries.Add(new WzMsContainerEntryInspection(
                i,
                Path.GetFileName(entryName),
                entryName.Replace('\\', '/'),
                checkSum,
                flags,
                relativeBlock,
                Offset: relativeBlock,
                size,
                sizeAligned,
                unknown1,
                unknown2,
                unknown3,
                unknown4)
            {
                Key = entryKey
            });
        }

        var dataStartPosition = Align(stream.Position);
        return (entries.Select(entry => entry with
        {
            Offset = dataStartPosition + entry.RelativeBlock * Alignment
        }).ToArray(), dataStartPosition);
    }

    private static IMsEntryReader CreateEntryReader(Stream stream, MsContainerReadContext context)
    {
        if (context.EncryptionKind == WzMsContainerEncryptionKind.Snow)
        {
            Span<byte> entryKey = stackalloc byte[16];
            BuildSnowEntryTableKey(context.FileNameWithSalt, entryKey);
            return new MsSnowBlockReader(stream, entryKey);
        }

        Span<byte> chacha20EntryKey = stackalloc byte[WzMsChaCha20.KeyLength];
        BuildChaCha20EntryTableKey(context.FileNameWithSalt, chacha20EntryKey);
        return new MsChaCha20BlockReader(stream, chacha20EntryKey);
    }

    private static void BuildChaCha20HeaderKey(string fileNameWithSalt, Span<byte> key)
    {
        for (var i = 0; i < key.Length; i++)
        {
            key[i] = (byte)(fileNameWithSalt[i % fileNameWithSalt.Length] + i);
            key[i] ^= ChaCha20KeyObscure[i];
        }
    }

    private static void BuildChaCha20EntryTableKey(string fileNameWithSalt, Span<byte> key)
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

    private static void ValidateEntryCount(int entryCount)
    {
        if (entryCount < 0 || entryCount > MaxEntryCount)
        {
            throw new InvalidDataException($"MS entry count is outside the supported range: {entryCount}.");
        }
    }

    private static void ValidateEntryStart(Stream stream, long entryStartPosition)
    {
        if (entryStartPosition > stream.Length)
        {
            throw new InvalidDataException(
                $"MS entry table starts beyond the end of the stream: {entryStartPosition}.");
        }
    }

    private static void ValidateSnowHeaderHash(
        int headerHash,
        int hashedSaltLength,
        byte[] saltBytes,
        byte version,
        int entryCount)
    {
        var actualHash = hashedSaltLength + version + entryCount;
        for (var i = 0; i + 1 < saltBytes.Length; i += 2)
        {
            actualHash += BinaryPrimitives.ReadUInt16LittleEndian(saltBytes.AsSpan(i, 2));
        }

        if (headerHash != actualHash)
        {
            throw new InvalidDataException(
                $"MS Snow header hash check failed. Expected {headerHash}, actual {actualHash}.");
        }
    }

    private static long Align(long position)
    {
        return (position & (Alignment - 1)) == 0
            ? position
            : position - (position & (Alignment - 1)) + Alignment;
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

    private static int ReadInt32(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        ReadExactly(stream, buffer);
        return BinaryPrimitives.ReadInt32LittleEndian(buffer);
    }

    private static byte[] ReadBytes(Stream stream, int count)
    {
        var bytes = new byte[count];
        ReadExactly(stream, bytes);
        return bytes;
    }

    private static void ReadExactly(Stream stream, Span<byte> buffer)
    {
        stream.ReadExactly(buffer);
    }

    private interface IMsEntryReader : IDisposable
    {
        int ReadInt32();

        byte[] ReadBytes(int count);

        string ReadString(int maxLength);
    }

    private sealed class MsSnowBlockReader : IMsEntryReader
    {
        private readonly Stream stream;
        private readonly WzSnow2CryptoTransform transform;
        private readonly byte[] buffer = new byte[4];
        private readonly byte[] encryptedBuffer = new byte[4];
        private int position = 4;

        public MsSnowBlockReader(Stream stream, ReadOnlySpan<byte> key)
        {
            this.stream = stream;
            transform = new WzSnow2CryptoTransform(key, [], encrypting: false);
        }

        public int ReadInt32()
        {
            Span<byte> buffer = stackalloc byte[4];
            ReadBytes(buffer);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        public byte[] ReadBytes(int count)
        {
            var bytes = new byte[count];
            ReadBytes(bytes);
            return bytes;
        }

        public string ReadString(int maxLength)
        {
            var length = ReadInt32();
            if (length < 0 || length > maxLength)
            {
                throw new InvalidDataException($"MS entry name length is outside the supported range: {length}.");
            }

            var bytes = ReadBytes(checked(length * 2));
            return Encoding.Unicode.GetString(bytes);
        }

        private void ReadBytes(Span<byte> destination)
        {
            while (!destination.IsEmpty)
            {
                if (position >= buffer.Length)
                {
                    stream.ReadExactly(encryptedBuffer);
                    transform.TransformBlock(encryptedBuffer, 0, encryptedBuffer.Length, buffer, 0);
                    position = 0;
                }

                var count = Math.Min(destination.Length, buffer.Length - position);
                buffer.AsSpan(position, count).CopyTo(destination);
                destination = destination[count..];
                position += count;
            }
        }

        public void Dispose()
        {
            transform.Dispose();
        }
    }

    private sealed class MsChaCha20BlockReader : IMsEntryReader
    {
        private readonly Stream stream;
        private readonly byte[] key;
        private readonly byte[] buffer = new byte[WzMsChaCha20.BlockLength];
        private readonly byte[] encryptedBuffer = new byte[WzMsChaCha20.BlockLength];
        private int position = WzMsChaCha20.BlockLength;

        public MsChaCha20BlockReader(Stream stream, ReadOnlySpan<byte> key)
        {
            this.stream = stream;
            this.key = key.ToArray();
        }

        public int ReadInt32()
        {
            Span<byte> buffer = stackalloc byte[4];
            ReadBytes(buffer);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        public byte[] ReadBytes(int count)
        {
            var bytes = new byte[count];
            ReadBytes(bytes);
            return bytes;
        }

        public string ReadString(int maxLength)
        {
            var length = ReadInt32();
            if (length < 0 || length > maxLength)
            {
                throw new InvalidDataException($"MS entry name length is outside the supported range: {length}.");
            }

            var bytes = ReadBytes(checked(length * 2));
            return Encoding.Unicode.GetString(bytes);
        }

        private void ReadBytes(Span<byte> destination)
        {
            while (!destination.IsEmpty)
            {
                if (position >= buffer.Length)
                {
                    stream.ReadExactly(encryptedBuffer);
                    WzMsChaCha20.XorBlock(encryptedBuffer, buffer, key);
                    position = 0;
                }

                var count = Math.Min(destination.Length, buffer.Length - position);
                buffer.AsSpan(position, count).CopyTo(destination);
                destination = destination[count..];
                position += count;
            }
        }

        public void Dispose()
        {
        }
    }

    private sealed record MsContainerReadContext(
        WzMsContainerHeaderInspection Header,
        string FileNameWithSalt,
        WzMsContainerEncryptionKind EncryptionKind);
}
