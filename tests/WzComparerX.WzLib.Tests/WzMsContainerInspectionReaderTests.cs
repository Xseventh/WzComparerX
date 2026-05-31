using WzComparerX.Tests;
using WzComparerX.WzLib;

namespace WzComparerX.WzLib.Tests;

public class WzMsContainerInspectionReaderTests
{
    [Fact]
    public void Read_Version2ContainerReturnsHeaderAndEntries()
    {
        var bytes = MsContainerFixture.CreateV2(
            "Skill_00002.ms",
            new MsContainerFixture.Entry("Skill/15500.img", 0, 12, 1024, Flags: 6, Unknown1: 1),
            new MsContainerFixture.Entry("Skill/15510.img", 1, 34, 1024, Flags: 6, Unknown2: 2));
        using var stream = new MemoryStream(bytes);

        var inspection = new WzMsContainerInspectionReader()
            .Read(stream, "/tmp/Skill_00002.ms");

        Assert.Equal(2, inspection.Header.Version);
        Assert.Equal(WzMsContainerEncryptionKind.Snow, inspection.Header.EncryptionKind);
        Assert.Equal(2, inspection.Header.EntryCount);
        Assert.True(inspection.Header.EntryStartPosition > inspection.Header.HeaderStartPosition);
        Assert.True(inspection.Header.DataStartPosition % 1024 == 0);
        Assert.Equal(bytes.Length, inspection.Header.FileSize);

        Assert.Collection(
            inspection.Entries,
            entry =>
            {
                Assert.Equal(0, entry.Index);
                Assert.Equal("15500.img", entry.Name);
                Assert.Equal("Skill/15500.img", entry.Path);
                Assert.Equal(100, entry.Checksum);
                Assert.Equal(6, entry.Flags);
                Assert.Equal(inspection.Header.DataStartPosition, entry.Offset);
                Assert.Equal(12, entry.Size);
                Assert.Equal(1, entry.Unknown1);
                Assert.Equal(0, entry.Unknown3);
                Assert.Equal(16, entry.Key.Count);
            },
            entry =>
            {
                Assert.Equal(1, entry.Index);
                Assert.Equal("15510.img", entry.Name);
                Assert.Equal("Skill/15510.img", entry.Path);
                Assert.Equal(101, entry.Checksum);
                Assert.Equal(6, entry.Flags);
                Assert.Equal(inspection.Header.DataStartPosition + 1024, entry.Offset);
                Assert.Equal(34, entry.Size);
                Assert.Equal(2, entry.Unknown2);
                Assert.Equal(0, entry.Unknown4);
            });
    }

    [Fact]
    public void Read_Version4ContainerReturnsHeaderAndEntries()
    {
        var bytes = MsContainerFixture.CreateV4(
            "Skill_00002.ms",
            new MsContainerFixture.Entry("Skill/1000.img", 0, 12, 1024, Flags: 7, Unknown1: 1, Unknown3: 3),
            new MsContainerFixture.Entry("Skill/2000.img", 1, 34, 1024, Flags: 8, Unknown2: 2, Unknown4: 4));
        using var stream = new MemoryStream(bytes);

        var inspection = new WzMsContainerInspectionReader()
            .Read(stream, "/tmp/Skill_00002.ms");

        Assert.Equal(4, inspection.Header.Version);
        Assert.Equal(WzMsContainerEncryptionKind.ChaCha20, inspection.Header.EncryptionKind);
        Assert.Equal(2, inspection.Header.EntryCount);
        Assert.Equal(0x12345678, inspection.Header.HeaderHash);
        Assert.True(inspection.Header.EntryStartPosition > inspection.Header.HeaderStartPosition);
        Assert.True(inspection.Header.DataStartPosition % 1024 == 0);
        Assert.Equal(bytes.Length, inspection.Header.FileSize);

        Assert.Collection(
            inspection.Entries,
            entry =>
            {
                Assert.Equal(0, entry.Index);
                Assert.Equal("1000.img", entry.Name);
                Assert.Equal("Skill/1000.img", entry.Path);
                Assert.Equal(100, entry.Checksum);
                Assert.Equal(7, entry.Flags);
                Assert.Equal(0, entry.RelativeBlock);
                Assert.Equal(inspection.Header.DataStartPosition, entry.Offset);
                Assert.Equal(12, entry.Size);
                Assert.Equal(1024, entry.SizeAligned);
                Assert.Equal(1, entry.Unknown1);
                Assert.Equal(3, entry.Unknown3);
                Assert.Equal(16, entry.Key.Count);
            },
            entry =>
            {
                Assert.Equal(1, entry.Index);
                Assert.Equal("2000.img", entry.Name);
                Assert.Equal("Skill/2000.img", entry.Path);
                Assert.Equal(101, entry.Checksum);
                Assert.Equal(8, entry.Flags);
                Assert.Equal(1, entry.RelativeBlock);
                Assert.Equal(inspection.Header.DataStartPosition + 1024, entry.Offset);
                Assert.Equal(34, entry.Size);
                Assert.Equal(1024, entry.SizeAligned);
                Assert.Equal(2, entry.Unknown2);
                Assert.Equal(4, entry.Unknown4);
            });
    }

    [Fact]
    public void Read_Version4ContainerHandlesEntryTableAcrossChaChaBlocks()
    {
        var bytes = MsContainerFixture.CreateV4(
            "Mob_00000.ms",
            new MsContainerFixture.Entry("Mob/0100000.img", 0, 207, 256, Flags: 6),
            new MsContainerFixture.Entry("Mob/0100001.img", 1, 207, 256, Flags: 6),
            new MsContainerFixture.Entry("Mob/BossPattern/BossSuu.img", 2, 512, 1024, Flags: 6));
        using var stream = new MemoryStream(bytes);

        var inspection = new WzMsContainerInspectionReader()
            .Read(stream, "/tmp/Mob_00000.ms");

        Assert.Equal(4, inspection.Header.Version);
        Assert.Equal(3, inspection.Header.EntryCount);
        Assert.Equal("Mob/0100000.img", inspection.Entries[0].Path);
        Assert.Equal("Mob/0100001.img", inspection.Entries[1].Path);
        Assert.Equal("Mob/BossPattern/BossSuu.img", inspection.Entries[2].Path);
        Assert.Equal(inspection.Header.DataStartPosition + 1024, inspection.Entries[1].Offset);
    }

    [Fact]
    public void Read_Version4MnContainerReturnsHeaderAndEntries()
    {
        var bytes = MsContainerFixture.CreateV4(
            "Quest_00001.mn",
            new MsContainerFixture.Entry("Quest/1000.img", 0, 21, 1024, Flags: 4));
        using var stream = new MemoryStream(bytes);

        var inspection = new WzMsContainerInspectionReader()
            .Read(stream, "/tmp/Quest_00001.mn");

        Assert.Equal(4, inspection.Header.Version);
        Assert.Equal(1, inspection.Header.EntryCount);
        var entry = Assert.Single(inspection.Entries);
        Assert.Equal("1000.img", entry.Name);
        Assert.Equal("Quest/1000.img", entry.Path);
        Assert.Equal(21, entry.Size);
    }

    [Fact]
    public void Read_NonVersion4ContainerThrowsNotSupportedException()
    {
        var bytes = MsContainerFixture.CreateV4(
            "Skill_00002.ms",
            new MsContainerFixture.Entry("Skill/1000.img", 0, 12, 1024));
        bytes[CalculateRandomByteCount("skill_00002.ms")] = 0x01;
        using var stream = new MemoryStream(bytes);

        Assert.Throws<NotSupportedException>(() =>
            new WzMsContainerInspectionReader().Read(stream, "/tmp/Skill_00002.ms"));
    }

    [Fact]
    public void ReadPayload_Version2ContainerDecryptsImagePayload()
    {
        var imageBytes = MsContainerFixture.CreatePropertyImage(
            MsContainerFixture.CreateScalarProperty("level", 7));
        var bytes = MsContainerFixture.CreateV2(
            "Skill_00002.ms",
            new MsContainerFixture.Entry(
                "Skill/1000.img",
                0,
                imageBytes.Length,
                1024,
                Payload: imageBytes));
        using var stream = new MemoryStream(bytes);
        var inspection = new WzMsContainerInspectionReader()
            .Read(stream, "/tmp/Skill_00002.ms");

        var payload = new WzMsImagePayloadReader()
            .Read(stream, inspection.Header, Assert.Single(inspection.Entries));

        Assert.Equal(imageBytes, payload.ToArray());
    }

    [Fact]
    public void ReadPayload_Version4ContainerDecryptsImagePayload()
    {
        var imageBytes = MsContainerFixture.CreatePropertyImage(
            MsContainerFixture.CreateScalarProperty("level", 7));
        var bytes = MsContainerFixture.CreateV4(
            "Skill_00002.ms",
            new MsContainerFixture.Entry(
                "Skill/1000.img",
                0,
                imageBytes.Length,
                1024,
                Payload: imageBytes));
        using var stream = new MemoryStream(bytes);
        var inspection = new WzMsContainerInspectionReader()
            .Read(stream, "/tmp/Skill_00002.ms");

        var payload = new WzMsImagePayloadReader()
            .Read(stream, inspection.Header, Assert.Single(inspection.Entries));

        Assert.Equal(imageBytes, payload.ToArray());
    }

    private static int CalculateRandomByteCount(string fileName)
    {
        return fileName.Sum(static c => c) % 312 + 30;
    }
}
