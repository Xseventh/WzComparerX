using static WzComparerX.WzLib.WzImageBinaryReaderPrimitives;

namespace WzComparerX.WzLib;

internal static class WzImagePayloadInspectionReader
{
    public static WzImageCanvasInspection ReadCanvas(Stream stream, long imageEndOffset)
    {
        var width = ReadCompressedInt32(stream);
        var height = ReadCompressedInt32(stream);
        var format = ReadCompressedInt32(stream);
        var scale = ReadByte(stream);
        var pages = ReadCompressedInt32(stream);
        var unknown1 = ReadCompressedInt32(stream);
        SkipBytes(stream, 2);
        var dataLength = ReadInt32LittleEndian(stream);
        var dataOffset = stream.Position;
        if (dataLength < 0)
        {
            throw new InvalidDataException($"Cannot read a negative canvas data length: {dataLength}.");
        }

        if (dataOffset + dataLength > imageEndOffset)
        {
            throw new InvalidDataException($"Canvas data extends past the image stream: {dataOffset + dataLength}.");
        }

        var compressionKind = DetectCanvasCompressionKind(stream, dataOffset, dataLength);
        var uncompressedDataLength = GetCanvasUncompressedDataLength(format, scale, pages, width, height);
        SkipBytes(stream, dataLength);
        return new WzImageCanvasInspection(
            width,
            height,
            format,
            scale,
            pages,
            unknown1,
            dataOffset,
            dataLength,
            compressionKind,
            uncompressedDataLength);
    }

    public static WzImageRawDataInspection ReadRawData(Stream stream, long imageEndOffset, int version)
    {
        var dataLength = ReadCompressedInt32(stream);
        if (dataLength < 0)
        {
            throw new InvalidDataException($"Cannot read a negative raw data length: {dataLength}.");
        }

        var dataOffset = stream.Position;
        if (dataOffset + dataLength > imageEndOffset)
        {
            throw new InvalidDataException($"Raw data extends past the image stream: {dataOffset + dataLength}.");
        }

        SkipBytes(stream, dataLength);
        return new WzImageRawDataInspection(version, dataOffset, dataLength);
    }

    public static WzImageVideoInspection ReadVideo(Stream stream, long imageEndOffset)
    {
        var unknown = ReadByte(stream);
        var dataLength = ReadCompressedInt32(stream);
        if (dataLength < 0)
        {
            throw new InvalidDataException($"Cannot read a negative video data length: {dataLength}.");
        }

        var dataOffset = stream.Position;
        if (dataOffset + dataLength > imageEndOffset)
        {
            throw new InvalidDataException($"Video data extends past the image stream: {dataOffset + dataLength}.");
        }

        SkipBytes(stream, dataLength);
        return new WzImageVideoInspection(unknown, dataOffset, dataLength);
    }

    public static WzImageSoundInspection ReadSound(Stream stream, long imageEndOffset, int version)
    {
        var dataLength = ReadCompressedInt32(stream);
        if (dataLength < 0)
        {
            throw new InvalidDataException($"Cannot read a negative sound data length: {dataLength}.");
        }

        var duration = ReadCompressedInt32(stream);
        var soundDeclaration = ReadByte(stream);
        var majorType = ReadGuidString(stream);
        var subType = ReadGuidString(stream);
        var fixedSizeSamples = ReadByte(stream) != 0;
        var temporalCompression = ReadByte(stream) != 0;
        var formatType = ReadGuidString(stream);
        int? formatExtraLength = null;
        if (soundDeclaration == 2)
        {
            formatExtraLength = ReadCompressedInt32(stream);
            if (formatExtraLength < 0)
            {
                throw new InvalidDataException($"Cannot read a negative sound format data length: {formatExtraLength}.");
            }

            if (stream.Position + formatExtraLength.Value > imageEndOffset)
            {
                throw new InvalidDataException($"Sound format data extends past the image stream: {stream.Position + formatExtraLength.Value}.");
            }

            SkipBytes(stream, formatExtraLength.Value);
        }

        var dataOffset = stream.Position;
        if (dataOffset + dataLength > imageEndOffset)
        {
            throw new InvalidDataException($"Sound data extends past the image stream: {dataOffset + dataLength}.");
        }

        SkipBytes(stream, dataLength);
        return new WzImageSoundInspection(
            version,
            duration,
            soundDeclaration,
            majorType,
            subType,
            fixedSizeSamples,
            temporalCompression,
            formatType,
            formatExtraLength,
            dataOffset,
            dataLength);
    }

    private static WzImageCanvasCompressionKind DetectCanvasCompressionKind(
        Stream stream,
        long dataOffset,
        int dataLength)
    {
        if (dataLength < 3)
        {
            return WzImageCanvasCompressionKind.Unknown;
        }

        var position = stream.Position;
        try
        {
            stream.Position = dataOffset + 1;
            var first = ReadByte(stream);
            var second = ReadByte(stream);
            return first == 0x78 && second == 0x9c
                ? WzImageCanvasCompressionKind.Zlib
                : WzImageCanvasCompressionKind.ChunkedEncryptedZlib;
        }
        finally
        {
            stream.Position = position;
        }
    }

    private static int? GetCanvasUncompressedDataLength(
        int format,
        int scale,
        int pages,
        int width,
        int height)
    {
        var actualScale = scale > 0 ? 1 << scale : 1;
        if (actualScale > 1)
        {
            if (width % actualScale != 0 || height % actualScale != 0)
            {
                return null;
            }

            width /= actualScale;
            height /= actualScale;
        }

        var perPageLength = format switch
        {
            1 or 257 or 513 or 769 => width * height * 2,
            2 or 2562 => width * height * 4,
            1026 or 2050 => ((width + 3) / 4) * ((height + 3) / 4) * 16,
            4098 => width * (height & ~3),
            4097 => ((width + 3) / 4) * ((height + 3) / 4) * 8,
            2304 => width * height,
            4100 => width * height * 16,
            _ => (int?)null
        };
        if (perPageLength is null)
        {
            return null;
        }

        var actualPages = pages > 0 ? pages : 1;
        return checked(perPageLength.Value * actualPages);
    }

    private static string ReadGuidString(Stream stream)
    {
        return new Guid(ReadBytes(stream, 16)).ToString();
    }

}
