using System.Buffers.Binary;

namespace WzComparerX.WzLib;

public sealed class WzDirectoryPreviewReader
{
    private readonly WzPackageHeaderReader headerReader;
    private readonly WzStringDecryptor stringDecryptor;

    public WzDirectoryPreviewReader(
        WzPackageHeaderReader? headerReader = null,
        WzStringDecryptor? stringDecryptor = null)
    {
        this.headerReader = headerReader ?? new WzPackageHeaderReader();
        this.stringDecryptor = stringDecryptor ?? new WzStringDecryptor();
    }

    public async Task<WzDirectoryPreview> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = File.OpenRead(path);
        var header = headerReader.Read(stream, Path.GetFullPath(path));
        return Read(stream, header, cancellationToken);
    }

    public WzDirectoryPreview Read(Stream stream, WzPackageHeader header, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(header);

        if (!header.IsValid)
        {
            return new WzDirectoryPreview(header, EntryCount: 0, Array.Empty<WzDirectoryEntryPreview>());
        }

        if (header.Format != WzPackageFormat.Pkg1)
        {
            throw new NotSupportedException("Directory preview currently supports PKG1 WZ files only.");
        }

        if (!stream.CanSeek)
        {
            throw new ArgumentException("Directory preview requires a seekable stream.", nameof(stream));
        }

        stream.Position = header.DirectoryStartPosition;
        var entries = new List<WzDirectoryEntryPreview>();
        var entryCount = ReadDirectoryTree(
            stream,
            header,
            entries,
            depth: 0,
            parentPath: string.Empty,
            cancellationToken);

        var directoryEndPosition = stream.Position;
        var version = WzPkg1VersionDetector.Detect(header, entries, directoryEndPosition);
        if (version is not null)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                var offset = WzPkg1VersionDetector.CalculateOffset(header, entries[i], version.HashVersion);
                entries[i] = entries[i] with { Offset = offset };
            }
        }

        return new WzDirectoryPreview(
            header,
            entryCount,
            entries,
            version?.WzVersion,
            version?.HashVersion);
    }

    private int ReadDirectoryTree(
        Stream stream,
        WzPackageHeader header,
        List<WzDirectoryEntryPreview> entries,
        int depth,
        string parentPath,
        CancellationToken cancellationToken)
    {
        var entryCount = ReadCompressedInt32(stream);
        var directoryEntries = new List<WzDirectoryEntryPreview>();

        for (var i = 0; i < entryCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var nodeType = ReadByte(stream);
            string? name = null;
            switch (nodeType)
            {
                case 0x02:
                    name = ReadStringAt(
                        stream,
                        header.DirectoryStartPosition + ReadInt32LittleEndian(stream) + GetStringReferenceOffset(header));
                    break;
                case 0x03:
                case 0x04:
                    name = ReadString(stream);
                    break;
                default:
                    throw new InvalidDataException($"Unknown PKG1 directory node type 0x{nodeType:X2}.");
            }

            var dataSize = ReadCompressedInt32(stream);
            var checksum = ReadCompressedInt32(stream);
            var hashOffsetPosition = stream.Position;
            var hashOffset = ReadUInt32LittleEndian(stream);
            var path = CombinePath(parentPath, name);

            var entry = new WzDirectoryEntryPreview(
                entries.Count,
                nodeType,
                ToEntryKind(nodeType),
                name,
                dataSize,
                checksum,
                hashOffsetPosition,
                hashOffset,
                Depth: depth,
                Path: path);
            entries.Add(entry);

            if (entry.Kind == WzDirectoryEntryKind.Directory)
            {
                directoryEntries.Add(entry);
            }
        }

        foreach (var entry in directoryEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadDirectoryTree(stream, header, entries, depth + 1, entry.Path ?? string.Empty, cancellationToken);
        }

        return entryCount;
    }

    private static string? CombinePath(string parentPath, string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.IsNullOrEmpty(parentPath) ? null : parentPath;
        }

        return string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";
    }

    private static WzDirectoryEntryKind ToEntryKind(byte nodeType)
    {
        return nodeType switch
        {
            0x02 or 0x04 => WzDirectoryEntryKind.Image,
            0x03 => WzDirectoryEntryKind.Directory,
            _ => WzDirectoryEntryKind.Unknown
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

    private static int GetStringReferenceOffset(WzPackageHeader header)
    {
        return header.IsEncryptedVersionMissing ? 2 : -1;
    }

    private static int ReadCompressedInt32(Stream stream)
    {
        var value = ReadSByte(stream);
        return value == sbyte.MinValue ? ReadInt32LittleEndian(stream) : value;
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

    private static uint ReadUInt32LittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
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

        if (stream.CanSeek)
        {
            stream.Seek(count, SeekOrigin.Current);
            return;
        }

        Span<byte> buffer = stackalloc byte[Math.Min(count, 1024)];
        var remaining = count;
        while (remaining > 0)
        {
            var read = stream.Read(buffer[..Math.Min(buffer.Length, remaining)]);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            remaining -= read;
        }
    }
}
