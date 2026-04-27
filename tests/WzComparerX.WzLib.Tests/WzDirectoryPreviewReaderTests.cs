using System.Buffers.Binary;
using System.Text;
using WzComparerX.WzLib;

namespace WzComparerX.WzLib.Tests;

public class WzDirectoryPreviewReaderTests
{
    [Fact]
    public void Read_ReturnsPkg1DirectoryEntryPreviews()
    {
        var bytes = CreatePkg1WithDirectoryEntries();
        var headerReader = new WzPackageHeaderReader();
        var previewReader = new WzDirectoryPreviewReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "Base.wz");

        var preview = previewReader.Read(stream, header);

        Assert.Equal(2, preview.EntryCount);
        Assert.Equal(WzDirectoryEntryKind.Directory, preview.Entries[0].Kind);
        Assert.Equal("abc", preview.Entries[0].Name);
        Assert.Equal(0x03, preview.Entries[0].NodeType);
        Assert.Equal(5, preview.Entries[0].DataSize);
        Assert.Equal(1, preview.Entries[0].Checksum);
        Assert.Equal(0x12345678u, preview.Entries[0].HashOffset);
        Assert.Equal(WzDirectoryEntryKind.Image, preview.Entries[1].Kind);
        Assert.Equal("def", preview.Entries[1].Name);
        Assert.Equal(0x04, preview.Entries[1].NodeType);
        Assert.Equal(7, preview.Entries[1].DataSize);
        Assert.Equal(2, preview.Entries[1].Checksum);
        Assert.Equal(0x90abcdefu, preview.Entries[1].HashOffset);
    }

    [Fact]
    public void Read_MissingEncryptedVersionComputesPkg1Offsets()
    {
        var bytes = CreatePkg1WithMissingEncryptedVersion();
        var headerReader = new WzPackageHeaderReader();
        var previewReader = new WzDirectoryPreviewReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "Base.wz");

        var preview = previewReader.Read(stream, header);

        Assert.True(header.IsEncryptedVersionMissing);
        Assert.Equal(777, preview.WzVersion);
        Assert.Equal(59192u, preview.HashVersion);
        Assert.Equal(24, preview.Entries[0].HashOffsetPosition);
        Assert.Equal(0x22c12300u, preview.Entries[0].HashOffset);
        Assert.Equal(28, preview.Entries[0].Offset);
    }

    [Fact]
    public void Read_EncryptedVersionDetectsPkg1VersionAndDirectoryStubOffset()
    {
        var bytes = CreatePkg1WithEncryptedVersionAndDirectoryStubOffset();
        var headerReader = new WzPackageHeaderReader();
        var previewReader = new WzDirectoryPreviewReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "Base.wz");

        var preview = previewReader.Read(stream, header);

        Assert.False(header.IsEncryptedVersionMissing);
        Assert.Equal(0x20, header.EncryptedVersion);
        Assert.Equal(777, preview.WzVersion);
        Assert.Equal(59192u, preview.HashVersion);
        Assert.Equal(26, preview.Entries[0].HashOffsetPosition);
        Assert.Equal(0x3176a2c0u, preview.Entries[0].HashOffset);
        Assert.Equal(30, preview.Entries[0].Offset);
    }

    [Fact]
    public void Read_NodeType02ReadsReferencedStringName()
    {
        var bytes = CreatePkg1WithReferencedStringName();
        var headerReader = new WzPackageHeaderReader();
        var previewReader = new WzDirectoryPreviewReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "String.wz");

        var preview = previewReader.Read(stream, header);

        Assert.Equal(1, preview.EntryCount);
        Assert.Equal(WzDirectoryEntryKind.Image, preview.Entries[0].Kind);
        Assert.Equal(0x02, preview.Entries[0].NodeType);
        Assert.Equal("ref", preview.Entries[0].Name);
        Assert.Equal(26, preview.Entries[0].HashOffsetPosition);
    }

    [Fact]
    public void Read_ReturnsRecursiveDirectoryEntries()
    {
        var bytes = CreatePkg1WithNestedDirectoryEntries();
        var headerReader = new WzPackageHeaderReader();
        var previewReader = new WzDirectoryPreviewReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "Nested.wz");

        var preview = previewReader.Read(stream, header);

        Assert.Equal(1, preview.EntryCount);
        Assert.Equal(2, preview.Entries.Count);
        Assert.Equal("dir", preview.Entries[0].Name);
        Assert.Equal("dir", preview.Entries[0].Path);
        Assert.Equal(0, preview.Entries[0].Depth);
        Assert.Equal(WzDirectoryEntryKind.Directory, preview.Entries[0].Kind);
        Assert.Equal("leaf.img", preview.Entries[1].Name);
        Assert.Equal("dir/leaf.img", preview.Entries[1].Path);
        Assert.Equal(1, preview.Entries[1].Depth);
        Assert.Equal(WzDirectoryEntryKind.Image, preview.Entries[1].Kind);
    }

    private static byte[] CreatePkg1WithDirectoryEntries()
    {
        byte[] directoryData =
        [
            0x02,
            0x03, 0xfd, 0xcb, 0xc9, 0xcf, 0x05, 0x01, 0x78, 0x56, 0x34, 0x12,
            0x04, 0xfd, 0xce, 0xce, 0xca, 0x07, 0x02, 0xef, 0xcd, 0xab, 0x90,
            0x00
        ];
        byte[] encryptedVersion = [0x7b, 0x00];
        var header = CreateHeader("PKG1", "Copyright", dataSize: encryptedVersion.Length + directoryData.Length);
        return [.. header, .. encryptedVersion, .. directoryData];
    }

    private static byte[] CreatePkg1WithMissingEncryptedVersion()
    {
        byte[] directoryData =
        [
            0x01,
            0x03, 0xfd, 0xcb, 0xc9, 0xcf, 0x05, 0x01, 0x00, 0x23, 0xc1, 0x22,
            0x00
        ];
        var header = CreateHeader("PKG1", string.Empty, dataSize: directoryData.Length);
        return [.. header, .. directoryData];
    }

    private static byte[] CreatePkg1WithEncryptedVersionAndDirectoryStubOffset()
    {
        byte[] encryptedVersion = [0x20, 0x00];
        byte[] directoryData =
        [
            0x01,
            0x03, 0xfd, 0xcb, 0xc9, 0xcf, 0x05, 0x01, 0xc0, 0xa2, 0x76, 0x31,
            0x00
        ];
        var header = CreateHeader("PKG1", string.Empty, dataSize: encryptedVersion.Length + directoryData.Length);
        return [.. header, .. encryptedVersion, .. directoryData];
    }

    private static byte[] CreatePkg1WithReferencedStringName()
    {
        byte[] encryptedVersion = [0x7b, 0x00];
        byte[] directoryData =
        [
            0x01,
            0x02, 0x0d, 0x00, 0x00, 0x00, 0x07, 0x02, 0xef, 0xcd, 0xab, 0x90,
            0xfd, 0xd8, 0xce, 0xca
        ];
        var header = CreateHeader("PKG1", string.Empty, dataSize: encryptedVersion.Length + directoryData.Length);
        return [.. header, .. encryptedVersion, .. directoryData];
    }

    private static byte[] CreatePkg1WithNestedDirectoryEntries()
    {
        byte[] encryptedVersion = [0x7b, 0x00];
        byte[] directoryData =
        [
            0x01,
            0x03, 0xfd, 0xce, 0xc2, 0xde, 0x00, 0x00, 0x44, 0x33, 0x22, 0x11,
            0x01,
            0x04, 0xf8, 0xc6, 0xce, 0xcd, 0xcb, 0x80, 0xc6, 0xdd, 0xd6,
            0x03, 0x04, 0x88, 0x77, 0x66, 0x55
        ];
        var header = CreateHeader("PKG1", string.Empty, dataSize: encryptedVersion.Length + directoryData.Length);
        return [.. header, .. encryptedVersion, .. directoryData];
    }

    private static byte[] CreateHeader(string signature, string copyright, long dataSize)
    {
        var copyrightBytes = Encoding.ASCII.GetBytes(copyright);
        var headerSize = 4 + sizeof(long) + sizeof(int) + copyrightBytes.Length;
        var bytes = new byte[headerSize];

        Encoding.ASCII.GetBytes(signature, bytes);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(4, sizeof(long)), dataSize);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12, sizeof(int)), headerSize);
        copyrightBytes.CopyTo(bytes.AsSpan(16));
        return bytes;
    }
}
