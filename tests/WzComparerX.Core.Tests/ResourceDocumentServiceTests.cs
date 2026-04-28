using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using WzComparerX.Core;
using WzComparerX.WzLib;

namespace WzComparerX.Core.Tests;

public class ResourceDocumentServiceTests
{
    [Fact]
    public async Task OpenAsync_AddsSyntheticDocumentToWorkspace()
    {
        var service = new ResourceDocumentService();
        var workspace = new ResourceWorkspace();

        var document = await service.OpenAsync(FixturePath("basic-tree.json"));
        workspace.Add(document);

        Assert.Single(workspace.Documents);
        Assert.Equal("basic-tree", workspace.Documents[0].Root.Name);
        Assert.True(Path.IsPathFullyQualified(workspace.Documents[0].SourcePath));
    }

    [Fact]
    public async Task Format_ReturnsDeterministicTreeListing()
    {
        var service = new ResourceDocumentService();
        var formatter = new ResourceTreeListFormatter();

        var document = await service.OpenAsync(FixturePath("basic-tree.json"));

        var output = formatter.Format(document);

        Assert.Equal(
            """
            basic-tree [directory]
              Character.wz [directory]
                Cap [directory]
                  00002000.img [image]
                    info [property]
                      name [value] : string = "Beginner Cap"
                      reqLevel [value] : int32 = 0
              String.wz [directory]
                Eqp.img [image]
                  Eqp [property]

            """.ReplaceLineEndings(),
            output);
    }

    [Fact]
    public async Task FormatJson_ReturnsDeterministicTreeDocument()
    {
        var service = new ResourceDocumentService();
        var formatter = new ResourceTreeJsonFormatter();

        var document = await service.OpenAsync(FixturePath("basic-tree.json"));

        var output = formatter.Format(document);
        using var json = JsonDocument.Parse(output);
        var root = json.RootElement.GetProperty("Root");

        Assert.True(json.RootElement.TryGetProperty("SourcePath", out _));
        Assert.Equal("basic-tree", root.GetProperty("Name").GetString());
        Assert.Equal("Directory", root.GetProperty("Kind").GetString());
        Assert.Contains("Beginner Cap", output);
    }

    [Fact]
    public async Task InspectSynthetic_ReturnsStableInspectionTree()
    {
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionFormatter();

        var inspection = await service.InspectAsync(FixturePath("basic-tree.json"));
        var output = formatter.Format(inspection);

        Assert.Equal("synthetic", inspection.Format);
        Assert.Equal("basic-tree", inspection.Root.Name);
        Assert.Contains("source: ", output);
        Assert.Contains("basic-tree [directory]", output);
        Assert.Contains("name [value] : string = \"Beginner Cap\"", output);
    }

    [Fact]
    public async Task InspectSyntheticJson_ReturnsInspectionDocument()
    {
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionJsonFormatter();

        var inspection = await service.InspectAsync(FixturePath("basic-tree.json"));
        var output = formatter.Format(inspection);
        using var json = JsonDocument.Parse(output);

        Assert.Equal("synthetic", json.RootElement.GetProperty("Format").GetString());
        Assert.Equal("basic-tree", json.RootElement.GetProperty("Root").GetProperty("Name").GetString());
    }

    [Fact]
    public async Task InspectInvalidWz_ThrowsInvalidDataException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wcx-invalid-{Guid.NewGuid():N}.wz");
        await File.WriteAllTextAsync(path, "NOPE");

        try
        {
            var service = new ResourceInspectionService();

            await Assert.ThrowsAsync<InvalidDataException>(
                () => service.InspectAsync(path, stringKey: WzComparerX.WzLib.WzStringEncryptionKind.None));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectDirectory_LinksSiblingSplitPackagesForEmptyTopLevelDirectories()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-split-package-");
        var baseDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Base"));
        var effectDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Effect"));
        var basePath = Path.Combine(baseDirectory.FullName, "Base.wz");
        var effectPath = Path.Combine(effectDirectory.FullName, "Effect.wz");
        var effectShardPath = Path.Combine(effectDirectory.FullName, "Effect_000.wz");
        await File.WriteAllBytesAsync(basePath, CreatePkg1DirectoryPackage(CreateDirectoryStub("Effect")));
        await File.WriteAllBytesAsync(effectPath, CreatePkg1DirectoryPackage(CreateDirectoryStub("_Canvas")));
        await File.WriteAllBytesAsync(effectShardPath, CreatePkg1DirectoryPackage(CreateImageDirectory("BasicEff.img")));
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                basePath,
                selector: null,
                new ResourceInspectionOptions(WzStringEncryptionKind.None, IncludeDebugMetadata: true));

            var effect = Assert.Single(inspection.Root.Children, child => child.Name == "Effect");
            Assert.Equal("directory", effect.Kind);
            Assert.Equal(2, effect.Children.Count);

            var primaryPackage = Assert.Single(effect.Children, child => child.Name == "Effect.wz");
            Assert.Equal("package", primaryPackage.Kind);
            Assert.Equal(effectPath, primaryPackage.Path);
            Assert.Contains(primaryPackage.Children, child => child.Name == "_Canvas");

            var shardPackage = Assert.Single(effect.Children, child => child.Name == "Effect_000.wz");
            Assert.Equal("package", shardPackage.Kind);
            Assert.Equal(effectShardPath, shardPackage.Path);
            Assert.Contains(shardPackage.Children, child => child.Name == "BasicEff.img" && child.Kind == "image");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectDebugImage_IncludesEntryPropertyAndPayloadMetadata()
    {
        var imageBytes = CreatePropertyImage(CreateObjectProperty(
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
        var path = WriteTemporaryPkg1ImageFile(imageBytes);
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionFormatter();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                selector: "Canvas.img",
                new ResourceInspectionOptions(WzStringEncryptionKind.None, MaxPropertyDepth: 2, IncludeDebugMetadata: true));
            var output = formatter.Format(inspection);

            Assert.Contains("entryDataSize:", output);
            Assert.Contains("objectType: Property", output);
            Assert.Contains("icon [canvas]", output);
            Assert.Contains("type: 0x09", output);
            Assert.Contains("dataOffset:", output);
            Assert.Contains("dataLength: 3", output);
            Assert.Contains("compressionKind: Zlib", output);
            Assert.Contains("Canvas pixel decoding is not implemented.", output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectNormalImage_DoesNotIncludeDebugMetadata()
    {
        var path = WriteTemporaryPkg1ImageFile(CreatePropertyImage());
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionFormatter();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                selector: "Canvas.img",
                new ResourceInspectionOptions(WzStringEncryptionKind.None));
            var output = formatter.Format(inspection);

            Assert.DoesNotContain("debug:", output);
            Assert.DoesNotContain("entryDataSize", output);
            Assert.DoesNotContain("nodeType", output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectDebugJson_IncludesStructuredMetadata()
    {
        var path = WriteTemporaryPkg1ImageFile(CreatePropertyImage());
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionJsonFormatter();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                selector: "Canvas.img",
                new ResourceInspectionOptions(WzStringEncryptionKind.None, IncludeDebugMetadata: true));
            var output = formatter.Format(inspection);
            using var json = JsonDocument.Parse(output);

            Assert.True(json.RootElement.TryGetProperty("DebugMetadata", out var metadata));
            Assert.Contains(metadata.EnumerateArray(), item =>
                item.GetProperty("Name").GetString() == "stringKey" &&
                item.GetProperty("Value").GetString() == "none");
            Assert.True(json.RootElement.GetProperty("Root").TryGetProperty("DebugMetadata", out _));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DiagnosticFormatter_ReturnsStableCliText()
    {
        var diagnostic = ResourceInspectionDiagnostics.ExportUnsupported(ResourceExportKind.Lua, "Text.img");

        var output = ResourceInspectionDiagnosticFormatter.Format(diagnostic);

        Assert.Equal(
            "error [wcx.export.unsupported]: Selected image is not a supported Lua IMG: Text.img. (Text.img)",
            output);
    }

    [Fact]
    public void DiagnosticFormatter_OmitsEmptyCodeAndPath()
    {
        var diagnostic = new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Info,
            "Nothing to report.",
            Code: " ",
            Path: "");

        var output = ResourceInspectionDiagnosticFormatter.Format(diagnostic);

        Assert.Equal("info: Nothing to report.", output);
    }

    [Fact]
    public void DiagnosticsFactory_ReturnsStablePayloadDiagnostic()
    {
        var diagnostic = ResourceInspectionDiagnostics.CanvasPixelDecodingUnsupported("icon");

        Assert.Equal(ResourceDiagnosticSeverities.Info, diagnostic.Severity);
        Assert.Equal("Canvas pixel decoding is not implemented.", diagnostic.Message);
        Assert.Equal("icon", diagnostic.Path);
        Assert.Equal(ResourceDiagnosticCodes.CanvasPixelDecodingUnsupported, diagnostic.Code);
        Assert.Equal(ResourceDiagnosticSources.Parser, diagnostic.Source);
    }

    [Fact]
    public void DiagnosticsFactory_ReturnsStableExportDiagnostic()
    {
        var diagnostic = ResourceInspectionDiagnostics.ExportLuaMultipleBlocks(2, "Script.lua");

        Assert.Equal(ResourceDiagnosticSeverities.Info, diagnostic.Severity);
        Assert.Equal("Exported 2 Lua blocks in stream order.", diagnostic.Message);
        Assert.Equal("Script.lua", diagnostic.Path);
        Assert.Equal(ResourceDiagnosticCodes.ExportLuaMultipleBlocks, diagnostic.Code);
        Assert.Equal(ResourceDiagnosticSources.Export, diagnostic.Source);
    }

    private static string FixturePath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "fixtures", "synthetic", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate fixture '{fileName}'.");
    }

    private static string WriteTemporaryPkg1ImageFile(byte[] imageBytes)
    {
        var directoryData = CreateDirectoryDataForImage("Canvas.img", imageBytes.Length - 4);
        var header = CreateHeader("PKG1", string.Empty, dataSize: directoryData.Length + imageBytes.Length);
        var path = Path.Combine(Path.GetTempPath(), $"wcx-inspect-image-{Guid.NewGuid():N}.wz");
        File.WriteAllBytes(path, [.. header, .. directoryData, .. imageBytes]);
        return path;
    }

    private static byte[] CreateDirectoryDataForImage(string name, int imageSize)
    {
        var bytes = new List<byte> { 0x01, 0x04 };
        AddWzString(bytes, name);
        bytes.Add((byte)imageSize);
        bytes.Add(0x00);
        var hashOffsetPosition = bytes.Count + 16;
        var imageOffset = 16 + bytes.Count + sizeof(uint) + 4;
        var hashOffset = CreateHashOffset(
            hashOffsetPosition: checked((uint)hashOffsetPosition),
            desiredOffset: checked((uint)imageOffset));
        bytes.AddRange(BitConverter.GetBytes(hashOffset));
        return bytes.ToArray();
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

    private static byte[] CreateImageDirectory(string name)
    {
        var bytes = new List<byte> { 0x01, 0x04 };
        AddWzString(bytes, name);
        bytes.Add(0x01);
        bytes.Add(0x00);
        bytes.AddRange(BitConverter.GetBytes(0u));
        return bytes.ToArray();
    }

    private static byte[] CreatePkg1DirectoryPackage(byte[] directoryData)
    {
        byte[] encryptedVersion = [0x7b, 0x00];
        var header = CreateHeader("PKG1", string.Empty, dataSize: encryptedVersion.Length + directoryData.Length);
        return [.. header, .. encryptedVersion, .. directoryData];
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
