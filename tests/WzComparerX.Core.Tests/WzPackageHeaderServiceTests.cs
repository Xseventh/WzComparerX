using System.Buffers.Binary;
using System.Text;
using WzComparerX.Core;
using WzComparerX.Tests;
using WzComparerX.WzLib;

namespace WzComparerX.Core.Tests;

public class WzPackageHeaderServiceTests
{
    [Fact]
    public async Task ReadAsync_FormatsPkg1HeaderDeterministically()
    {
        var path = WriteTemporaryPkg1File();
        var service = new WzPackageHeaderService();
        var formatter = new WzPackageHeaderFormatter();

        try
        {
            var header = await service.ReadAsync(path);
            var output = formatter.Format(header);

            Assert.Equal(
                $"""
                source: {path}
                format: pkg1
                signature: PKG1
                valid: true
                headerSize: 25
                dataSize: 3
                fileSize: 28
                directoryStartPosition: 27
                copyright: Copyright
                encryptedVersion: 123
                encryptedVersionMissing: false

                """.ReplaceLineEndings(),
                output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadAsync_FormatsPkg1HeaderJsonDeterministically()
    {
        var path = WriteTemporaryPkg1File();
        var service = new WzPackageHeaderService();
        var formatter = new WzPackageHeaderJsonFormatter();

        try
        {
            var header = await service.ReadAsync(path);
            var output = formatter.Format(header);

            Assert.Contains("\"Format\": \"Pkg1\"", output);
            Assert.Contains("\"Signature\": \"PKG1\"", output);
            Assert.Contains("\"HeaderSize\": 25", output);
            Assert.Contains("\"EncryptedVersion\": 123", output);
            Assert.Contains("\"IsEncryptedVersionMissing\": false", output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ScanAsync_ReturnsSortedHeadersForDirectory()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-header-scan-");
        var firstPath = Path.Combine(directory.FullName, "A.wz");
        var secondPath = Path.Combine(directory.FullName, "B.wz");
        File.WriteAllBytes(secondPath, CreatePkg1(copyright: "Copyright", encryptedVersionBytes: [0x7b, 0x00]));
        File.WriteAllBytes(firstPath, CreatePkg1(copyright: "Copyright", encryptedVersionBytes: [0x7b, 0x00]));

        var service = new WzPackageHeaderScanService();

        try
        {
            var headers = await service.ScanAsync(directory.FullName);

            Assert.Equal([firstPath, secondPath], headers.Select(header => header.SourcePath));
            Assert.All(headers, header => Assert.True(header.IsValid));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task ScanFormatJson_ReturnsHeaderCollection()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-header-scan-json-");
        var path = Path.Combine(directory.FullName, "Base.wz");
        File.WriteAllBytes(path, CreatePkg1(copyright: "Copyright", encryptedVersionBytes: [0x7b, 0x00]));

        var service = new WzPackageHeaderScanService();
        var formatter = new WzPackageHeaderScanJsonFormatter();

        try
        {
            var headers = await service.ScanAsync(directory.FullName);
            var output = formatter.Format(headers);

            Assert.Contains("\"Files\": 1", output);
            Assert.Contains("\"Headers\": [", output);
            Assert.Contains("\"SourcePath\":", output);
            Assert.Contains("\"Format\": \"Pkg1\"", output);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectFolder_ProjectsPackagesAsInspectionTree()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-folder-inspection-");
        var path = Path.Combine(directory.FullName, "Base.wz");
        File.WriteAllBytes(path, CreatePkg1(copyright: "Copyright", encryptedVersionBytes: [0x7b, 0x00]));
        var service = new ResourceFolderInspectionService();

        try
        {
            var document = await service.InspectAsync(directory.FullName);

            Assert.Equal("folder", document.Format);
            Assert.Equal("folder", document.Root.Kind);
            Assert.Equal("1 packages", document.Root.DisplayValue);
            Assert.Contains(document.DebugMetadata ?? [], item => item.Name == "packageCount" && (int)item.Value! == 1);

            var package = Assert.Single(document.Root.Children);
            Assert.Equal("Base.wz", package.Name);
            Assert.Equal("package", package.Kind);
            Assert.Equal(path, package.Path);
            Assert.Equal("pkg1", package.DisplayValue);
            Assert.Contains(package.DebugMetadata ?? [], item => item.Name == "valid" && (bool)item.Value!);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectFolder_ProjectsMsAndMnContainersAsPackages()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-folder-ms-inspection-");
        var wzPath = Path.Combine(directory.FullName, "Base.wz");
        var msPath = Path.Combine(directory.FullName, "Skill_00002.ms");
        var mnPath = Path.Combine(directory.FullName, "Quest_00001.mn");
        File.WriteAllBytes(wzPath, CreatePkg1(copyright: "Copyright", encryptedVersionBytes: [0x7b, 0x00]));
        await File.WriteAllBytesAsync(
            msPath,
            MsContainerFixture.CreateV4(
                Path.GetFileName(msPath),
                new MsContainerFixture.Entry("Skill/1000.img", 0, 12, 1024)));
        await File.WriteAllBytesAsync(
            mnPath,
            MsContainerFixture.CreateV4(
                Path.GetFileName(mnPath),
                new MsContainerFixture.Entry("Quest/1000.img", 0, 21, 1024)));
        var service = new ResourceFolderInspectionService();

        try
        {
            var document = await service.InspectAsync(directory.FullName);

            Assert.Equal("folder", document.Format);
            Assert.Equal("3 packages", document.Root.DisplayValue);
            Assert.Contains(document.DebugMetadata ?? [], item => item.Name == "packageCount" && (int)item.Value! == 3);
            Assert.Collection(
                document.Root.Children,
                package =>
                {
                    Assert.Equal("Base.wz", package.Name);
                    Assert.Equal("pkg1", package.DisplayValue);
                },
                package =>
                {
                    Assert.Equal("Quest_00001.mn", package.Name);
                    Assert.Equal("ms", package.DisplayValue);
                    Assert.Equal(mnPath, package.Identity?.PackagePath);
                    Assert.Contains(package.DebugMetadata ?? [], item => item.Name == "version" && Equals(item.Value, 4));
                    Assert.Contains(package.DebugMetadata ?? [], item => item.Name == "entryCount" && Equals(item.Value, 1));
                },
                package =>
                {
                    Assert.Equal("Skill_00002.ms", package.Name);
                    Assert.Equal("ms", package.DisplayValue);
                    Assert.Equal(msPath, package.Identity?.PackagePath);
                    Assert.Contains(package.DebugMetadata ?? [], item => item.Name == "version" && Equals(item.Value, 4));
                    Assert.Contains(package.DebugMetadata ?? [], item => item.Name == "entryCount" && Equals(item.Value, 1));
                });
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectFolder_SkipsListWzHelperFile()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-folder-listwz-inspection-");
        var wzPath = Path.Combine(directory.FullName, "Base.wz");
        var listPath = Path.Combine(directory.FullName, "List.wz");
        File.WriteAllBytes(wzPath, CreatePkg1(copyright: "Copyright", encryptedVersionBytes: [0x7b, 0x00]));
        await File.WriteAllBytesAsync(
            listPath,
            WzListFileFixture.Create(
                WzStringEncryptionKind.None,
                "dummy",
                "Base/Character.wz"));
        var service = new ResourceFolderInspectionService();

        try
        {
            var document = await service.InspectAsync(directory.FullName);

            Assert.Equal("folder", document.Format);
            Assert.Equal("1 packages", document.Root.DisplayValue);
            var package = Assert.Single(document.Root.Children);
            Assert.Equal("Base.wz", package.Name);
            Assert.DoesNotContain(document.Root.Children, child => child.Name == "List.wz");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectDebug_FormatsDirectoryDiagnostics()
    {
        var path = WriteTemporaryPkg1DirectoryFile();
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionFormatter();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                selector: null,
                new ResourceInspectionOptions(WzStringEncryptionKind.None, IncludeDebugMetadata: true));
            var output = formatter.Format(inspection);

            Assert.Contains("entryCount: 1", output);
            Assert.Contains("abc [directory]", output);
            Assert.Contains("nodeType: 0x03", output);
            Assert.Contains("dataSize: 5", output);
            Assert.Contains("checksum: 1", output);
            Assert.Contains("hashOffset: 305419896", output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectDebug_AutoDetectsNoOpStringKey()
    {
        var path = WriteTemporaryPkg1DirectoryFile();
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionFormatter();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                selector: null,
                new ResourceInspectionOptions(StringKey: null, IncludeDebugMetadata: true));
            var output = formatter.Format(inspection);

            Assert.Contains("stringKey: none", output);
            Assert.Contains("abc [directory]", output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteTemporaryPkg1File()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wz");
        File.WriteAllBytes(path, CreatePkg1(copyright: "Copyright", encryptedVersionBytes: [0x7b, 0x00]));
        return path;
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
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wz");
        var header = CreateHeader("PKG1", "Copyright", dataSize: encryptedVersion.Length + directoryData.Length);
        File.WriteAllBytes(path, [.. header, .. encryptedVersion, .. directoryData]);
        return path;
    }

    private static byte[] CreatePkg1(string copyright, byte[] encryptedVersionBytes)
    {
        var header = CreateHeader("PKG1", copyright, dataSize: encryptedVersionBytes.Length + 1);
        return [.. header, .. encryptedVersionBytes, 0x00];
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
