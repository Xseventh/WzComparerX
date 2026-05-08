using System.Buffers.Binary;

namespace WzComparerX.WzLib;

public sealed class WzDirectoryInspectionReader
{
    private readonly WzPackageHeaderReader headerReader;
    private readonly WzStringDecryptor stringDecryptor;

    public WzDirectoryInspectionReader(
        WzPackageHeaderReader? headerReader = null,
        WzStringDecryptor? stringDecryptor = null)
    {
        this.headerReader = headerReader ?? new WzPackageHeaderReader();
        this.stringDecryptor = stringDecryptor ?? new WzStringDecryptor();
    }

    public async Task<WzDirectoryInspection> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = File.OpenRead(path);
        var header = headerReader.Read(stream, Path.GetFullPath(path));
        return Read(stream, header, cancellationToken);
    }

    public WzDirectoryInspection Read(Stream stream, WzPackageHeader header, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(header);

        if (!header.IsValid)
        {
            return new WzDirectoryInspection(header, EntryCount: 0, Array.Empty<WzDirectoryEntryInspection>());
        }

        if (header.Format == WzPackageFormat.Pkg2)
        {
            return ReadPkg2(stream, header, cancellationToken);
        }

        if (header.Format != WzPackageFormat.Pkg1)
        {
            throw new NotSupportedException("Directory inspection currently supports PKG1 WZ files only.");
        }

        if (!stream.CanSeek)
        {
            throw new ArgumentException("Directory inspection requires a seekable stream.", nameof(stream));
        }

        stream.Position = header.DirectoryStartPosition;
        var entries = new List<WzDirectoryEntryInspection>();
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

        return new WzDirectoryInspection(
            header,
            entryCount,
            entries,
            version?.WzVersion,
            version?.HashVersion,
            stringDecryptor.Kind);
    }

    private WzDirectoryInspection ReadPkg2(
        Stream stream,
        WzPackageHeader header,
        CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("Directory inspection requires a seekable stream.", nameof(stream));
        }

        var profile = DetectPkg2Profile(stream, header);
        stream.Position = header.DirectoryStartPosition;
        var entries = new List<WzDirectoryEntryInspection>();
        var entryCount = ReadPkg2DirectoryTree(
            stream,
            header,
            profile,
            entries,
            depth: 0,
            parentPath: string.Empty,
            cancellationToken);

        return new WzDirectoryInspection(
            header,
            entryCount,
            entries,
            profile.WzVersion,
            profile.HashVersion,
            stringDecryptor.Kind,
            profile.Name);
    }

    private WzPkg2DirectoryProfile DetectPkg2Profile(Stream stream, WzPackageHeader header)
    {
        var position = stream.Position;
        try
        {
            stream.Position = header.DirectoryStartPosition;
            ReadCompressedInt32(stream);
            var nodeType = ReadByte(stream);
            if (nodeType is not (0x03 or 0x04))
            {
                throw new InvalidDataException($"Unknown PKG2 directory node type 0x{nodeType:X2}.");
            }

            var rawBytes = ReadPkg2StringBytes(stream);
            if (!WzPkg2DirectoryProfile.TryDetect(header, rawBytes, out var profile))
            {
                throw new NotSupportedException("PKG2 directory inspection currently supports only KMST1199/1200 directory profiles.");
            }

            return profile;
        }
        finally
        {
            stream.Position = position;
        }
    }

    private int ReadDirectoryTree(
        Stream stream,
        WzPackageHeader header,
        List<WzDirectoryEntryInspection> entries,
        int depth,
        string parentPath,
        CancellationToken cancellationToken)
    {
        var entryCount = ReadCompressedInt32(stream);
        if (entryCount < 0)
        {
            throw new InvalidDataException($"PKG1 directory entry count cannot be negative: {entryCount}.");
        }

        var directoryEntries = new List<WzDirectoryEntryInspection>();

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

            var entry = new WzDirectoryEntryInspection(
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

    private int ReadPkg2DirectoryTree(
        Stream stream,
        WzPackageHeader header,
        WzPkg2DirectoryProfile profile,
        List<WzDirectoryEntryInspection> entries,
        int depth,
        string parentPath,
        CancellationToken cancellationToken)
    {
        var encryptedEntryCount = ReadCompressedInt32(stream);
        var entryCount = profile.DecryptEntryCount(encryptedEntryCount);
        if (entryCount < 0)
        {
            throw new InvalidDataException($"PKG2 directory entry count cannot be negative: {entryCount}.");
        }

        var directoryEntries = new List<WzDirectoryEntryInspection>();
        var entryHeaders = new List<Pkg2DirectoryEntryHeader>(entryCount);
        for (var i = 0; i < entryCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var nodeType = ReadByte(stream);
            if (nodeType is not (0x03 or 0x04))
            {
                throw new InvalidDataException($"Unknown PKG2 directory node type 0x{nodeType:X2}.");
            }

            var name = i == 0
                ? WzPkg2DirectoryProfile.DecodePkg2String(ReadPkg2StringBytes(stream), profile.Pkg2StringKey)
                : ReadString(stream);
            var dataSize = ReadCompressedInt32(stream);
            var checksum = ReadCompressedInt32(stream);
            entryHeaders.Add(new Pkg2DirectoryEntryHeader(nodeType, name, dataSize, checksum));
        }

        var encryptedOffsetCount = ReadCompressedInt32(stream);
        if (encryptedOffsetCount != encryptedEntryCount)
        {
            throw new InvalidDataException("PKG2 directory offset count does not match entry count.");
        }

        foreach (var entryHeader in entryHeaders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var hashOffsetPosition = stream.Position;
            var hashOffset = ReadUInt32LittleEndian(stream);
            var path = CombinePath(parentPath, entryHeader.Name);
            var entry = new WzDirectoryEntryInspection(
                entries.Count,
                entryHeader.NodeType,
                ToEntryKind(entryHeader.NodeType),
                entryHeader.Name,
                entryHeader.DataSize,
                entryHeader.Checksum,
                hashOffsetPosition,
                hashOffset,
                profile.CalculateOffset(checked((uint)hashOffsetPosition), hashOffset, checked((uint)header.HeaderSize)),
                depth,
                path);
            entries.Add(entry);

            if (entry.Kind == WzDirectoryEntryKind.Directory)
            {
                directoryEntries.Add(entry);
            }
        }

        foreach (var entry in directoryEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadPkg2DirectoryTree(
                stream,
                header,
                profile,
                entries,
                depth + 1,
                entry.Path ?? string.Empty,
                cancellationToken);
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

        if (stream.CanSeek && offset >= stream.Length)
        {
            throw new InvalidDataException($"Cannot read a string beyond the end of the stream: {offset}.");
        }

        var position = stream.Position;
        try
        {
            stream.Position = offset;
            return ReadString(stream);
        }
        finally
        {
            stream.Position = position;
        }
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

    private static byte[] ReadPkg2StringBytes(Stream stream)
    {
        var size = ReadSByte(stream);
        if (size >= 0)
        {
            throw new InvalidDataException($"Unexpected PKG2 directory string length: {size}.");
        }

        return ReadBytes(stream, -size * sizeof(char));
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

    private sealed record Pkg2DirectoryEntryHeader(
        byte NodeType,
        string Name,
        int DataSize,
        int Checksum);
}
