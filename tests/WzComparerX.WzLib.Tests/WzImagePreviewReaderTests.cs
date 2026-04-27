using WzComparerX.WzLib;

namespace WzComparerX.WzLib.Tests;

public class WzImagePreviewReaderTests
{
    [Fact]
    public void Read_ReturnsInlineImageObjectType()
    {
        byte[] bytes =
        [
            0x00, 0x00, 0x00, 0x00,
            0x73, 0xf8, 0xfa, 0xd9, 0xc3, 0xdd, 0xcb, 0xdd, 0xc4, 0xc8,
            0x00, 0x00, 0x00
        ];
        using var stream = new MemoryStream(bytes);
        var reader = new WzImagePreviewReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var preview = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4), "0");

        Assert.True(preview.IsValid);
        Assert.Equal("Property", preview.ObjectType);
        Assert.Equal(0, preview.PropertyCount);
        Assert.Equal(4, preview.Entry?.Offset);
    }

    [Fact]
    public void Read_ReturnsReferencedImageObjectType()
    {
        byte[] bytes =
        [
            0x00, 0x00, 0x00, 0x00,
            0x1b, 0x08, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00,
            0xf8, 0xfa, 0xd9, 0xc3, 0xdd, 0xcb, 0xdd, 0xc4, 0xc8
        ];
        using var stream = new MemoryStream(bytes);
        var reader = new WzImagePreviewReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var preview = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4), "0");

        Assert.True(preview.IsValid);
        Assert.Equal("Property", preview.ObjectType);
        Assert.Equal(0, preview.PropertyCount);
    }

    [Fact]
    public void Read_ReturnsTopLevelPropertyScalars()
    {
        byte[] bytes =
        [
            0x00, 0x00, 0x00, 0x00,
            0x73, 0xf8, 0xfa, 0xd9, 0xc3, 0xdd, 0xcb, 0xdd, 0xc4, 0xc8,
            0x00, 0x00, 0x02,
            0x00, 0xfd, 0xcc, 0xc4, 0xc3, 0x03, 0x2a,
            0x00, 0xfc, 0xc4, 0xca, 0xc1, 0xc8, 0x08, 0x00, 0xfd, 0xc8, 0xca, 0xde
        ];
        using var stream = new MemoryStream(bytes);
        var reader = new WzImagePreviewReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var preview = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4), "0");

        Assert.Equal(2, preview.PropertyCount);
        Assert.NotNull(preview.Properties);
        Assert.Equal("foo", preview.Properties[0].Name);
        Assert.Equal("int32", preview.Properties[0].Kind);
        Assert.Equal(42, preview.Properties[0].Value);
        Assert.Equal("name", preview.Properties[1].Name);
        Assert.Equal("string", preview.Properties[1].Kind);
        Assert.Equal("bar", preview.Properties[1].Value);
    }

    private static WzPackageHeader CreateHeader()
    {
        return new WzPackageHeader(
            WzPackageFormat.Pkg1,
            "PKG1",
            "Synthetic.wz",
            string.Empty,
            HeaderSize: 0,
            DataSize: 0,
            FileSize: 18,
            DirectoryStartPosition: 0);
    }

    private static WzDirectoryEntryPreview CreateImageEntry(long offset)
    {
        return new WzDirectoryEntryPreview(
            Index: 0,
            NodeType: 0x04,
            WzDirectoryEntryKind.Image,
            Name: "Synthetic.img",
            DataSize: 10,
            Checksum: 0,
            HashOffsetPosition: 0,
            HashOffset: 0,
            Offset: offset);
    }
}
