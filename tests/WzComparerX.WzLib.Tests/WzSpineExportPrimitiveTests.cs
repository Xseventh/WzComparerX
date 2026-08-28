using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using WzComparerX.WzLib;

namespace WzComparerX.WzLib.Tests;

public class WzSpineExportPrimitiveTests
{
    [Fact]
    public void ReadRawDataPayload_ReturnsExactBytesAndRestoresPosition()
    {
        using var stream = new MemoryStream([0x10, 0x20, 0x30, 0x40, 0x50]);
        stream.Position = 1;

        var payload = WzImageRawDataPayloadReader.Read(
            stream,
            new WzImageRawDataInspection(1, 2, 2));

        Assert.Equal([0x30, 0x40], payload);
        Assert.Equal(1, stream.Position);
    }

    [Fact]
    public void ReadBinaryVersion_ReadsSpine4VersionAfterHash()
    {
        byte[] data =
        [
            0x57, 0xe6, 0x4e, 0xcf, 0x25, 0xd0, 0xb4, 0x59,
            0x07, (byte)'4', (byte)'.', (byte)'1', (byte)'.', (byte)'2', (byte)'4'
        ];

        Assert.Equal("4.1.24", WzSpineSkeletonVersionReader.ReadBinaryVersion(data));
    }

    [Fact]
    public void ReadPages_ReturnsEveryAtlasTextureAndDeclaredSize()
    {
        const string atlas = """
            first.png
            size:2,1
            filter:Linear,Linear
            region
            bounds:0,0,2,1

            second.png
            size:1,2
            pma:true
            region2
            bounds:0,0,1,2
            """;

        var pages = WzSpineAtlasReader.ReadPages(atlas);

        Assert.Equal(2, pages.Count);
        Assert.Equal(new WzSpineAtlasPage("first.png", 2, 1), pages[0]);
        Assert.Equal(new WzSpineAtlasPage("second.png", 1, 2), pages[1]);
    }

    [Fact]
    public void EncodeBgra8888_WritesRgbaPngWithExpectedDimensionsAndPixels()
    {
        var png = WzImageCanvasPngEncoder.EncodeBgra8888(
            2,
            1,
            [0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80]);

        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png[..8]);
        Assert.Equal(2, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)));
        Assert.Equal(1, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));

        var idat = ReadChunk(png, "IDAT");
        using var compressed = new MemoryStream(idat);
        using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
        using var scanlines = new MemoryStream();
        zlib.CopyTo(scanlines);
        Assert.Equal(
            new byte[] { 0x00, 0x30, 0x20, 0x10, 0x40, 0x70, 0x60, 0x50, 0x80 },
            scanlines.ToArray());
    }

    private static byte[] ReadChunk(byte[] png, string requestedType)
    {
        var offset = 8;
        while (offset < png.Length)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset, 4));
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            if (type == requestedType)
            {
                return png.AsSpan(offset + 8, length).ToArray();
            }

            offset += 12 + length;
        }

        throw new InvalidDataException($"PNG chunk not found: {requestedType}.");
    }
}
