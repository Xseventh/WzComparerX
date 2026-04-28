using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using WzComparerX.Cli;
using WzComparerX.WzLib;

namespace WzComparerX.Core.Tests;

public class CliApplicationTests
{
    [Fact]
    public async Task InspectSyntheticText_MatchesGoldenOutput()
    {
        var fixture = FixturePath("basic-tree.json");

        var result = await RunCliAsync("inspect", fixture);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.Error);
        Assert.Equal(
            """
            source: <fixture>
            format: synthetic
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
            NormalizePath(result.Output, fixture, "<fixture>"));
    }

    [Fact]
    public async Task InspectSyntheticDebugJson_MatchesGoldenOutput()
    {
        var fixture = FixturePath("basic-tree.json");

        var result = await RunCliAsync("inspect", "--debug", "--json", fixture);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.Error);
        Assert.Equal(
            """
            {
              "SourcePath": "<fixture>",
              "Format": "synthetic",
              "Root": {
                "Name": "basic-tree",
                "Kind": "directory",
                "Path": "basic-tree",
                "Children": [
                  {
                    "Name": "Character.wz",
                    "Kind": "directory",
                    "Path": "basic-tree/Character.wz",
                    "Children": [
                      {
                        "Name": "Cap",
                        "Kind": "directory",
                        "Path": "basic-tree/Character.wz/Cap",
                        "Children": [
                          {
                            "Name": "00002000.img",
                            "Kind": "image",
                            "Path": "basic-tree/Character.wz/Cap/00002000.img",
                            "Children": [
                              {
                                "Name": "info",
                                "Kind": "property",
                                "Path": "basic-tree/Character.wz/Cap/00002000.img/info",
                                "Children": [
                                  {
                                    "Name": "name",
                                    "Kind": "value",
                                    "Path": "basic-tree/Character.wz/Cap/00002000.img/info/name",
                                    "DisplayValue": "string = \u0022Beginner Cap\u0022",
                                    "Children": []
                                  },
                                  {
                                    "Name": "reqLevel",
                                    "Kind": "value",
                                    "Path": "basic-tree/Character.wz/Cap/00002000.img/info/reqLevel",
                                    "DisplayValue": "int32 = 0",
                                    "Children": []
                                  }
                                ]
                              }
                            ]
                          }
                        ]
                      }
                    ]
                  },
                  {
                    "Name": "String.wz",
                    "Kind": "directory",
                    "Path": "basic-tree/String.wz",
                    "Children": [
                      {
                        "Name": "Eqp.img",
                        "Kind": "image",
                        "Path": "basic-tree/String.wz/Eqp.img",
                        "Children": [
                          {
                            "Name": "Eqp",
                            "Kind": "property",
                            "Path": "basic-tree/String.wz/Eqp.img/Eqp",
                            "Children": []
                          }
                        ]
                      }
                    ]
                  }
                ]
              },
              "DebugMetadata": [
                {
                  "Name": "sourceKind",
                  "Value": "synthetic"
                }
              ]
            }

            """.ReplaceLineEndings(),
            NormalizePath(result.Output, fixture, "<fixture>"));
    }

    [Fact]
    public async Task InspectDebugDirectory_EmitsCliDiagnostics()
    {
        var path = WriteTemporaryPkg1DirectoryFile();

        try
        {
            var result = await RunCliAsync("inspect", "--debug", "--key", "none", path);
            var output = NormalizePath(result.Output, path, "<wz>");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Error);
            Assert.Contains("source: <wz>", output);
            Assert.Contains("format: pkg1", output);
            Assert.Contains("entryCount: 1", output);
            Assert.Contains("totalEntryCount: 1", output);
            Assert.Contains("stringKey: none", output);
            Assert.Contains("<wz> [package] : pkg1", output);
            Assert.Contains("abc [directory]", output);
            Assert.Contains("nodeType: 0x03", output);
            Assert.Contains("dataSize: 5", output);
            Assert.Contains("checksum: 1", output);
            Assert.Contains("hashOffsetPosition: 35", output);
            Assert.Contains("hashOffset: 305419896", output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectNormalDirectory_OmitsDebugDiagnostics()
    {
        var path = WriteTemporaryPkg1DirectoryFile();

        try
        {
            var result = await RunCliAsync("inspect", "--key", "none", path);
            var output = NormalizePath(result.Output, path, "<wz>");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Error);
            Assert.Contains("<wz> [package] : pkg1", output);
            Assert.DoesNotContain("debug:", output);
            Assert.DoesNotContain("nodeType:", output);
            Assert.DoesNotContain("hashOffset:", output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectDebugImage_EmitsPayloadDiagnostics()
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

        try
        {
            var result = await RunCliAsync("inspect", "--debug", "--key", "none", "--depth", "2", path, "Canvas.img");
            var output = NormalizePath(result.Output, path, "<wz>");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Error);
            Assert.Contains("source: <wz>", output);
            Assert.Contains("selector: Canvas.img", output);
            Assert.Contains("stringKey: none", output);
            Assert.Contains("entryName: Canvas.img", output);
            Assert.Contains("entryDataSize: 49", output);
            Assert.Contains("entryOffset: 39", output);
            Assert.Contains("objectType: Property", output);
            Assert.Contains("propertyCount: 1", output);
            Assert.Contains("icon [canvas]", output);
            Assert.Contains("type: 0x09", output);
            Assert.Contains("valueType: canvas", output);
            Assert.Contains("width: 16", output);
            Assert.Contains("height: 8", output);
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
    public async Task InspectDebugImage_DepthLimitKeepsMiniPropertyMetadataCompact()
    {
        var imageBytes = CreatePropertyImage(CreateObjectProperty(
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
                0x00,
                0x78,
                0x9c)));
        var path = WriteTemporaryPkg1ImageFile(imageBytes);

        try
        {
            var result = await RunCliAsync("inspect", "--debug", "--key", "none", "--depth", "1", path, "Canvas.img");
            var output = NormalizePath(result.Output, path, "<wz>");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Error);
            Assert.Contains("icon [canvas]", output);
            Assert.Contains("childCount: 1", output);
            Assert.Contains("dataLength: 3", output);
            Assert.Contains("compressionKind: Zlib", output);
            Assert.DoesNotContain("kind [int32]", output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectDebugImage_DepthLimitJsonKeepsMiniPropertyMetadataCompact()
    {
        var imageBytes = CreatePropertyImage(CreateObjectProperty(
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
                0x00,
                0x78,
                0x9c)));
        var path = WriteTemporaryPkg1ImageFile(imageBytes);

        try
        {
            var result = await RunCliAsync("inspect", "--debug", "--json", "--key", "none", "--depth", "1", path, "Canvas.img");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Error);
            using var json = JsonDocument.Parse(NormalizePath(result.Output, path, "<wz>"));
            var root = json.RootElement.GetProperty("Root");
            var icon = Assert.Single(root.GetProperty("Children").EnumerateArray());

            Assert.Equal("Canvas.img", root.GetProperty("Name").GetString());
            Assert.Equal("image", root.GetProperty("Kind").GetString());
            Assert.Equal("Property", root.GetProperty("DisplayValue").GetString());
            Assert.Equal("icon", icon.GetProperty("Name").GetString());
            Assert.Equal("canvas", icon.GetProperty("Kind").GetString());
            Assert.Empty(icon.GetProperty("Children").EnumerateArray());
            Assert.Equal(1, GetMetadataInt32(icon, "childCount"));
            Assert.Equal(3, GetMetadataInt32(icon, "dataLength"));
            Assert.Equal("Zlib", GetMetadataString(icon, "compressionKind"));
            var diagnostic = Assert.Single(icon.GetProperty("Diagnostics").EnumerateArray());
            Assert.Equal("wcx.payload.canvas.pixelsUnsupported", diagnostic.GetProperty("Code").GetString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task PreviewCommands_AreNotAccepted()
    {
        var fixture = FixturePath("basic-tree.json");

        var result = await RunCliAsync("preview-dir", fixture);

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("wcx inspect", result.Error);
        Assert.DoesNotContain("preview-dir", result.Error);
        Assert.DoesNotContain("preview-img", result.Error);
    }

    [Fact]
    public async Task ExportMetadataSynthetic_MatchesInspectDebugJson()
    {
        var fixture = FixturePath("basic-tree.json");

        var inspect = await RunCliAsync("inspect", "--debug", "--json", fixture);
        var export = await RunCliAsync("export", "--type", "metadata", fixture);

        Assert.Equal(0, export.ExitCode);
        Assert.Equal(string.Empty, export.Error);
        Assert.Equal(
            NormalizePath(inspect.Output, fixture, "<fixture>"),
            NormalizePath(export.Output, fixture, "<fixture>"));
    }

    [Fact]
    public async Task ExportTextImage_ReturnsOriginalTextImgStream()
    {
        const string text = "#Property\nname=hello\ncount=2\n";
        var path = WriteTemporaryPkg1ImageFile("Text.img", CreateTextImage(text));

        try
        {
            var result = await RunCliAsync("export", "--type", "text", "--key", "none", path, "Text.img");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Error);
            Assert.Equal(text, result.Output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExportLuaImage_ReturnsFullScript()
    {
        const string script = "return 42\nprint(\"ok\")\n";
        var path = WriteTemporaryPkg1ImageFile("Script.lua", CreateLuaImage(script));

        try
        {
            var result = await RunCliAsync("export", "--type", "lua", "--key", "none", path, "Script.lua");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Error);
            Assert.Equal(script, result.Output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExportLuaImage_ConcatenatesMultipleBlocksWithoutSeparator()
    {
        var path = WriteTemporaryPkg1ImageFile("Script.lua", CreateLuaImage("return ", "42\n"));

        try
        {
            var result = await RunCliAsync("export", "--type", "lua", "--key", "none", path, "Script.lua");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal("return 42\n", result.Output);
            Assert.Contains("info [wcx.export.lua.multipleBlocks]: Exported 2 Lua blocks in stream order. (Script.lua)", result.Error);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExportWithOut_WritesContentToFileAndKeepsStdoutEmpty()
    {
        const string text = "#Property\nname=hello\n";
        var path = WriteTemporaryPkg1ImageFile("Text.img", CreateTextImage(text));
        var outputPath = Path.Combine(Path.GetTempPath(), $"wcx-export-{Guid.NewGuid():N}.txt");

        try
        {
            var result = await RunCliAsync("export", "--type", "text", "--out", outputPath, "--key", "none", path, "Text.img");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Output);
            Assert.Equal(string.Empty, result.Error);
            Assert.Equal(text, await File.ReadAllTextAsync(outputPath));
        }
        finally
        {
            File.Delete(path);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportUnsupportedType_ReturnsStructuredDiagnostic()
    {
        const string text = "#Property\nname=hello\n";
        var path = WriteTemporaryPkg1ImageFile("Text.img", CreateTextImage(text));

        try
        {
            var result = await RunCliAsync("export", "--type", "lua", "--key", "none", path, "Text.img");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.Output);
            Assert.Contains("error [wcx.export.unsupported]: Selected image is not a supported Lua IMG: Text.img. (Text.img)", result.Error);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task<CliResult> RunCliAsync(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = await CliApplication.RunAsync(args, output, error);
        return new CliResult(exitCode, output.ToString(), error.ToString());
    }

    private static string NormalizePath(string value, string path, string replacement)
    {
        return value
            .Replace(Path.GetFullPath(path), replacement, StringComparison.Ordinal)
            .Replace(Path.GetFileName(path), replacement, StringComparison.Ordinal)
            .ReplaceLineEndings();
    }

    private static int GetMetadataInt32(JsonElement node, string name)
    {
        return GetMetadataValue(node, name).GetInt32();
    }

    private static string? GetMetadataString(JsonElement node, string name)
    {
        return GetMetadataValue(node, name).GetString();
    }

    private static JsonElement GetMetadataValue(JsonElement node, string name)
    {
        foreach (var metadata in node.GetProperty("DebugMetadata").EnumerateArray())
        {
            if (metadata.GetProperty("Name").GetString() == name)
            {
                return metadata.GetProperty("Value");
            }
        }

        throw new InvalidOperationException($"Metadata value not found: {name}.");
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

    private static string WriteTemporaryPkg1DirectoryFile()
    {
        byte[] directoryData =
        [
            0x01,
            0x03, 0xfd, 0xcb, 0xc9, 0xcf, 0x05, 0x01, 0x78, 0x56, 0x34, 0x12,
            0x00
        ];
        byte[] encryptedVersion = [0x7b, 0x00];
        var path = Path.Combine(Path.GetTempPath(), $"wcx-cli-directory-{Guid.NewGuid():N}.wz");
        var header = CreateHeader("PKG1", "Copyright", dataSize: encryptedVersion.Length + directoryData.Length);
        File.WriteAllBytes(path, [.. header, .. encryptedVersion, .. directoryData]);
        return path;
    }

    private static string WriteTemporaryPkg1ImageFile(byte[] imageBytes)
    {
        return WriteTemporaryPkg1ImageFile("Canvas.img", imageBytes);
    }

    private static string WriteTemporaryPkg1ImageFile(string imageName, byte[] imageBytes)
    {
        var directoryData = CreateDirectoryDataForImage(imageName, imageBytes.Length - 4);
        var header = CreateHeader("PKG1", string.Empty, dataSize: directoryData.Length + imageBytes.Length);
        var path = Path.Combine(Path.GetTempPath(), $"wcx-cli-image-{Guid.NewGuid():N}.wz");
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

    private static byte[] CreateTextImage(string text)
    {
        return [0x00, 0x00, 0x00, 0x00, .. Encoding.UTF8.GetBytes(text)];
    }

    private static byte[] CreateLuaImage(params string[] scripts)
    {
        var bytes = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
        foreach (var script in scripts)
        {
            var payload = Encoding.UTF8.GetBytes(script);
            if (payload.Length > sbyte.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(scripts), "Test Lua payloads must fit in one compressed-int byte.");
            }

            bytes.Add(0x01);
            bytes.Add((byte)payload.Length);
            bytes.AddRange(payload);
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

    private sealed record CliResult(int ExitCode, string Output, string Error);
}
