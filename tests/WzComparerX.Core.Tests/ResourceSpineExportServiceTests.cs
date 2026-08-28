using System.Buffers.Binary;
using WzComparerX.Tests;
using WzComparerX.WzLib;

namespace WzComparerX.Core.Tests;

public class ResourceSpineExportServiceTests
{
    [Fact]
    public async Task LoadAsync_ExportsSkeletonAtlasAndEveryTexturePage()
    {
        const string atlas = """
            hero.png
            size:2,1
            filter:Linear,Linear
            body
            bounds:0,0,2,1

            hero_2.png
            size:1,2
            pma:true
            glow
            bounds:0,0,1,2
            """;
        var skeleton = CreateSpine4Skeleton("4.1.24");
        byte[] firstPixels = [0x10, 0x20, 0x30, 0xff, 0x40, 0x50, 0x60, 0xff];
        byte[] secondPixels = [0x70, 0x80, 0x90, 0xff, 0xa0, 0xb0, 0xc0, 0xff];
        var image = MsContainerFixture.CreateSpinePropertyImage(
            "asset",
            "hero",
            atlas,
            skeleton,
            ("hero.png", firstPixels, 2, 1),
            ("hero_2.png", secondPixels, 1, 2));
        var path = Path.Combine(Path.GetTempPath(), $"Spine_00000-{Guid.NewGuid():N}.ms");
        await File.WriteAllBytesAsync(
            path,
            MsContainerFixture.CreateV4(
                Path.GetFileName(path),
                new MsContainerFixture.Entry("Map/Test.img", 0, image.Length, 1024, Payload: image)));

        try
        {
            var document = await new ResourceSpineExportService().LoadAsync(
                path,
                "Map/Test.img",
                "asset",
                new ResourceInspectionOptions(WzStringEncryptionKind.None));

            Assert.Equal("hero", document.SpineName);
            Assert.Equal("4.1.24", document.SpineVersion);
            Assert.Equal("hero.atlas", document.AtlasFileName);
            Assert.Equal("hero.skel", document.SkeletonFileName);
            Assert.Equal(skeleton, AssertFile(document, "hero.skel").Content);
            Assert.Equal(atlas, System.Text.Encoding.UTF8.GetString(AssertFile(document, "hero.atlas").Content));
            AssertPng(AssertFile(document, "hero.png").Content, 2, 1);
            AssertPng(AssertFile(document, "hero_2.png").Content, 1, 2);
            Assert.Equal(
                [
                    new ResourceSpineExportPage("hero.png", 2, 1),
                    new ResourceSpineExportPage("hero_2.png", 1, 2)
                ],
                document.Pages);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static ResourceExportFile AssertFile(ResourceSpineExportDocument document, string name)
    {
        return Assert.Single(document.Files, file => file.RelativePath == name);
    }

    private static void AssertPng(byte[] bytes, int width, int height)
    {
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes[..8]);
        Assert.Equal(width, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)));
        Assert.Equal(height, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
    }

    private static byte[] CreateSpine4Skeleton(string version)
    {
        return
        [
            0x57, 0xe6, 0x4e, 0xcf, 0x25, 0xd0, 0xb4, 0x59,
            checked((byte)(version.Length + 1)),
            .. System.Text.Encoding.UTF8.GetBytes(version)
        ];
    }
}
