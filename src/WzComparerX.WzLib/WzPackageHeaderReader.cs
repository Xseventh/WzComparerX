using System.Buffers.Binary;
using System.Text;

namespace WzComparerX.WzLib;

public sealed class WzPackageHeaderReader
{
    public const string Pkg1Signature = "PKG1";
    public const string Pkg2Signature = "PKG2";

    public async Task<WzPackageHeader> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = File.OpenRead(path);
        return Read(stream, Path.GetFullPath(path));
    }

    public WzPackageHeader Read(Stream stream, string sourcePath)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        if (!stream.CanRead)
        {
            throw new ArgumentException("The WZ header stream must be readable.", nameof(stream));
        }

        var fileSize = stream.CanSeek ? stream.Length : 0;
        if (fileSize < 4 || !TryReadExact(stream, 4, out var signatureBytes))
        {
            return Invalid(sourcePath, fileSize);
        }

        var signature = Encoding.ASCII.GetString(signatureBytes);
        if (signature is not (Pkg1Signature or Pkg2Signature))
        {
            return Invalid(sourcePath, fileSize, signature);
        }

        var dataSize = ReadInt64LittleEndian(stream);
        var headerSize = ReadInt32LittleEndian(stream);
        if (headerSize < stream.Position || headerSize > fileSize)
        {
            return Invalid(sourcePath, fileSize, signature);
        }

        var copyrightLength = headerSize - (int)stream.Position;
        var copyright = copyrightLength == 0
            ? string.Empty
            : Encoding.ASCII.GetString(ReadExact(stream, copyrightLength));

        return signature switch
        {
            Pkg1Signature => ReadPkg1(stream, sourcePath, signature, copyright, headerSize, dataSize, fileSize),
            Pkg2Signature => ReadPkg2(stream, sourcePath, signature, copyright, headerSize, dataSize, fileSize),
            _ => Invalid(sourcePath, fileSize, signature)
        };
    }

    private static WzPackageHeader ReadPkg1(
        Stream stream,
        string sourcePath,
        string signature,
        string copyright,
        int headerSize,
        long dataSize,
        long fileSize)
    {
        var encryptedVersionMissing = false;
        int encryptedVersion = -1;

        if (dataSize >= 2 && stream.CanSeek)
        {
            stream.Position = headerSize;
            if (TryReadExact(stream, 2, out var versionBytes))
            {
                encryptedVersion = BinaryPrimitives.ReadUInt16LittleEndian(versionBytes);
                encryptedVersionMissing = encryptedVersion > 0xff;

                if (encryptedVersion == 0x80 && dataSize >= 5 && stream.CanSeek)
                {
                    stream.Position = headerSize;
                    var propertyCount = ReadCompressedInt32(stream);
                    encryptedVersionMissing = propertyCount > 0
                        && (propertyCount & 0xff) == 0
                        && propertyCount <= 0xffff;
                }
            }
        }
        else
        {
            encryptedVersionMissing = true;
        }

        var directoryStart = headerSize + (encryptedVersionMissing ? 0 : 2);
        return new WzPackageHeader(
            WzPackageFormat.Pkg1,
            signature,
            sourcePath,
            copyright,
            headerSize,
            dataSize,
            fileSize,
            directoryStart,
            encryptedVersion,
            encryptedVersionMissing);
    }

    private static WzPackageHeader ReadPkg2(
        Stream stream,
        string sourcePath,
        string signature,
        string copyright,
        int headerSize,
        long dataSize,
        long fileSize)
    {
        var hash1 = ReadUInt32LittleEndian(stream);
        var hash2 = ReadUInt32LittleEndian(stream);

        return new WzPackageHeader(
            WzPackageFormat.Pkg2,
            signature,
            sourcePath,
            copyright,
            headerSize,
            dataSize,
            fileSize,
            stream.Position,
            Hash1: hash1,
            Hash2: hash2);
    }

    private static WzPackageHeader Invalid(string sourcePath, long fileSize, string signature = "")
    {
        return new WzPackageHeader(
            WzPackageFormat.Unknown,
            signature,
            sourcePath,
            string.Empty,
            HeaderSize: 0,
            DataSize: 0,
            fileSize,
            DirectoryStartPosition: 0);
    }

    private static int ReadCompressedInt32(Stream stream)
    {
        var first = stream.ReadByte();
        if (first < 0)
        {
            throw new EndOfStreamException();
        }

        if ((sbyte)first == sbyte.MinValue)
        {
            return ReadInt32LittleEndian(stream);
        }

        return (sbyte)first;
    }

    private static int ReadInt32LittleEndian(Stream stream)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(ReadExact(stream, sizeof(int)));
    }

    private static long ReadInt64LittleEndian(Stream stream)
    {
        return BinaryPrimitives.ReadInt64LittleEndian(ReadExact(stream, sizeof(long)));
    }

    private static uint ReadUInt32LittleEndian(Stream stream)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(ReadExact(stream, sizeof(uint)));
    }

    private static byte[] ReadExact(Stream stream, int length)
    {
        if (!TryReadExact(stream, length, out var bytes))
        {
            throw new EndOfStreamException();
        }

        return bytes;
    }

    private static bool TryReadExact(Stream stream, int length, out byte[] bytes)
    {
        bytes = new byte[length];
        var offset = 0;
        while (offset < length)
        {
            var read = stream.Read(bytes, offset, length - offset);
            if (read == 0)
            {
                bytes = [];
                return false;
            }

            offset += read;
        }

        return true;
    }
}
