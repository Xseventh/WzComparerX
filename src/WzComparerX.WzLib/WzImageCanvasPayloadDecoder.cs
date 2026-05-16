using System.IO.Compression;

namespace WzComparerX.WzLib;

public sealed class WzImageCanvasPayloadDecoder
{
    public WzImageCanvasBitmap Decode(Stream stream, WzImageCanvasInspection canvas)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(canvas);

        if (canvas.CompressionKind != WzImageCanvasCompressionKind.Zlib)
        {
            throw new NotSupportedException($"Unsupported Canvas compression kind: {canvas.CompressionKind}.");
        }

        if (canvas.Format is not 1 and not 2 and not 257 and not 513 and not 2050 and not 2562)
        {
            throw new NotSupportedException($"Unsupported Canvas format: {canvas.Format}.");
        }

        if (canvas.UncompressedDataLength is null)
        {
            throw new InvalidDataException("Canvas uncompressed data length is unknown.");
        }

        if (canvas.DataLength < 1)
        {
            throw new InvalidDataException($"Canvas data length is too small: {canvas.DataLength}.");
        }

        if (canvas.DataOffset < 0)
        {
            throw new InvalidDataException($"Canvas data offset is negative: {canvas.DataOffset}.");
        }

        var position = stream.Position;
        try
        {
            stream.Position = canvas.DataOffset + 1;
            using var limitedStream = new WzLimitedReadStream(stream, canvas.DataLength - 1);
            using var zlibStream = new ZLibStream(limitedStream, CompressionMode.Decompress, leaveOpen: false);
            var pixels = new byte[canvas.UncompressedDataLength.Value];
            zlibStream.ReadExactly(pixels);
            if (zlibStream.ReadByte() >= 0)
            {
                throw new InvalidDataException("Canvas payload contains more decompressed data than expected.");
            }

            return new WzImageCanvasBitmap(canvas.Width, canvas.Height, canvas.Format, pixels);
        }
        finally
        {
            stream.Position = position;
        }
    }
}
