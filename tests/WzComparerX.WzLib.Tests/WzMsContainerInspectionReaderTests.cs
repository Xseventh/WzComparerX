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

    private static int CalculateRandomByteCount(string fileName)
    {
        return fileName.Sum(static c => c) % 312 + 30;
    }
}
