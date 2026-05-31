using WzComparerX.WzLib;

namespace WzComparerX.WzLib.Tests;

public class WzImageInspectionReaderTests
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
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4), "0");

        Assert.True(inspection.IsValid);
        Assert.Equal("Property", inspection.ObjectType);
        Assert.Equal(0, inspection.PropertyCount);
        Assert.Equal(4, inspection.Entry?.Offset);
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
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4), "0");

        Assert.True(inspection.IsValid);
        Assert.Equal("Property", inspection.ObjectType);
        Assert.Equal(0, inspection.PropertyCount);
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
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4), "0");

        Assert.Equal(2, inspection.PropertyCount);
        Assert.NotNull(inspection.Properties);
        Assert.Equal("foo", inspection.Properties[0].Name);
        Assert.Equal("int32", inspection.Properties[0].Kind);
        Assert.Equal(42, inspection.Properties[0].Value);
        Assert.Equal("name", inspection.Properties[1].Name);
        Assert.Equal("string", inspection.Properties[1].Kind);
        Assert.Equal("bar", inspection.Properties[1].Value);
    }

    [Fact]
    public void Read_ReturnsSupportedScalarPropertyEncodings()
    {
        var bytes = CreatePropertyImage(
            CreateNullProperty("nil"),
            CreateScalarProperty("short", 0x02, BitConverter.GetBytes((short)1234)),
            CreateScalarProperty("shortAlt", 0x0b, BitConverter.GetBytes((short)-123)),
            CreateScalarProperty("intSmall", 0x03, 12),
            CreateScalarProperty("intLong", 0x03, 0x80, BitConverter.GetBytes(123456789)),
            CreateScalarProperty("intAlt", 0x13, 0x80, BitConverter.GetBytes(-7654321)),
            CreateScalarProperty("longSmall", 0x14, unchecked((byte)(sbyte)-7)),
            CreateScalarProperty("longLong", 0x14, 0x80, BitConverter.GetBytes(1234567890123L)),
            CreateScalarProperty("singleSmall", 0x04, 3),
            CreateScalarProperty("singleLong", 0x04, 0x80, BitConverter.GetBytes(12.5f)),
            CreateScalarProperty("double", 0x05, BitConverter.GetBytes(123.25d)),
            CreateScalarProperty("text", 0x08, CreateImageString("hello")));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(inspection.Properties);
        Assert.Equal(12, inspection.PropertyCount);
        Assert.Collection(
            inspection.Properties,
            property =>
            {
                Assert.Equal("nil", property.Name);
                Assert.Equal("null", property.Kind);
                Assert.Null(property.Value);
            },
            property =>
            {
                Assert.Equal("short", property.Name);
                Assert.Equal("int16", property.Kind);
                Assert.Equal((short)1234, property.Value);
            },
            property =>
            {
                Assert.Equal("shortAlt", property.Name);
                Assert.Equal("int16", property.Kind);
                Assert.Equal((short)-123, property.Value);
            },
            property =>
            {
                Assert.Equal("intSmall", property.Name);
                Assert.Equal("int32", property.Kind);
                Assert.Equal(12, property.Value);
            },
            property =>
            {
                Assert.Equal("intLong", property.Name);
                Assert.Equal("int32", property.Kind);
                Assert.Equal(123456789, property.Value);
            },
            property =>
            {
                Assert.Equal("intAlt", property.Name);
                Assert.Equal("int32", property.Kind);
                Assert.Equal(-7654321, property.Value);
            },
            property =>
            {
                Assert.Equal("longSmall", property.Name);
                Assert.Equal("int64", property.Kind);
                Assert.Equal(-7L, property.Value);
            },
            property =>
            {
                Assert.Equal("longLong", property.Name);
                Assert.Equal("int64", property.Kind);
                Assert.Equal(1234567890123L, property.Value);
            },
            property =>
            {
                Assert.Equal("singleSmall", property.Name);
                Assert.Equal("single", property.Kind);
                Assert.Equal(3f, property.Value);
            },
            property =>
            {
                Assert.Equal("singleLong", property.Name);
                Assert.Equal("single", property.Kind);
                Assert.Equal(12.5f, property.Value);
            },
            property =>
            {
                Assert.Equal("double", property.Name);
                Assert.Equal("double", property.Kind);
                Assert.Equal(123.25d, property.Value);
            },
            property =>
            {
                Assert.Equal("text", property.Name);
                Assert.Equal("string", property.Kind);
                Assert.Equal("hello", property.Value);
            });
    }

    [Fact]
    public void Read_ReturnsEmptyNestedProperty()
    {
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "empty",
            CreateObjectValue("Property", 0x00, 0x00, 0)));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(
            new WzStringDecryptor(WzStringEncryptionKind.None),
            maxPropertyDepth: 2);

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(inspection.Properties);
        var property = Assert.Single(inspection.Properties);
        Assert.Equal("empty", property.Name);
        Assert.Equal("object", property.Kind);
        Assert.Equal("Property", property.Value);
        Assert.Equal(0, property.ChildCount);
        Assert.NotNull(property.Children);
        Assert.Empty(property.Children);
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
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None), maxPropertyDepth: 2);

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(inspection.Properties);
        var property = Assert.Single(inspection.Properties);
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
    public void Read_SkipsNestedPropertyChildrenWhenDepthStopsAtParent()
    {
        var bytes = CreatePropertyImage(
            CreateObjectProperty(
                "child",
                CreateObjectValue(
                    "Property",
                    0x00,
                    0x00,
                    1,
                    CreateImageString("foo"),
                    0x03,
                    42)),
            CreateScalarProperty("after", 7));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(
            new WzStringDecryptor(WzStringEncryptionKind.None),
            maxPropertyDepth: 1);

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(inspection.Properties);
        Assert.Equal(2, inspection.Properties.Count);
        var property = inspection.Properties[0];
        Assert.Equal("child", property.Name);
        Assert.Equal("object", property.Kind);
        Assert.Equal("Property", property.Value);
        Assert.Null(property.ChildCount);
        Assert.Null(property.Children);
        Assert.Equal("after", inspection.Properties[1].Name);
        Assert.Equal(7, inspection.Properties[1].Value);
    }

    [Fact]
    public void Read_DefaultDepthExpandsNestedProperty()
    {
        var bytes = CreatePropertyImage(
            CreateObjectProperty(
                "child",
                CreateObjectValue(
                    "Property",
                    0x00,
                    0x00,
                    1,
                    CreateImageString("foo"),
                    0x03,
                    42)));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(inspection.Properties);
        var property = Assert.Single(inspection.Properties);
        Assert.NotNull(property.Children);
        var child = Assert.Single(property.Children);
        Assert.Equal("foo", child.Name);
        Assert.Equal(42, child.Value);
    }

    [Fact]
    public void Constructor_RejectsDepthAboveLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new WzImageInspectionReader(maxPropertyDepth: WzImageInspectionReader.MaxPropertyInspectionDepth + 1));
    }

    [Fact]
    public void Read_ReturnsVectorObjectValue()
    {
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "origin",
            CreateObjectValue("Shape2D#Vector2D", 12, 34)));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(inspection.Properties);
        var property = Assert.Single(inspection.Properties);
        Assert.Equal("vector", property.Kind);
        var vector = Assert.IsType<WzImageVectorInspection>(property.Value);
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
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(inspection.Properties);
        var property = Assert.Single(inspection.Properties);
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
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(inspection.Properties);
        var property = Assert.Single(inspection.Properties);
        Assert.Equal("canvas", property.Kind);
        var canvas = Assert.IsType<WzImageCanvasInspection>(property.Value);
        Assert.Equal(16, canvas.Width);
        Assert.Equal(8, canvas.Height);
        Assert.Equal(2, canvas.Format);
        Assert.Equal(1, canvas.Pages);
        Assert.Equal(3, canvas.DataLength);
        Assert.Equal(WzImageCanvasCompressionKind.ChunkedEncryptedZlib, canvas.CompressionKind);
        Assert.Equal(512, canvas.UncompressedDataLength);
    }

    [Fact]
    public void Read_ConsumesCanvasMiniPropertiesWhenDepthStopsAtParent()
    {
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "icon",
            CreateObjectValue(
                "Canvas",
                0x00,
                0x01,
                0x00,
                0x00,
                1,
                CreateImageString("kind"),
                0x03,
                42,
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
        var reader = new WzImageInspectionReader(
            new WzStringDecryptor(WzStringEncryptionKind.None),
            maxPropertyDepth: 1);

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(inspection.Properties);
        var property = Assert.Single(inspection.Properties);
        Assert.Equal("canvas", property.Kind);
        Assert.Equal(1, property.ChildCount);
        Assert.Null(property.Children);
        var canvas = Assert.IsType<WzImageCanvasInspection>(property.Value);
        Assert.Equal(16, canvas.Width);
        Assert.Equal(8, canvas.Height);
        Assert.Equal(3, canvas.DataLength);
        Assert.Equal(WzImageCanvasCompressionKind.ChunkedEncryptedZlib, canvas.CompressionKind);
    }

    [Fact]
    public void Read_ReturnsTopLevelCanvasMetadata()
    {
        var bytes = CreateImage(
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
            0x03);
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.Equal("Canvas", inspection.ObjectType);
        var canvas = Assert.IsType<WzImageCanvasInspection>(inspection.ObjectValue);
        Assert.Equal(16, canvas.Width);
        Assert.Equal(8, canvas.Height);
        Assert.Equal(2, canvas.Format);
        Assert.Equal(1, canvas.Pages);
        Assert.Equal(3, canvas.DataLength);
        Assert.Equal(WzImageCanvasCompressionKind.ChunkedEncryptedZlib, canvas.CompressionKind);
        Assert.Equal(512, canvas.UncompressedDataLength);
    }

    [Fact]
    public void Read_ReturnsRgba32FloatCanvasUncompressedLength()
    {
        var bytes = CreateImage(
            "Canvas",
            0x00,
            0x00,
            16,
            8,
            CreateCompressedInt32(4100),
            0x00,
            1,
            0,
            (byte)0x00,
            (byte)0x00,
            BitConverter.GetBytes(3),
            0x01,
            0x02,
            0x03);
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        var canvas = Assert.IsType<WzImageCanvasInspection>(inspection.ObjectValue);
        Assert.Equal(4100, canvas.Format);
        Assert.Equal(2048, canvas.UncompressedDataLength);
    }

    [Fact]
    public void Read_ReturnsCanvasZlibCompressionMetadata()
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
                0x00,
                0x78,
                0x9c)));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(inspection.Properties);
        var canvas = Assert.IsType<WzImageCanvasInspection>(Assert.Single(inspection.Properties).Value);
        Assert.Equal(WzImageCanvasCompressionKind.Zlib, canvas.CompressionKind);
        Assert.Equal(512, canvas.UncompressedDataLength);
    }

    [Fact]
    public void Read_ReturnsTopLevelVectorValue()
    {
        var bytes = CreateImage("Shape2D#Vector2D", 12, 34);
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.Equal("Shape2D#Vector2D", inspection.ObjectType);
        Assert.Equal(new WzImageVectorInspection(12, 34), inspection.ObjectValue);
    }

    [Fact]
    public void Read_DoesNotReadTopLevelObjectValueWhenDepthIsZero()
    {
        var bytes = CreateImage("Shape2D#Vector2D", 12, 34);
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(
            new WzStringDecryptor(WzStringEncryptionKind.None),
            maxPropertyDepth: 0);

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.Equal("Shape2D#Vector2D", inspection.ObjectType);
        Assert.Null(inspection.ObjectValue);
    }

    [Fact]
    public void Read_ReturnsLuaInspectionForLuaImage()
    {
        var bytes = CreateLuaImage("return 42\n");
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var inspection = reader.Read(
            stream,
            CreateHeader(),
            CreateImageEntry(offset: 4, dataSize: bytes.Length - 4, name: "Synthetic.lua"),
            "0");

        Assert.Equal("Lua", inspection.ObjectType);
        Assert.Equal(1, inspection.PropertyCount);
        var lua = Assert.IsType<WzImageLuaInspection>(inspection.ObjectValue);
        Assert.Equal(10, lua.ScriptLength);
        Assert.Equal("return 42\\n", lua.Snippet);
        Assert.Equal("return 42\n", lua.Script);
        Assert.NotNull(inspection.Properties);
        Assert.Equal("lua", Assert.Single(inspection.Properties).Kind);
    }

    [Fact]
    public void Read_DoesNotReadLuaPayloadWhenDepthIsZero()
    {
        var bytes = CreateLuaImage("return 42\n");
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(
            new WzStringDecryptor(WzStringEncryptionKind.None),
            maxPropertyDepth: 0);

        var inspection = reader.Read(
            stream,
            CreateHeader(),
            CreateImageEntry(offset: 4, dataSize: bytes.Length - 4, name: "Synthetic.lua"),
            "0");

        Assert.Equal("Lua", inspection.ObjectType);
        Assert.Null(inspection.ObjectValue);
        Assert.Null(inspection.PropertyCount);
        Assert.Null(inspection.Properties);
    }

    [Fact]
    public void Read_ReturnsTextPropertyV1Inspection()
    {
        var bytes = CreateTextImage("""
            #Property
            name = Beginner Cap
            reqLevel = 0
            info = {
              attack = 1
            }
            """);
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(
            new WzStringDecryptor(WzStringEncryptionKind.None),
            maxPropertyDepth: 2);

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.Equal("Property", inspection.ObjectType);
        var text = Assert.IsType<WzImageTextInspection>(inspection.ObjectValue);
        Assert.Equal("v1", text.Format);
        Assert.Contains("Beginner Cap", text.Text);
        Assert.Equal(3, inspection.PropertyCount);
        Assert.NotNull(inspection.Properties);
        Assert.Equal("name", inspection.Properties[0].Name);
        Assert.Equal("string", inspection.Properties[0].Kind);
        Assert.Equal("Beginner Cap", inspection.Properties[0].Value);
        Assert.Equal("int32", inspection.Properties[1].Kind);
        Assert.Equal(0, inspection.Properties[1].Value);
        var info = inspection.Properties[2];
        Assert.Equal("object", info.Kind);
        Assert.Equal(1, info.ChildCount);
        Assert.NotNull(info.Children);
        Assert.Equal("info/attack", Assert.Single(info.Children).Path);
    }

    [Fact]
    public void Read_ReturnsTextPropertyV2Inspection()
    {
        var bytes = CreateTextImage(
            "Root <Property>\n" +
            "\ttab <Vector>\t12,34\n" +
            "\ttitle <String>\tHello\n" +
            "\tnested <Property>\t[no_binary]\n" +
            "\t\tcount <I4>\t7\n");
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(
            new WzStringDecryptor(WzStringEncryptionKind.None),
            maxPropertyDepth: 2);

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.Equal("Property", inspection.ObjectType);
        var text = Assert.IsType<WzImageTextInspection>(inspection.ObjectValue);
        Assert.Equal("v2", text.Format);
        Assert.Contains("Root <Property>", text.Text);
        Assert.Equal(3, inspection.PropertyCount);
        Assert.NotNull(inspection.Properties);
        Assert.Equal(new WzImageVectorInspection(12, 34), inspection.Properties[0].Value);
        Assert.Equal("Hello", inspection.Properties[1].Value);
        var nested = inspection.Properties[2];
        Assert.Equal("object", nested.Kind);
        Assert.Equal(1, nested.ChildCount);
        Assert.NotNull(nested.Children);
        Assert.Equal(7, Assert.Single(nested.Children).Value);
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
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(inspection.Properties);
        var property = Assert.Single(inspection.Properties);
        Assert.Equal("convex", property.Kind);
        var convex = Assert.IsType<WzImageConvexInspection>(property.Value);
        Assert.Equal(2, convex.Points.Count);
        Assert.Equal(new WzImageVectorInspection(1, 2), convex.Points[0]);
        Assert.Equal(new WzImageVectorInspection(3, 4), convex.Points[1]);
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
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

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
        var reader = new WzImageInspectionReader(
            new WzStringDecryptor(WzStringEncryptionKind.None),
            maxPropertyDepth: 2);

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(inspection.Properties);
        var property = Assert.Single(inspection.Properties);
        Assert.Equal("rawData", property.Kind);
        Assert.Equal(1, property.ChildCount);
        Assert.NotNull(property.Children);
        Assert.Equal("kind", Assert.Single(property.Children).Name);
        var rawData = Assert.IsType<WzImageRawDataInspection>(property.Value);
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
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

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
        var reader = new WzImageInspectionReader(
            new WzStringDecryptor(WzStringEncryptionKind.None),
            maxPropertyDepth: 2);

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(inspection.Properties);
        var property = Assert.Single(inspection.Properties);
        Assert.Equal("video", property.Kind);
        Assert.Equal(1, property.ChildCount);
        Assert.NotNull(property.Children);
        Assert.Equal("kind", Assert.Single(property.Children).Name);
        var video = Assert.IsType<WzImageVideoInspection>(property.Value);
        Assert.Equal(5, video.Unknown);
        Assert.Equal(4, video.DataLength);
        Assert.Null(video.Header);
        Assert.NotNull(video.HeaderError);
    }

    [Fact]
    public void Read_ReturnsVideoMcvHeaderMetadata()
    {
        var payload = CreateMcvVideoPayload();
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "clip",
            CreateObjectValue(
                "Canvas#Video",
                0x00,
                0x00,
                5,
                CreateCompressedInt32(payload.Length),
                payload)));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(
            new WzStringDecryptor(WzStringEncryptionKind.None),
            maxPropertyDepth: 2);

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(inspection.Properties);
        var property = Assert.Single(inspection.Properties);
        var video = Assert.IsType<WzImageVideoInspection>(property.Value);
        Assert.Equal(5, video.Unknown);
        Assert.Equal(payload.Length, video.DataLength);
        Assert.Null(video.HeaderError);

        var header = Assert.IsType<WzImageVideoHeaderInspection>(video.Header);
        Assert.Equal("MCV0", header.Signature);
        Assert.Equal(36, header.HeaderLength);
        Assert.Equal("VP80", header.FourCcText);
        Assert.Equal(320, header.Width);
        Assert.Equal(180, header.Height);
        Assert.Equal(2, header.FrameCount);
        Assert.Equal(
            WzImageVideoDataFlags.AlphaMap |
            WzImageVideoDataFlags.PerFrameDelay |
            WzImageVideoDataFlags.PerFrameTimeline,
            header.DataFlags);
        Assert.Equal(1000, header.FrameDelayUnit);
        Assert.Equal(33, header.DefaultDelay);
        Assert.Collection(
            header.Frames,
            frame =>
            {
                Assert.Equal(0, frame.Index);
                Assert.Equal(92, frame.DataOffset);
                Assert.Equal(3, frame.DataLength);
                Assert.Equal(99, frame.AlphaDataOffset);
                Assert.Equal(2, frame.AlphaDataLength);
                Assert.Equal(16000, frame.DelayInNanoseconds);
                Assert.Equal(0, frame.StartTimeInNanoseconds);
            },
            frame =>
            {
                Assert.Equal(1, frame.Index);
                Assert.Equal(95, frame.DataOffset);
                Assert.Equal(4, frame.DataLength);
                Assert.Equal(101, frame.AlphaDataOffset);
                Assert.Equal(2, frame.AlphaDataLength);
                Assert.Equal(17000, frame.DelayInNanoseconds);
                Assert.Equal(16000, frame.StartTimeInNanoseconds);
            });
    }

    [Fact]
    public void Read_RejectsVideoExtendingPastImage()
    {
        var bytes = CreatePropertyImage(CreateObjectProperty(
            "clip",
            CreateObjectValue("Canvas#Video", 0x00, 0x00, 5, 100)));
        using var stream = new MemoryStream(bytes);
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

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
        var reader = new WzImageInspectionReader(
            new WzStringDecryptor(WzStringEncryptionKind.None),
            maxPropertyDepth: 2);

        var inspection = reader.Read(stream, CreateHeader(), CreateImageEntry(offset: 4, dataSize: bytes.Length - 4), "0");

        Assert.NotNull(inspection.Properties);
        var property = Assert.Single(inspection.Properties);
        Assert.Equal("sound", property.Kind);
        Assert.Equal(1, property.ChildCount);
        Assert.NotNull(property.Children);
        Assert.Equal("kind", Assert.Single(property.Children).Name);
        var sound = Assert.IsType<WzImageSoundInspection>(property.Value);
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
        var reader = new WzImageInspectionReader(new WzStringDecryptor(WzStringEncryptionKind.None));

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

    private static WzDirectoryEntryInspection CreateImageEntry(
        long offset,
        int dataSize = 10,
        string name = "Synthetic.img")
    {
        return new WzDirectoryEntryInspection(
            Index: 0,
            NodeType: 0x04,
            WzDirectoryEntryKind.Image,
            Name: name,
            DataSize: dataSize,
            Checksum: 0,
            HashOffsetPosition: 0,
            HashOffset: 0,
            Offset: offset);
    }

    private static byte[] CreatePropertyImage(params byte[][] entries)
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

    private static byte[] CreateLuaImage(string script)
    {
        var payload = System.Text.Encoding.UTF8.GetBytes(script);
        var bytes = new List<byte> { 0x00, 0x00, 0x00, 0x00, 0x01 };
        bytes.Add((byte)payload.Length);
        bytes.AddRange(payload);
        return bytes.ToArray();
    }

    private static byte[] CreateTextImage(string text)
    {
        var payload = System.Text.Encoding.UTF8.GetBytes(text.ReplaceLineEndings("\n"));
        var bytes = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
        bytes.AddRange(payload);
        return bytes.ToArray();
    }

    private static byte[] CreateImage(string objectType, params object[] payloadParts)
    {
        var bytes = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
        AddImageObjectName(bytes, objectType);
        AddPayloadParts(bytes, payloadParts);
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

    private static byte[] CreateScalarProperty(string name, int value)
    {
        var bytes = new List<byte>();
        bytes.AddRange(CreateImageString(name));
        bytes.Add(0x03);
        bytes.Add((byte)value);
        return bytes.ToArray();
    }

    private static byte[] CreateObjectValue(string objectType, params object[] payloadParts)
    {
        var bytes = new List<byte>();
        AddImageObjectName(bytes, objectType);
        AddPayloadParts(bytes, payloadParts);

        return bytes.ToArray();
    }

    private static byte[] CreateNullProperty(string name)
    {
        var bytes = new List<byte>();
        bytes.AddRange(CreateImageString(name));
        bytes.Add(0x00);
        return bytes.ToArray();
    }

    private static byte[] CreateScalarProperty(string name, byte type, params object[] payloadParts)
    {
        var bytes = new List<byte>();
        bytes.AddRange(CreateImageString(name));
        bytes.Add(type);
        AddPayloadParts(bytes, payloadParts);
        return bytes.ToArray();
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

    private static byte[] CreateCompressedInt32(int value)
    {
        return [(byte)0x80, .. BitConverter.GetBytes(value)];
    }

    private static byte[] CreateMcvVideoPayload()
    {
        const uint fourCc = 0x30385056; // VP80 in little-endian FourCC order.
        var bytes = new List<byte>();
        bytes.AddRange("MCV0"u8.ToArray());
        bytes.AddRange(new byte[2]);
        bytes.AddRange(BitConverter.GetBytes((ushort)36));
        bytes.AddRange(BitConverter.GetBytes(fourCc ^ 0xa5a5a5a5u));
        bytes.AddRange(BitConverter.GetBytes((ushort)320));
        bytes.AddRange(BitConverter.GetBytes((ushort)180));
        bytes.AddRange(BitConverter.GetBytes(2));
        bytes.Add((byte)(WzImageVideoDataFlags.AlphaMap | WzImageVideoDataFlags.PerFrameDelay | WzImageVideoDataFlags.PerFrameTimeline));
        bytes.AddRange(new byte[3]);
        bytes.AddRange(BitConverter.GetBytes(1000L));
        bytes.AddRange(BitConverter.GetBytes(33));

        bytes.AddRange(BitConverter.GetBytes(0));
        bytes.AddRange(BitConverter.GetBytes(3));
        bytes.AddRange(BitConverter.GetBytes(3));
        bytes.AddRange(BitConverter.GetBytes(4));

        bytes.AddRange(BitConverter.GetBytes(7));
        bytes.AddRange(BitConverter.GetBytes(2));
        bytes.AddRange(BitConverter.GetBytes(9));
        bytes.AddRange(BitConverter.GetBytes(2));

        bytes.AddRange(BitConverter.GetBytes(16));
        bytes.AddRange(BitConverter.GetBytes(17));

        bytes.AddRange(BitConverter.GetBytes(0L));
        bytes.AddRange(BitConverter.GetBytes(16L));

        bytes.AddRange(CreateBytes(0xee, 11));
        return bytes.ToArray();
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
