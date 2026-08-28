using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace WzComparerX.WzLib;

public static class WzImageCanvasPngEncoder
{
    private static ReadOnlySpan<byte> Signature => [137, 80, 78, 71, 13, 10, 26, 10];

    public static byte[] EncodeBgra8888(int width, int height, ReadOnlySpan<byte> pixels)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "PNG dimensions must be positive.");
        }

        var stride = checked(width * 4);
        var expectedLength = checked(stride * height);
        if (pixels.Length != expectedLength)
        {
            throw new ArgumentException(
                $"Expected {expectedLength} BGRA8888 bytes for a {width}x{height} image, found {pixels.Length}.",
                nameof(pixels));
        }

        var scanlines = new byte[checked((stride + 1) * height)];
        for (var y = 0; y < height; y++)
        {
            var sourceRow = pixels.Slice(y * stride, stride);
            var outputRow = scanlines.AsSpan(y * (stride + 1), stride + 1);
            outputRow[0] = 0;
            for (var x = 0; x < width; x++)
            {
                var source = x * 4;
                var destination = 1 + source;
                outputRow[destination] = sourceRow[source + 2];
                outputRow[destination + 1] = sourceRow[source + 1];
                outputRow[destination + 2] = sourceRow[source];
                outputRow[destination + 3] = sourceRow[source + 3];
            }
        }

        byte[] compressed;
        using (var compressedStream = new MemoryStream())
        {
            using (var zlib = new ZLibStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(scanlines);
            }

            compressed = compressedStream.ToArray();
        }

        using var output = new MemoryStream();
        output.Write(Signature);

        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], height);
        header[8] = 8;
        header[9] = 6;
        header[10] = 0;
        header[11] = 0;
        header[12] = 0;
        WriteChunk(output, "IHDR", header);
        WriteChunk(output, "IDAT", compressed);
        WriteChunk(output, "IEND", ReadOnlySpan<byte>.Empty);
        return output.ToArray();
    }

    private static void WriteChunk(Stream output, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        output.Write(length);

        Span<byte> typeBytes = stackalloc byte[4];
        Encoding.ASCII.GetBytes(type, typeBytes);
        output.Write(typeBytes);
        output.Write(data);

        var crc = ComputeCrc32(typeBytes, data);
        Span<byte> checksum = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(checksum, crc);
        output.Write(checksum);
    }

    private static uint ComputeCrc32(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        var crc = uint.MaxValue;
        UpdateCrc(ref crc, type);
        UpdateCrc(ref crc, data);
        return ~crc;
    }

    private static void UpdateCrc(ref uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0
                    ? (crc >> 1) ^ 0xedb88320u
                    : crc >> 1;
            }
        }
    }
}
