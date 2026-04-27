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

    [Fact]
    public void Read_ExpandsNestedPropertyWhenDepthAllows()
    {
        byte[] bytes =
        [
            0x00, 0x00, 0x00, 0x00,
            0x73, 0xf8, 0xfa, 0xd9, 0xc3, 0xdd, 0xcb, 0xdd, 0xc4, 0xc8,
            0x00, 0x00, 0x01,
            0x00, 0xfb, 0xc9, 0xc3, 0xc5, 0xc1, 0xca, 0x09,
            0x14, 0x00, 0x00, 0x00,
            0x73, 0xf8, 0xfa, 0xd9, 0xc3, 0xdd, 0xcb, 0xdd, 0xc4, 0xc8,
            0x00, 0x00, 0x01,
            0x00, 0xfd, 0xcc, 0xc4, 0xc3, 0x03, 0x2a
        ];
        using var stream = new MemoryStream(bytes);
        var reader = new WzImagePreviewReader(new WzStringDecryptor(WzStringEncryptionKind.None), maxPropertyDepth: 2);

        var preview = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(preview.Properties);
        var property = Assert.Single(preview.Properties);
        Assert.Equal("child", property.Name);
        Assert.Equal("object", property.Kind);
        Assert.Equal("Property", property.Value);
        Assert.Equal(1, property.ChildCount);
        Assert.NotNull(property.Children);
        var child = Assert.Single(property.Children);
        Assert.Equal("foo", child.Name);
        Assert.Equal("child/foo", child.Path);
        Assert.Equal(1, child.Depth);
        Assert.Equal(42, child.Value);
    }

    [Fact]
    public void Constructor_RejectsDepthAboveLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new WzImagePreviewReader(maxPropertyDepth: WzImagePreviewReader.MaxPropertyPreviewDepth + 1));
    }

    [Fact]
    public void Read_ReturnsVectorObjectValue()
    {
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "origin",
            CreateObjectValue("Shape2D#Vector2D", 12, 34)));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImagePreviewReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var preview = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(preview.Properties);
        var property = Assert.Single(preview.Properties);
        Assert.Equal("vector", property.Kind);
        var vector = Assert.IsType<WzImageVectorPreview>(property.Value);
        Assert.Equal(12, vector.X);
        Assert.Equal(34, vector.Y);
    }

    [Fact]
    public void Read_ReturnsUolObjectValue()
    {
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "link",
            CreateObjectValue("UOL", 0x00, CreateImageString("../foo"))));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImagePreviewReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var preview = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(preview.Properties);
        var property = Assert.Single(preview.Properties);
        Assert.Equal("uol", property.Kind);
        Assert.Equal("../foo", property.Value);
    }

    [Fact]
    public void Read_ReturnsCanvasMetadata()
    {
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "icon",
            CreateObjectValue(
                "Canvas",
                0x00,
                0x00,
                16,
                8,
                2,
                0x00,
                1,
                0,
                (byte)0x00,
                (byte)0x00,
                BitConverter.GetBytes(3),
                0x01,
                0x02,
                0x03)));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImagePreviewReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var preview = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(preview.Properties);
        var property = Assert.Single(preview.Properties);
        Assert.Equal("canvas", property.Kind);
        var canvas = Assert.IsType<WzImageCanvasPreview>(property.Value);
        Assert.Equal(16, canvas.Width);
        Assert.Equal(8, canvas.Height);
        Assert.Equal(2, canvas.Format);
        Assert.Equal(1, canvas.Pages);
        Assert.Equal(3, canvas.DataLength);
    }

    [Fact]
    public void Read_ReturnsConvexObjectValue()
    {
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "polygon",
            CreateObjectValue(
                "Shape2D#Convex2D",
                2,
                CreateObjectValue("Shape2D#Vector2D", 1, 2),
                CreateObjectValue("Shape2D#Vector2D", 3, 4))));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImagePreviewReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var preview = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(preview.Properties);
        var property = Assert.Single(preview.Properties);
        Assert.Equal("convex", property.Kind);
        var convex = Assert.IsType<WzImageConvexPreview>(property.Value);
        Assert.Equal(2, convex.Points.Count);
        Assert.Equal(new WzImageVectorPreview(1, 2), convex.Points[0]);
        Assert.Equal(new WzImageVectorPreview(3, 4), convex.Points[1]);
    }

    [Fact]
    public void Read_RejectsConvexWithNonVectorPoint()
    {
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "polygon",
            CreateObjectValue(
                "Shape2D#Convex2D",
                1,
                CreateObjectValue("Property", 0x00, 0x00, 0))));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImagePreviewReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        Assert.Throws<InvalidDataException>(
            () => reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0"));
    }

    [Fact]
    public void Read_ReturnsRawDataMetadata()
    {
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "payload",
            CreateObjectValue(
                "RawData",
                1,
                0x01,
                0x00,
                0x00,
                1,
                CreateImageString("kind"),
                0x03,
                42,
                3,
                0x01,
                0x02,
                0x03)));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImagePreviewReader(
            new WzStringDecryptor(WzStringEncryptionKind.None),
            maxPropertyDepth: 2);

        var preview = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(preview.Properties);
        var property = Assert.Single(preview.Properties);
        Assert.Equal("rawData", property.Kind);
        Assert.Equal(1, property.ChildCount);
        Assert.NotNull(property.Children);
        Assert.Equal("kind", Assert.Single(property.Children).Name);
        var rawData = Assert.IsType<WzImageRawDataPreview>(property.Value);
        Assert.Equal(1, rawData.Version);
        Assert.Equal(3, rawData.DataLength);
    }

    [Fact]
    public void Read_RejectsRawDataExtendingPastImage()
    {
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "payload",
            CreateObjectValue("RawData", 0, 100)));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImagePreviewReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        Assert.Throws<InvalidDataException>(
            () => reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0"));
    }

    [Fact]
    public void Read_ReturnsVideoMetadata()
    {
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "clip",
            CreateObjectValue(
                "Canvas#Video",
                0x00,
                0x01,
                0x00,
                0x00,
                1,
                CreateImageString("kind"),
                0x03,
                7,
                5,
                4,
                0x01,
                0x02,
                0x03,
                0x04)));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImagePreviewReader(
            new WzStringDecryptor(WzStringEncryptionKind.None),
            maxPropertyDepth: 2);

        var preview = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(preview.Properties);
        var property = Assert.Single(preview.Properties);
        Assert.Equal("video", property.Kind);
        Assert.Equal(1, property.ChildCount);
        Assert.NotNull(property.Children);
        Assert.Equal("kind", Assert.Single(property.Children).Name);
        var video = Assert.IsType<WzImageVideoPreview>(property.Value);
        Assert.Equal(5, video.Unknown);
        Assert.Equal(4, video.DataLength);
    }

    [Fact]
    public void Read_RejectsVideoExtendingPastImage()
    {
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "clip",
            CreateObjectValue("Canvas#Video", 0x00, 0x00, 5, 100)));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImagePreviewReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        Assert.Throws<InvalidDataException>(
            () => reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0"));
    }

    [Fact]
    public void Read_ReturnsSoundMetadata()
    {
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "sound",
            CreateObjectValue(
                "Sound_DX8",
                1,
                0x01,
                0x00,
                0x00,
                1,
                CreateImageString("kind"),
                0x03,
                7,
                3,
                60,
                2,
                CreateBytes(0x00, 16),
                CreateBytes(0x01, 16),
                0x01,
                0x00,
                CreateBytes(0x02, 16),
                4,
                CreateBytes(0x03, 4),
                0x10,
                0x11,
                0x12)));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImagePreviewReader(
            new WzStringDecryptor(WzStringEncryptionKind.None),
            maxPropertyDepth: 2);

        var preview = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(preview.Properties);
        var property = Assert.Single(preview.Properties);
        Assert.Equal("sound", property.Kind);
        Assert.Equal(1, property.ChildCount);
        Assert.NotNull(property.Children);
        Assert.Equal("kind", Assert.Single(property.Children).Name);
        var sound = Assert.IsType<WzImageSoundPreview>(property.Value);
        Assert.Equal(1, sound.Version);
        Assert.Equal(60, sound.Duration);
        Assert.Equal(2, sound.SoundDeclaration);
        Assert.True(sound.FixedSizeSamples);
        Assert.False(sound.TemporalCompression);
        Assert.Equal(4, sound.FormatExtraLength);
        Assert.Equal(3, sound.DataLength);
    }

    [Fact]
    public void Read_RejectsSoundExtendingPastImage()
    {
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "sound",
            CreateObjectValue(
                "Sound_DX8",
                0,
                100,
                1,
                0,
                CreateBytes(0x00, 16),
                CreateBytes(0x00, 16),
                0x00,
                0x00,
                CreateBytes(0x00, 16))));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImagePreviewReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        Assert.Throws<InvalidDataException>(
            () => reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0"));
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

    private static WzDirectoryEntryPreview CreateImageEntry(long offset, int dataSize = 10)
    {
        return new WzDirectoryEntryPreview(
            Index: 0,
            NodeType: 0x04,
            WzDirectoryEntryKind.Image,
            Name: "Synthetic.img",
            DataSize: dataSize,
            Checksum: 0,
            HashOffsetPosition: 0,
            HashOffset: 0,
            Offset: offset);
    }

    private static byte[] CreatePropertyImage(params byte[][] entries)
    {
        var bytes = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
        AddImageObjectName(bytes, "Property");
        bytes.Add(0x00);
        bytes.Add(0x00);
        bytes.Add((byte)entries.Length);
        foreach (var entry in entries)
        {
            bytes.AddRange(entry);
        }

        return bytes.ToArray();
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

    private static byte[] CreateObjectValue(string objectType, params object[] payloadParts)
    {
        var bytes = new List<byte>();
        AddImageObjectName(bytes, objectType);
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

        return bytes.ToArray();
    }

    private static byte[] CreateImageString(string value)
    {
        var bytes = new List<byte> { 0x00 };
        AddWzString(bytes, value);
        return bytes.ToArray();
    }

    private static byte[] CreateBytes(byte value, int count)
    {
        return Enumerable.Repeat(value, count).ToArray();
    }

    private static void AddImageObjectName(List<byte> bytes, string value)
    {
        bytes.Add(0x73);
        AddWzString(bytes, value);
    }

    private static void AddWzString(List<byte> bytes, string value)
    {
        bytes.Add(unchecked((byte)(sbyte)-value.Length));
        for (var i = 0; i < value.Length; i++)
        {
            bytes.Add((byte)(value[i] ^ (byte)(0xAA + i)));
        }
    }
}
