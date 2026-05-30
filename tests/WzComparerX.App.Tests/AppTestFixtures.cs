using System.Buffers.Binary;
using System.Text;
using WzComparerX.WzLib;

namespace WzComparerX.App.Tests;

internal static class AppTestFixtures
{
    public static string FixturePath(string name)
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "fixtures",
            "synthetic",
            name);
    }

    public static string MaterializeHexFixture(string name, string extension)
    {
        var hex = new StringBuilder();
        foreach (var ch in File.ReadAllText(FixturePath(name)))
        {
            if (Uri.IsHexDigit(ch))
            {
                hex.Append(ch);
            }
        }

        var path = Path.Combine(Path.GetTempPath(), $"wcx-app-fixture-{Guid.NewGuid():N}{extension}");
        File.WriteAllBytes(path, Convert.FromHexString(hex.ToString()));
        return path;
    }

    public static byte[] CreatePkg1()
    {
        byte[] encryptedVersion = [0x7b, 0x00];
        byte[] directoryData = [0x00];
        var header = CreateHeader("PKG1", "Copyright", dataSize: encryptedVersion.Length + directoryData.Length);
        return [.. header, .. encryptedVersion, .. directoryData];
    }

    public static byte[] CreatePkg1DirectoryStubPackage(string name)
    {
        byte[] encryptedVersion = [0x7b, 0x00];
        var directoryData = CreateDirectoryStub(name);
        var header = CreateHeader("PKG1", "Copyright", dataSize: encryptedVersion.Length + directoryData.Length);
        return [.. header, .. encryptedVersion, .. directoryData];
    }

    public static byte[] CreatePkg1ImagePackage(string imageName, byte[] imageBytes)
    {
        var directoryData = CreateDirectoryDataForImage(imageName, imageBytes.Length - 4);
        var header = CreateHeader("PKG1", string.Empty, dataSize: directoryData.Length + imageBytes.Length);
        return [.. header, .. directoryData, .. imageBytes];
    }

    public static byte[] CreateCanvasPropertyImage(string propertyName, byte[] pixels)
    {
        return CreatePropertyImage(CreateObjectProperty(propertyName, CreateCanvasObjectValue(pixels)));
    }

    public static byte[] CreateLinkedCanvasPropertyImage(string propertyName, string linkName, string linkValue)
    {
        byte[] pixels = [0x00, 0x00, 0x00, 0x00];
        var payload = CreateDirectZlibPayload(pixels);
        return CreatePropertyImage(CreateObjectProperty(
            propertyName,
            CreateObjectValue(
                "Canvas",
                0x00,
                0x01,
                0x00,
                0x00,
                1,
                CreateImageString(linkName),
                0x08,
                CreateImageString(linkValue),
                1,
                1,
                2,
                0x00,
                1,
                0,
                (byte)0x00,
                (byte)0x00,
                BitConverter.GetBytes(payload.Length),
                payload)));
    }

    public static byte[] CreateUolToLinkedCanvasPropertyImage(
        string uolParentName,
        string uolPropertyName,
        string uolTarget,
        string canvasParentName,
        string canvasPropertyName,
        string linkName,
        string linkValue)
    {
        return CreatePropertyImage(
            CreateObjectProperty(
                uolParentName,
                CreateObjectValue(
                    "Property",
                    0x00,
                    0x00,
                    1,
                    CreateObjectProperty(
                        uolPropertyName,
                        CreateObjectValue(
                            "UOL",
                            0x00,
                            CreateImageString(uolTarget))))),
            CreateObjectProperty(
                canvasParentName,
                CreateObjectValue(
                    "Property",
                    0x00,
                    0x00,
                    1,
                    CreateLinkedCanvasProperty(
                        canvasPropertyName,
                        linkName,
                        linkValue))));
    }

    private static byte[] CreateDirectoryStub(string name)
    {
        var bytes = new List<byte> { 0x01, 0x03 };
        AddWzString(bytes, name);
        bytes.Add(0x00);
        bytes.Add(0x00);
        bytes.AddRange(BitConverter.GetBytes(0u));
        bytes.Add(0x00);
        return bytes.ToArray();
    }

    private static byte[] CreateDirectoryDataForImage(string name, int imageSize)
    {
        var bytes = new List<byte> { 0x01, 0x04 };
        AddWzString(bytes, name);
        AddCompressedInt32(bytes, imageSize);
        bytes.Add(0x00);
        var hashOffsetPosition = bytes.Count + 16;
        var imageOffset = 16 + bytes.Count + sizeof(uint) + 4;
        var hashOffset = CreateHashOffset(
            hashOffsetPosition: checked((uint)hashOffsetPosition),
            desiredOffset: checked((uint)imageOffset));
        bytes.AddRange(BitConverter.GetBytes(hashOffset));
        return bytes.ToArray();
    }

    private static void AddCompressedInt32(List<byte> bytes, int value)
    {
        if (value > sbyte.MinValue && value <= sbyte.MaxValue)
        {
            bytes.Add(unchecked((byte)(sbyte)value));
            return;
        }

        bytes.Add(0x80);
        bytes.AddRange(BitConverter.GetBytes(value));
    }

    private static uint CreateHashOffset(uint hashOffsetPosition, uint desiredOffset)
    {
        const uint headerSize = 16;
        var hashVersion = WzPkg1VersionHash.CalculateHashVersion(777);
        unchecked
        {
            var offset = hashOffsetPosition - headerSize;
            offset = ~offset;
            offset *= hashVersion;
            offset -= 0x581C3F6D;
            var distance = (int)offset & 0x1F;
            offset = (offset << distance) | (offset >> (32 - distance));
            return offset ^ (desiredOffset - headerSize * 2);
        }
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

    private static byte[] CreateCanvasObjectValue(byte[] pixels)
    {
        var payload = CreateDirectZlibPayload(pixels);
        return CreateObjectValue(
            "Canvas",
            0x00,
            0x00,
            1,
            1,
            2,
            0x00,
            1,
            0,
            (byte)0x00,
            (byte)0x00,
            BitConverter.GetBytes(payload.Length),
            payload);
    }

    private static byte[] CreateLinkedCanvasProperty(string name, string linkName, string linkValue)
    {
        byte[] pixels = [0x00, 0x00, 0x00, 0x00];
        var payload = CreateDirectZlibPayload(pixels);
        return CreateObjectProperty(
            name,
            CreateObjectValue(
                "Canvas",
                0x00,
                0x01,
                0x00,
                0x00,
                1,
                CreateImageString(linkName),
                0x08,
                CreateImageString(linkValue),
                1,
                1,
                2,
                0x00,
                1,
                0,
                (byte)0x00,
                (byte)0x00,
                BitConverter.GetBytes(payload.Length),
                payload));
    }

    private static byte[] CreateDirectZlibPayload(byte[] pixels)
    {
        using var output = new MemoryStream();
        output.WriteByte(0x00);
        using (var zlib = new System.IO.Compression.ZLibStream(output, System.IO.Compression.CompressionMode.Compress, leaveOpen: true))
        {
            zlib.Write(pixels);
        }

        return output.ToArray();
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
        AddPayloadParts(bytes, payloadParts);
        return bytes.ToArray();
    }

    private static byte[] CreateImage(string objectType, params object[] payloadParts)
    {
        var bytes = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
        AddImageObjectName(bytes, objectType);
        AddPayloadParts(bytes, payloadParts);
        return bytes.ToArray();
    }

    private static byte[] CreateImageString(string value)
    {
        var bytes = new List<byte> { 0x00 };
        AddWzString(bytes, value);
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
