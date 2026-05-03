using WzComparerX.Tests;
using WzComparerX.WzLib;

namespace WzComparerX.WzLib.Tests;

public class WzListFileReaderTests
{
    [Fact]
    public void Read_NoOpListReturnsEntriesAndExcludesDummy()
    {
        var bytes = WzListFileFixture.Create(
            WzStringEncryptionKind.None,
            "dummy",
            "Base/Character.wz",
            "Mob/0100000.img");

        var inspection = new WzListFileReader().Read(bytes, "/tmp/List.wz", WzStringEncryptionKind.None);

        Assert.Equal(WzStringEncryptionKind.None, inspection.StringEncryptionKind);
        Assert.Equal(3, inspection.RawEntryCount);
        Assert.Collection(
            inspection.Entries,
            entry =>
            {
                Assert.Equal(1, entry.Index);
                Assert.Equal("Base/Character.wz", entry.Path);
                Assert.Equal("Base/Character.wz".Length, entry.CharacterCount);
                Assert.Equal(16, entry.LengthPosition);
                Assert.Equal(20, entry.DataPosition);
            },
            entry =>
            {
                Assert.Equal(2, entry.Index);
                Assert.Equal("Mob/0100000.img", entry.Path);
                Assert.Equal("Mob/0100000.img".Length, entry.CharacterCount);
            });
    }

    [Fact]
    public void Read_AutoDetectsGmsList()
    {
        var bytes = WzListFileFixture.Create(
            WzStringEncryptionKind.Gms,
            "dummy",
            "Data/String.wz");

        var inspection = new WzListFileReader().Read(bytes, "/tmp/List.wz");

        Assert.Equal(WzStringEncryptionKind.Gms, inspection.StringEncryptionKind);
        var entry = Assert.Single(inspection.Entries);
        Assert.Equal("Data/String.wz", entry.Path);
    }

    [Fact]
    public void Read_AutoDetectsKmsList()
    {
        var bytes = WzListFileFixture.Create(
            WzStringEncryptionKind.Kms,
            "dummy",
            "Data/Map.wz");

        var inspection = new WzListFileReader().Read(bytes, "/tmp/List.wz");

        Assert.Equal(WzStringEncryptionKind.Kms, inspection.StringEncryptionKind);
        var entry = Assert.Single(inspection.Entries);
        Assert.Equal("Data/Map.wz", entry.Path);
    }

    [Fact]
    public void Read_NegativeEntryLengthThrowsInvalidDataException()
    {
        byte[] bytes = [0xff, 0xff, 0xff, 0xff];

        var exception = Assert.Throws<InvalidDataException>(() =>
            new WzListFileReader().Read(bytes, "/tmp/List.wz", WzStringEncryptionKind.None));

        Assert.Contains("character count cannot be negative", exception.Message);
    }
}
