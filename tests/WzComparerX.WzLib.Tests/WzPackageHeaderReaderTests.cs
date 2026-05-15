using System.Buffers.Binary;
using System.Text;
using WzComparerX.Tests;
using WzComparerX.WzLib;

namespace WzComparerX.WzLib.Tests;

public class WzPackageHeaderReaderTests
{
    [Fact]
    public void Read_Pkg1HeaderWithEncryptedVersion_ReturnsDirectoryStartAfterEncver()
    {
        var bytes = CreatePkg1(copyright: "Copyright", encryptedVersionBytes: [0x7b, 0x00]);
        var reader = new WzPackageHeaderReader();

        var header = reader.Read(new MemoryStream(bytes), "String.wz");

        Assert.True(header.IsValid);
        Assert.Equal(WzPackageFormat.Pkg1, header.Format);
        Assert.Equal("PKG1", header.Signature);
        Assert.Equal("Copyright", header.Copyright);
        Assert.Equal(0x7b, header.EncryptedVersion);
        Assert.False(header.IsEncryptedVersionMissing);
        Assert.Equal(header.HeaderSize + 2, header.DirectoryStartPosition);
    }

    [Fact]
    public void Read_Pkg1HeaderWithMissingEncryptedVersion_ReturnsDirectoryStartAtHeaderSize()
    {
        var bytes = CreatePkg1(copyright: "Copyright", encryptedVersionBytes: [0x01, 0x02]);
        var reader = new WzPackageHeaderReader();

        var header = reader.Read(new MemoryStream(bytes), "Character.wz");

        Assert.True(header.IsValid);
        Assert.Equal(WzPackageFormat.Pkg1, header.Format);
        Assert.Equal(0x0201, header.EncryptedVersion);
        Assert.True(header.IsEncryptedVersionMissing);
        Assert.Equal(header.HeaderSize, header.DirectoryStartPosition);
    }

    [Fact]
    public void Read_Pkg2Header_ReturnsHeaderHashes()
    {
        var bytes = CreatePkg2(copyright: "Copyright", hash1: 0x11223344, hash2: 0xaabbccdd);
        var reader = new WzPackageHeaderReader();

        var header = reader.Read(new MemoryStream(bytes), "Data.wz");

        Assert.True(header.IsValid);
        Assert.Equal(WzPackageFormat.Pkg2, header.Format);
        Assert.Equal("PKG2", header.Signature);
        Assert.Equal(0x11223344u, header.Hash1);
        Assert.Equal(0xaabbccddu, header.Hash2);
        Assert.Equal(header.HeaderSize + 8, header.DirectoryStartPosition);
    }

    [Fact]
    public void Read_ModernPkg2Header_ReturnsGatheredHashes()
    {
        var bytes = Pkg2PackageFixture.CreateModernKms(
            new Pkg2PackageFixture.Entry(
                "ItemOption.img",
                Pkg2PackageFixture.CreateTextImage(("name", "item"))));
        var reader = new WzPackageHeaderReader();

        var header = reader.Read(new MemoryStream(bytes), "Item_000.wz");

        Assert.True(header.IsValid);
        Assert.Equal(WzPackageFormat.Pkg2, header.Format);
        Assert.Equal("PKG2", header.Signature);
        Assert.True(header.IsModernPkg2Header);
        Assert.Equal(Pkg2PackageFixture.ModernHeaderSize, header.HeaderSize);
        Assert.Equal(Pkg2PackageFixture.ModernHeaderSize, header.DirectoryStartPosition);
        Assert.Equal(bytes.Length - Pkg2PackageFixture.ModernHeaderSize, header.DataSize);
        Assert.Equal(Pkg2PackageFixture.ModernHash1, header.Hash1);
        Assert.Equal(Pkg2PackageFixture.ModernHash2, header.Hash2);
    }

    [Fact]
    public void Read_InvalidSignature_ReturnsInvalidHeader()
    {
        var reader = new WzPackageHeaderReader();

        var header = reader.Read(new MemoryStream("NOPE"u8.ToArray()), "bad.wz");

        Assert.False(header.IsValid);
        Assert.Equal(WzPackageFormat.Unknown, header.Format);
        Assert.Equal("NOPE", header.Signature);
    }

    private static byte[] CreatePkg1(string copyright, byte[] encryptedVersionBytes)
    {
        var header = CreateHeader("PKG1", copyright, dataSize: encryptedVersionBytes.Length + 1);
        return [.. header, .. encryptedVersionBytes, 0x00];
    }

    private static byte[] CreatePkg2(string copyright, uint hash1, uint hash2)
    {
        var header = CreateHeader("PKG2", copyright, dataSize: 1);
        var hashBytes = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(hashBytes.AsSpan(0, 4), hash1);
        BinaryPrimitives.WriteUInt32LittleEndian(hashBytes.AsSpan(4, 4), hash2);
        return [.. header, .. hashBytes, 0x00];
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
