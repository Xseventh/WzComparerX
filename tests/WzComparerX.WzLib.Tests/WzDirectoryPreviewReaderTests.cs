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
            new WzStringDecryptor(WzStringEncryptionKind.Bms));
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

    private static byte[] CreatePkg1WithDirectoryEntries()
    {
        byte[] directoryData =
        [
            0x02,
            0x03, 0xfd, 0xcb, 0xc9, 0xcf, 0x05, 0x01, 0x78, 0x56, 0x34, 0x12,
            0x04, 0xfd, 0xce, 0xce, 0xca, 0x07, 0x02, 0xef, 0xcd, 0xab, 0x90
        ];
        byte[] encryptedVersion = [0x7b, 0x00];
        var header = CreateHeader("PKG1", "Copyright", dataSize: encryptedVersion.Length + directoryData.Length);
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
