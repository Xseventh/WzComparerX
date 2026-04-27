using System.Buffers.Binary;
using System.Text;
using WzComparerX.Core;
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
    public async Task PreviewDir_FormatsDirectoryPreview()
    {
        var path = WriteTemporaryPkg1DirectoryFile();
        var service = new WzDirectoryPreviewService(
            new WzDirectoryPreviewReader(stringDecryptor: new WzStringDecryptor(WzStringEncryptionKind.Bms)));
        var formatter = new WzDirectoryPreviewFormatter();

        try
        {
            var preview = await service.ReadAsync(path);
            var output = formatter.Format(preview);

            Assert.Contains("entries: 1", output);
            Assert.Contains("0 | directory | name=abc | type=0x03 | size=5 | checksum=1 | hashOffset=305419896", output);
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
            0x03, 0xfd, 0xcb, 0xc9, 0xcf, 0x05, 0x01, 0x78, 0x56, 0x34, 0x12
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
