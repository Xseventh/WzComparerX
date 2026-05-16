using System.IO.Compression;
using WzComparerX.WzLib;

namespace WzComparerX.WzLib.Tests;

public class WzImageCanvasPayloadDecoderTests
{
    [Fact]
    public void Decode_ReturnsFormat2ZlibPixels()
    {
        byte[] pixels = [0x10, 0x20, 0x30, 0xff, 0x40, 0x50, 0x60, 0xff];
        var payload = CreateDirectZlibPayload(pixels);
        using var stream = new MemoryStream([0xaa, 0xbb, .. payload, 0xcc]);
        stream.Position = stream.Length;
        var canvas = new WzImageCanvasInspection(
            Width: 2,
            Height: 1,
            Format: 2,
            Scale: 0,
            Pages: 1,
            Unknown1: 0,
            DataOffset: 2,
            DataLength: payload.Length,
            WzImageCanvasCompressionKind.Zlib,
            UncompressedDataLength: pixels.Length);
        var decoder = new WzImageCanvasPayloadDecoder();

        var bitmap = decoder.Decode(stream, canvas);

        Assert.Equal(2, bitmap.Width);
        Assert.Equal(1, bitmap.Height);
        Assert.Equal(2, bitmap.Format);
        Assert.Equal(pixels, bitmap.Pixels);
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void Decode_ReturnsFormat1ZlibRawPixels()
    {
        byte[] pixels = [0x21, 0xf3];
        var payload = CreateDirectZlibPayload(pixels);
        using var stream = new MemoryStream(payload);
        var canvas = new WzImageCanvasInspection(
            Width: 1,
            Height: 1,
            Format: 1,
            Scale: 0,
            Pages: 1,
            Unknown1: 0,
            DataOffset: 0,
            DataLength: payload.Length,
            WzImageCanvasCompressionKind.Zlib,
            UncompressedDataLength: pixels.Length);
        var decoder = new WzImageCanvasPayloadDecoder();

        var bitmap = decoder.Decode(stream, canvas);

        Assert.Equal(1, bitmap.Width);
        Assert.Equal(1, bitmap.Height);
        Assert.Equal(1, bitmap.Format);
        Assert.Equal(pixels, bitmap.Pixels);
    }

    [Theory]
    [InlineData(257)]
    [InlineData(513)]
    [InlineData(769)]
    public void Decode_Returns16BitZlibRawPixels(int format)
    {
        byte[] pixels = [0x00, 0xfc, 0xe0, 0x07];
        var payload = CreateDirectZlibPayload(pixels);
        using var stream = new MemoryStream(payload);
        var canvas = new WzImageCanvasInspection(
            Width: 2,
            Height: 1,
            Format: format,
            Scale: 0,
            Pages: 1,
            Unknown1: 0,
            DataOffset: 0,
            DataLength: payload.Length,
            WzImageCanvasCompressionKind.Zlib,
            UncompressedDataLength: pixels.Length);
        var decoder = new WzImageCanvasPayloadDecoder();

        var bitmap = decoder.Decode(stream, canvas);

        Assert.Equal(2, bitmap.Width);
        Assert.Equal(1, bitmap.Height);
        Assert.Equal(format, bitmap.Format);
        Assert.Equal(pixels, bitmap.Pixels);
    }

    [Fact]
    public void Decode_ReturnsFormat2304ZlibRawPixels()
    {
        byte[] pixels = [0x00, 0x80, 0xff];
        var payload = CreateDirectZlibPayload(pixels);
        using var stream = new MemoryStream(payload);
        var canvas = new WzImageCanvasInspection(
            Width: 3,
            Height: 1,
            Format: 2304,
            Scale: 0,
            Pages: 1,
            Unknown1: 0,
            DataOffset: 0,
            DataLength: payload.Length,
            WzImageCanvasCompressionKind.Zlib,
            UncompressedDataLength: pixels.Length);
        var decoder = new WzImageCanvasPayloadDecoder();

        var bitmap = decoder.Decode(stream, canvas);

        Assert.Equal(3, bitmap.Width);
        Assert.Equal(1, bitmap.Height);
        Assert.Equal(2304, bitmap.Format);
        Assert.Equal(pixels, bitmap.Pixels);
    }

    [Fact]
    public void Decode_ReturnsFormat4097ZlibRawBlocks()
    {
        byte[] pixels = [0x00, 0xf8, 0x00, 0x00, 0xe4, 0x00, 0x00, 0x00];
        var payload = CreateDirectZlibPayload(pixels);
        using var stream = new MemoryStream(payload);
        var canvas = new WzImageCanvasInspection(
            Width: 4,
            Height: 4,
            Format: 4097,
            Scale: 0,
            Pages: 1,
            Unknown1: 0,
            DataOffset: 0,
            DataLength: payload.Length,
            WzImageCanvasCompressionKind.Zlib,
            UncompressedDataLength: pixels.Length);
        var decoder = new WzImageCanvasPayloadDecoder();

        var bitmap = decoder.Decode(stream, canvas);

        Assert.Equal(4, bitmap.Width);
        Assert.Equal(4, bitmap.Height);
        Assert.Equal(4097, bitmap.Format);
        Assert.Equal(pixels, bitmap.Pixels);
    }

    [Fact]
    public void Decode_ReturnsFormat4100ZlibRawPixels()
    {
        byte[] pixels =
        [
            .. BitConverter.GetBytes(1f),
            .. BitConverter.GetBytes(0.5f),
            .. BitConverter.GetBytes(0f),
            .. BitConverter.GetBytes(1f)
        ];
        var payload = CreateDirectZlibPayload(pixels);
        using var stream = new MemoryStream(payload);
        var canvas = new WzImageCanvasInspection(
            Width: 1,
            Height: 1,
            Format: 4100,
            Scale: 0,
            Pages: 1,
            Unknown1: 0,
            DataOffset: 0,
            DataLength: payload.Length,
            WzImageCanvasCompressionKind.Zlib,
            UncompressedDataLength: pixels.Length);
        var decoder = new WzImageCanvasPayloadDecoder();

        var bitmap = decoder.Decode(stream, canvas);

        Assert.Equal(1, bitmap.Width);
        Assert.Equal(1, bitmap.Height);
        Assert.Equal(4100, bitmap.Format);
        Assert.Equal(pixels, bitmap.Pixels);
    }

    [Fact]
    public void Decode_ReturnsFormat2050ZlibRawBlocks()
    {
        var pixels = CreateDxt5Block(color0: 0xf800, color1: 0x0000);
        var payload = CreateDirectZlibPayload(pixels);
        using var stream = new MemoryStream(payload);
        var canvas = new WzImageCanvasInspection(
            Width: 4,
            Height: 4,
            Format: 2050,
            Scale: 0,
            Pages: 1,
            Unknown1: 0,
            DataOffset: 0,
            DataLength: payload.Length,
            WzImageCanvasCompressionKind.Zlib,
            UncompressedDataLength: pixels.Length);
        var decoder = new WzImageCanvasPayloadDecoder();

        var bitmap = decoder.Decode(stream, canvas);

        Assert.Equal(4, bitmap.Width);
        Assert.Equal(4, bitmap.Height);
        Assert.Equal(2050, bitmap.Format);
        Assert.Equal(pixels, bitmap.Pixels);
    }

    [Fact]
    public void Decode_ReturnsFormat1026ZlibRawBlocks()
    {
        var pixels = CreateDxt3Block(color0: 0xf800, color1: 0x0000);
        var payload = CreateDirectZlibPayload(pixels);
        using var stream = new MemoryStream(payload);
        var canvas = new WzImageCanvasInspection(
            Width: 4,
            Height: 4,
            Format: 1026,
            Scale: 0,
            Pages: 1,
            Unknown1: 0,
            DataOffset: 0,
            DataLength: payload.Length,
            WzImageCanvasCompressionKind.Zlib,
            UncompressedDataLength: pixels.Length);
        var decoder = new WzImageCanvasPayloadDecoder();

        var bitmap = decoder.Decode(stream, canvas);

        Assert.Equal(4, bitmap.Width);
        Assert.Equal(4, bitmap.Height);
        Assert.Equal(1026, bitmap.Format);
        Assert.Equal(pixels, bitmap.Pixels);
    }

    [Fact]
    public void Decode_RejectsUnsupportedCompression()
    {
        using var stream = new MemoryStream([0x00, 0x01, 0x02]);
        var canvas = new WzImageCanvasInspection(
            Width: 1,
            Height: 1,
            Format: 2,
            Scale: 0,
            Pages: 1,
            Unknown1: 0,
            DataOffset: 0,
            DataLength: 3,
            WzImageCanvasCompressionKind.ChunkedEncryptedZlib,
            UncompressedDataLength: 4);
        var decoder = new WzImageCanvasPayloadDecoder();

        var ex = Assert.Throws<NotSupportedException>(() => decoder.Decode(stream, canvas));

        Assert.Contains("ChunkedEncryptedZlib", ex.Message);
    }

    [Fact]
    public void Decode_RejectsUnsupportedFormat()
    {
        byte[] pixels = [0x10, 0x20, 0x30, 0xff];
        var payload = CreateDirectZlibPayload(pixels);
        using var stream = new MemoryStream(payload);
        var canvas = new WzImageCanvasInspection(
            Width: 1,
            Height: 1,
            Format: 9999,
            Scale: 0,
            Pages: 1,
            Unknown1: 0,
            DataOffset: 0,
            DataLength: payload.Length,
            WzImageCanvasCompressionKind.Zlib,
            UncompressedDataLength: pixels.Length);
        var decoder = new WzImageCanvasPayloadDecoder();

        var ex = Assert.Throws<NotSupportedException>(() => decoder.Decode(stream, canvas));

        Assert.Contains("format: 9999", ex.Message);
    }

    [Fact]
    public void Decode_RejectsUnexpectedUncompressedLength()
    {
        byte[] pixels = [0x10, 0x20, 0x30, 0xff];
        var payload = CreateDirectZlibPayload(pixels);
        using var stream = new MemoryStream(payload);
        var canvas = new WzImageCanvasInspection(
            Width: 1,
            Height: 1,
            Format: 2,
            Scale: 0,
            Pages: 1,
            Unknown1: 0,
            DataOffset: 0,
            DataLength: payload.Length,
            WzImageCanvasCompressionKind.Zlib,
            UncompressedDataLength: 3);
        var decoder = new WzImageCanvasPayloadDecoder();

        Assert.Throws<InvalidDataException>(() => decoder.Decode(stream, canvas));
    }

    private static byte[] CreateDirectZlibPayload(byte[] pixels)
    {
        using var output = new MemoryStream();
        output.WriteByte(0x00);
        using (var zlib = new ZLibStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            zlib.Write(pixels);
        }

        return output.ToArray();
    }

    private static byte[] CreateDxt5Block(
        byte alpha0 = 255,
        byte alpha1 = 0,
        ushort color0 = 0xf800,
        ushort color1 = 0,
        ulong alphaBits = 0,
        uint colorBits = 0)
    {
        return
        [
            alpha0,
            alpha1,
            (byte)(alphaBits & 0xff),
            (byte)((alphaBits >> 8) & 0xff),
            (byte)((alphaBits >> 16) & 0xff),
            (byte)((alphaBits >> 24) & 0xff),
            (byte)((alphaBits >> 32) & 0xff),
            (byte)((alphaBits >> 40) & 0xff),
            (byte)(color0 & 0xff),
            (byte)(color0 >> 8),
            (byte)(color1 & 0xff),
            (byte)(color1 >> 8),
            (byte)(colorBits & 0xff),
            (byte)((colorBits >> 8) & 0xff),
            (byte)((colorBits >> 16) & 0xff),
            (byte)((colorBits >> 24) & 0xff)
        ];
    }

    private static byte[] CreateDxt3Block(
        ulong alphaNibbles = ulong.MaxValue,
        ushort color0 = 0xf800,
        ushort color1 = 0,
        uint colorBits = 0)
    {
        return
        [
            (byte)(alphaNibbles & 0xff),
            (byte)((alphaNibbles >> 8) & 0xff),
            (byte)((alphaNibbles >> 16) & 0xff),
            (byte)((alphaNibbles >> 24) & 0xff),
            (byte)((alphaNibbles >> 32) & 0xff),
            (byte)((alphaNibbles >> 40) & 0xff),
            (byte)((alphaNibbles >> 48) & 0xff),
            (byte)((alphaNibbles >> 56) & 0xff),
            (byte)(color0 & 0xff),
            (byte)(color0 >> 8),
            (byte)(color1 & 0xff),
            (byte)(color1 >> 8),
            (byte)(colorBits & 0xff),
            (byte)((colorBits >> 8) & 0xff),
            (byte)((colorBits >> 16) & 0xff),
            (byte)((colorBits >> 24) & 0xff)
        ];
    }
}
