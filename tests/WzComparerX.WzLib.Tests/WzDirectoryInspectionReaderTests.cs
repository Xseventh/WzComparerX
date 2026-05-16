using System.Buffers.Binary;
using System.Text;
using WzComparerX.Tests;
using WzComparerX.WzLib;

namespace WzComparerX.WzLib.Tests;

public class WzDirectoryInspectionReaderTests
{
    [Fact]
    public void Read_ReturnsPkg1DirectoryEntryInspections()
    {
        var bytes = CreatePkg1WithDirectoryEntries();
        var headerReader = new WzPackageHeaderReader();
        var inspectionReader = new WzDirectoryInspectionReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "Base.wz");

        var inspection = inspectionReader.Read(stream, header);

        Assert.Equal(2, inspection.EntryCount);
        Assert.Equal(WzDirectoryEntryKind.Directory, inspection.Entries[0].Kind);
        Assert.Equal("abc", inspection.Entries[0].Name);
        Assert.Equal(0x03, inspection.Entries[0].NodeType);
        Assert.Equal(5, inspection.Entries[0].DataSize);
        Assert.Equal(1, inspection.Entries[0].Checksum);
        Assert.Equal(0x12345678u, inspection.Entries[0].HashOffset);
        Assert.Equal(WzDirectoryEntryKind.Image, inspection.Entries[1].Kind);
        Assert.Equal("def", inspection.Entries[1].Name);
        Assert.Equal(0x04, inspection.Entries[1].NodeType);
        Assert.Equal(7, inspection.Entries[1].DataSize);
        Assert.Equal(2, inspection.Entries[1].Checksum);
        Assert.Equal(0x90abcdefu, inspection.Entries[1].HashOffset);
    }

    [Fact]
    public void Read_MissingEncryptedVersionComputesPkg1Offsets()
    {
        var bytes = CreatePkg1WithMissingEncryptedVersion();
        var headerReader = new WzPackageHeaderReader();
        var inspectionReader = new WzDirectoryInspectionReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "Base.wz");

        var inspection = inspectionReader.Read(stream, header);

        Assert.True(header.IsEncryptedVersionMissing);
        Assert.Equal(777, inspection.WzVersion);
        Assert.Equal(59192u, inspection.HashVersion);
        Assert.Equal(24, inspection.Entries[0].HashOffsetPosition);
        Assert.Equal(0x22c12300u, inspection.Entries[0].HashOffset);
        Assert.Equal(28, inspection.Entries[0].Offset);
    }

    [Fact]
    public void Read_EncryptedVersionDetectsPkg1VersionAndDirectoryStubOffset()
    {
        var bytes = CreatePkg1WithEncryptedVersionAndDirectoryStubOffset();
        var headerReader = new WzPackageHeaderReader();
        var inspectionReader = new WzDirectoryInspectionReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "Base.wz");

        var inspection = inspectionReader.Read(stream, header);

        Assert.False(header.IsEncryptedVersionMissing);
        Assert.Equal(0x20, header.EncryptedVersion);
        Assert.Equal(777, inspection.WzVersion);
        Assert.Equal(59192u, inspection.HashVersion);
        Assert.Equal(26, inspection.Entries[0].HashOffsetPosition);
        Assert.Equal(0x3176a2c0u, inspection.Entries[0].HashOffset);
        Assert.Equal(30, inspection.Entries[0].Offset);
    }

    [Fact]
    public void Read_NodeType02ReadsReferencedStringName()
    {
        var bytes = CreatePkg1WithReferencedStringName();
        var headerReader = new WzPackageHeaderReader();
        var inspectionReader = new WzDirectoryInspectionReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "String.wz");

        var inspection = inspectionReader.Read(stream, header);

        Assert.Equal(1, inspection.EntryCount);
        Assert.Equal(WzDirectoryEntryKind.Image, inspection.Entries[0].Kind);
        Assert.Equal(0x02, inspection.Entries[0].NodeType);
        Assert.Equal("ref", inspection.Entries[0].Name);
        Assert.Equal(26, inspection.Entries[0].HashOffsetPosition);
    }

    [Fact]
    public void Read_NegativeDirectoryEntryCountThrowsInvalidDataException()
    {
        var bytes = CreatePkg1WithNegativeDirectoryEntryCount();
        var headerReader = new WzPackageHeaderReader();
        var inspectionReader = new WzDirectoryInspectionReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "Broken.wz");

        var exception = Assert.Throws<InvalidDataException>(() => inspectionReader.Read(stream, header));

        Assert.Contains("entry count cannot be negative", exception.Message);
    }

    [Fact]
    public void Read_NodeType02ReferenceBeyondStreamThrowsInvalidDataException()
    {
        var bytes = CreatePkg1WithInvalidReferencedStringName();
        var headerReader = new WzPackageHeaderReader();
        var inspectionReader = new WzDirectoryInspectionReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "Broken.wz");

        var exception = Assert.Throws<InvalidDataException>(() => inspectionReader.Read(stream, header));

        Assert.Contains("beyond the end of the stream", exception.Message);
    }

    [Fact]
    public void Read_ExpandedDirectoryEntryCountReturnsLargePkg1Table()
    {
        var bytes = CreatePkg1WithExpandedDirectoryEntryCount(130);
        var headerReader = new WzPackageHeaderReader();
        var inspectionReader = new WzDirectoryInspectionReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "Large.wz");

        var inspection = inspectionReader.Read(stream, header);

        Assert.Equal(130, inspection.EntryCount);
        Assert.Equal(130, inspection.Entries.Count);
        Assert.All(inspection.Entries, entry => Assert.Equal(WzDirectoryEntryKind.Image, entry.Kind));
        Assert.Equal(0, inspection.Entries[0].Index);
        Assert.Equal("entry000.img", inspection.Entries[0].Name);
        Assert.Equal("entry000.img", inspection.Entries[0].Path);
        Assert.Equal(129, inspection.Entries[^1].Index);
        Assert.Equal("entry129.img", inspection.Entries[^1].Name);
        Assert.Equal("entry129.img", inspection.Entries[^1].Path);
    }

    [Fact]
    public void Read_InvalidPkg1HashOffsetsLeaveEncryptedVersionUndetected()
    {
        var bytes = CreatePkg1WithInvalidEncryptedVersionHashOffset();
        var headerReader = new WzPackageHeaderReader();
        var inspectionReader = new WzDirectoryInspectionReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "Broken.wz");

        var inspection = inspectionReader.Read(stream, header);

        Assert.Equal(1, inspection.EntryCount);
        Assert.Null(inspection.WzVersion);
        Assert.Null(inspection.HashVersion);
        Assert.Null(inspection.Entries[0].Offset);
    }

    [Fact]
    public void Read_ReturnsRecursiveDirectoryEntries()
    {
        var bytes = CreatePkg1WithNestedDirectoryEntries();
        var headerReader = new WzPackageHeaderReader();
        var inspectionReader = new WzDirectoryInspectionReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "Nested.wz");

        var inspection = inspectionReader.Read(stream, header);

        Assert.Equal(1, inspection.EntryCount);
        Assert.Equal(2, inspection.Entries.Count);
        Assert.Equal("dir", inspection.Entries[0].Name);
        Assert.Equal("dir", inspection.Entries[0].Path);
        Assert.Equal(0, inspection.Entries[0].Depth);
        Assert.Equal(WzDirectoryEntryKind.Directory, inspection.Entries[0].Kind);
        Assert.Equal("leaf.img", inspection.Entries[1].Name);
        Assert.Equal("dir/leaf.img", inspection.Entries[1].Path);
        Assert.Equal(1, inspection.Entries[1].Depth);
        Assert.Equal(WzDirectoryEntryKind.Image, inspection.Entries[1].Kind);
    }

    [Fact]
    public void Read_ReturnsPkg2Kmst1200DirectoryEntries()
    {
        var bytes = Pkg2PackageFixture.CreateKmst1200(
            new Pkg2PackageFixture.Entry(
                "ItemOption.img",
                Pkg2PackageFixture.CreateTextImage(("name", "item"))),
            new Pkg2PackageFixture.Entry(
                "SkillOption.img",
                Pkg2PackageFixture.CreateTextImage(("reqLevel", "12"))));
        var headerReader = new WzPackageHeaderReader();
        var inspectionReader = new WzDirectoryInspectionReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "Item_000.wz");

        var inspection = inspectionReader.Read(stream, header);

        Assert.Equal(WzPackageFormat.Pkg2, inspection.Header.Format);
        Assert.Equal(2, inspection.EntryCount);
        Assert.Equal(2, inspection.Entries.Count);
        Assert.Equal(Pkg2PackageFixture.WzVersion, inspection.WzVersion);
        Assert.Equal(Pkg2PackageFixture.HashVersion, inspection.HashVersion);
        Assert.Equal("pkg2_kmst1200", inspection.FormatProfile);
        Assert.Equal("ItemOption.img", inspection.Entries[0].Name);
        Assert.Equal("SkillOption.img", inspection.Entries[1].Name);
        Assert.Equal(WzDirectoryEntryKind.Image, inspection.Entries[0].Kind);
        Assert.Equal(0x04, inspection.Entries[0].NodeType);
        Assert.True(inspection.Entries[0].Offset > inspection.Entries[0].HashOffsetPosition);
        Assert.True(inspection.Entries[1].Offset > inspection.Entries[1].HashOffsetPosition);
    }

    [Fact]
    public void Read_ReturnsModernKmsPkg2DirectoryEntries()
    {
        var bytes = Pkg2PackageFixture.CreateModernKms(
            new Pkg2PackageFixture.Entry(
                "ItemOption.img",
                Pkg2PackageFixture.CreateTextImage(("name", "item"))),
            new Pkg2PackageFixture.Entry(
                "SkillOption.img",
                Pkg2PackageFixture.CreateTextImage(("reqLevel", "12"))));
        var headerReader = new WzPackageHeaderReader();
        var inspectionReader = new WzDirectoryInspectionReader(
            headerReader,
            new WzStringDecryptor(WzStringEncryptionKind.None));
        using var stream = new MemoryStream(bytes);
        var header = headerReader.Read(stream, "Item_000.wz");

        var inspection = inspectionReader.Read(stream, header);

        Assert.Equal(WzPackageFormat.Pkg2, inspection.Header.Format);
        Assert.True(inspection.Header.IsModernPkg2Header);
        Assert.Equal(2, inspection.EntryCount);
        Assert.Equal(2, inspection.Entries.Count);
        Assert.Null(inspection.WzVersion);
        Assert.Equal(Pkg2PackageFixture.HashVersion, inspection.HashVersion);
        Assert.Equal("pkg2_modern_kms", inspection.FormatProfile);
        Assert.Equal("ItemOption.img", inspection.Entries[0].Name);
        Assert.Equal("SkillOption.img", inspection.Entries[1].Name);
        Assert.Equal(WzDirectoryEntryKind.Image, inspection.Entries[0].Kind);
        Assert.Equal(0x04, inspection.Entries[0].NodeType);
        Assert.True(inspection.Entries[0].Offset > inspection.Entries[0].HashOffsetPosition);
        Assert.True(inspection.Entries[1].Offset > inspection.Entries[1].HashOffsetPosition);
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

    private static byte[] CreatePkg1WithNegativeDirectoryEntryCount()
    {
        byte[] encryptedVersion = [0x7b, 0x00];
        byte[] directoryData = [0xff];
        var header = CreateHeader("PKG1", string.Empty, dataSize: encryptedVersion.Length + directoryData.Length);
        return [.. header, .. encryptedVersion, .. directoryData];
    }

    private static byte[] CreatePkg1WithInvalidReferencedStringName()
    {
        byte[] encryptedVersion = [0x7b, 0x00];
        byte[] directoryData =
        [
            0x01,
            0x02, 0x40, 0x00, 0x00, 0x00
        ];
        var header = CreateHeader("PKG1", string.Empty, dataSize: encryptedVersion.Length + directoryData.Length);
        return [.. header, .. encryptedVersion, .. directoryData];
    }

    private static byte[] CreatePkg1WithExpandedDirectoryEntryCount(int entryCount)
    {
        using var directoryData = new MemoryStream();
        WriteCompressedInt32(directoryData, entryCount);
        for (var i = 0; i < entryCount; i++)
        {
            directoryData.WriteByte(0x04);
            WriteWzAsciiString(directoryData, $"entry{i:000}.img");
            WriteCompressedInt32(directoryData, 0);
            WriteCompressedInt32(directoryData, 0);
            WriteUInt32LittleEndian(directoryData, 0);
        }

        var directoryBytes = directoryData.ToArray();
        var header = CreateHeader("PKG1", string.Empty, dataSize: directoryBytes.Length);
        return [.. header, .. directoryBytes];
    }

    private static byte[] CreatePkg1WithInvalidEncryptedVersionHashOffset()
    {
        byte[] encryptedVersion = [0x20, 0x00];
        using var directoryData = new MemoryStream();
        WriteCompressedInt32(directoryData, 1);
        directoryData.WriteByte(0x04);
        WriteWzAsciiString(directoryData, "bad.img");
        WriteCompressedInt32(directoryData, 64);
        WriteCompressedInt32(directoryData, 0);
        WriteUInt32LittleEndian(directoryData, 0);

        var directoryBytes = directoryData.ToArray();
        var header = CreateHeader("PKG1", string.Empty, dataSize: encryptedVersion.Length + directoryBytes.Length);
        return [.. header, .. encryptedVersion, .. directoryBytes];
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

    private static void WriteCompressedInt32(Stream stream, int value)
    {
        if (value is > sbyte.MinValue and <= sbyte.MaxValue)
        {
            stream.WriteByte(unchecked((byte)(sbyte)value));
            return;
        }

        stream.WriteByte(unchecked((byte)sbyte.MinValue));
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void WriteUInt32LittleEndian(Stream stream, uint value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void WriteWzAsciiString(Stream stream, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        if (bytes.Length <= sbyte.MaxValue)
        {
            stream.WriteByte(unchecked((byte)(sbyte)-bytes.Length));
        }
        else
        {
            stream.WriteByte(unchecked((byte)sbyte.MinValue));
            Span<byte> lengthBytes = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(lengthBytes, bytes.Length);
            stream.Write(lengthBytes);
        }

        for (var i = 0; i < bytes.Length; i++)
        {
            stream.WriteByte((byte)(bytes[i] ^ (byte)(0xAA + i)));
        }
    }
}
