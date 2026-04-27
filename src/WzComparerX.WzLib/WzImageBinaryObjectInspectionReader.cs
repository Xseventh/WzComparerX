using static WzComparerX.WzLib.WzImageBinaryReaderPrimitives;

namespace WzComparerX.WzLib;

internal sealed class WzImageBinaryObjectInspectionReader
{
    private readonly WzStringDecryptor stringDecryptor;
    private readonly int maxPropertyDepth;

    public WzImageBinaryObjectInspectionReader(WzStringDecryptor stringDecryptor, int maxPropertyDepth)
    {
        this.stringDecryptor = stringDecryptor;
        this.maxPropertyDepth = maxPropertyDepth;
    }

    public string ReadObjectTypeName(Stream stream, long imageBaseOffset)
    {
        var flag = ReadByte(stream);
        return flag switch
        {
            0x73 => ReadString(stream),
            0x1b => ReadStringAt(stream, imageBaseOffset + ReadInt32LittleEndian(stream)),
            _ => throw new InvalidDataException($"Unexpected image object type flag 0x{flag:X2}.")
        };
    }

    public List<WzImagePropertyInspectionEntry>? ReadPropertyEntries(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int depth,
        string parentPath,
        out int? propertyCount)
    {
        SkipBytes(stream, 2);
        var count = ReadCompressedInt32(stream);
        propertyCount = count;
        var properties = new List<WzImagePropertyInspectionEntry>(Math.Max(count, 0));

        for (var i = 0; i < count; i++)
        {
            var name = ReadImageString(stream, imageBaseOffset);
            var type = ReadByte(stream);
            var path = CombinePath(parentPath, name);
            properties.Add(ReadPropertyValue(stream, imageBaseOffset, imageEndOffset, i, name, type, depth, path));
        }

        return properties;
    }

    private WzImagePropertyInspectionEntry ReadPropertyValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        return type switch
        {
            0x00 => new WzImagePropertyInspectionEntry(index, name, type, "null", Depth: depth, Path: path),
            0x02 or 0x0b => new WzImagePropertyInspectionEntry(index, name, type, "int16", ReadInt16LittleEndian(stream), depth, path),
            0x03 or 0x13 => new WzImagePropertyInspectionEntry(index, name, type, "int32", ReadCompressedInt32(stream), depth, path),
            0x14 => new WzImagePropertyInspectionEntry(index, name, type, "int64", ReadCompressedInt64(stream), depth, path),
            0x04 => new WzImagePropertyInspectionEntry(index, name, type, "single", ReadCompressedSingle(stream), depth, path),
            0x05 => new WzImagePropertyInspectionEntry(index, name, type, "double", ReadDoubleLittleEndian(stream), depth, path),
            0x08 => new WzImagePropertyInspectionEntry(index, name, type, "string", ReadImageString(stream, imageBaseOffset), depth, path),
            0x09 => ReadObjectPropertyValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            _ => throw new InvalidDataException($"Unknown image property value type 0x{type:X2}.")
        };
    }

    private WzImagePropertyInspectionEntry ReadObjectPropertyValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        var objectDataLength = ReadInt32LittleEndian(stream);
        if (objectDataLength < 0)
        {
            throw new InvalidDataException($"Cannot read a negative object data length: {objectDataLength}.");
        }

        var endPosition = stream.Position + objectDataLength;
        if (endPosition > imageEndOffset)
        {
            throw new InvalidDataException($"Object data extends past the image stream: {endPosition}.");
        }

        var objectType = ReadObjectTypeName(stream, imageBaseOffset);
        var property = objectType switch
        {
            "Property" => ReadNestedPropertyObjectValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            "Shape2D#Vector2D" => ReadVectorObjectValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            "Canvas" => ReadCanvasObjectValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            "Shape2D#Convex2D" => ReadConvexObjectValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            "UOL" => ReadUolObjectValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            "RawData" => ReadRawDataObjectValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            "Canvas#Video" => ReadVideoObjectValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            "Sound_DX8" => ReadSoundObjectValue(stream, imageBaseOffset, imageEndOffset, index, name, type, depth, path),
            _ => new WzImagePropertyInspectionEntry(index, name, type, "object", objectType, depth, path)
        };

        if (stream.Position > endPosition)
        {
            throw new InvalidDataException($"Object data parser moved past the object boundary: {stream.Position}.");
        }

        stream.Position = endPosition;
        return property;
    }

    public WzImagePropertyInspectionEntry? ReadTopLevelObjectValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        string objectType)
    {
        return objectType switch
        {
            "Shape2D#Vector2D" => ReadVectorObjectValue(stream, imageBaseOffset, imageEndOffset, 0, null, 0x09, 0, null),
            "Canvas" => ReadCanvasObjectValue(stream, imageBaseOffset, imageEndOffset, 0, null, 0x09, 0, null),
            "Shape2D#Convex2D" => ReadConvexObjectValue(stream, imageBaseOffset, imageEndOffset, 0, null, 0x09, 0, null),
            "UOL" => ReadUolObjectValue(stream, imageBaseOffset, imageEndOffset, 0, null, 0x09, 0, null),
            "RawData" => ReadRawDataObjectValue(stream, imageBaseOffset, imageEndOffset, 0, null, 0x09, 0, null),
            "Canvas#Video" => ReadVideoObjectValue(stream, imageBaseOffset, imageEndOffset, 0, null, 0x09, 0, null),
            "Sound_DX8" => ReadSoundObjectValue(stream, imageBaseOffset, imageEndOffset, 0, null, 0x09, 0, null),
            _ => null
        };
    }

    private WzImagePropertyInspectionEntry ReadNestedPropertyObjectValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        int? childCount = null;
        List<WzImagePropertyInspectionEntry>? children = null;
        if (depth + 1 < maxPropertyDepth)
        {
            children = ReadPropertyEntries(stream, imageBaseOffset, imageEndOffset, depth + 1, path ?? string.Empty, out childCount);
        }

        return new WzImagePropertyInspectionEntry(index, name, type, "object", "Property", depth, path, childCount, children);
    }

    private static WzImagePropertyInspectionEntry ReadVectorObjectValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        _ = imageBaseOffset;
        _ = imageEndOffset;
        return new WzImagePropertyInspectionEntry(index, name, type, "vector", ReadVectorInspection(stream), depth, path);
    }

    private WzImagePropertyInspectionEntry ReadCanvasObjectValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        SkipBytes(stream, 1);
        int? childCount = null;
        List<WzImagePropertyInspectionEntry>? children = null;
        if (ReadByte(stream) == 0x01)
        {
            var parsedChildren = ReadPropertyEntries(stream, imageBaseOffset, imageEndOffset, depth + 1, path ?? string.Empty, out childCount);
            if (depth + 1 < maxPropertyDepth)
            {
                children = parsedChildren;
            }
        }

        var canvas = WzImagePayloadInspectionReader.ReadCanvas(stream, imageEndOffset);
        return new WzImagePropertyInspectionEntry(index, name, type, "canvas", canvas, depth, path, childCount, children);
    }

    private WzImagePropertyInspectionEntry ReadConvexObjectValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        _ = imageEndOffset;
        var pointCount = ReadCompressedInt32(stream);
        if (pointCount < 0)
        {
            throw new InvalidDataException($"Cannot read a negative convex point count: {pointCount}.");
        }

        var points = new List<WzImageVectorInspection>(pointCount);
        for (var i = 0; i < pointCount; i++)
        {
            var objectType = ReadObjectTypeName(stream, imageBaseOffset);
            if (objectType != "Shape2D#Vector2D")
            {
                throw new InvalidDataException($"Convex2D point {i} is not a vector: {objectType}.");
            }

            points.Add(ReadVectorInspection(stream));
        }

        return new WzImagePropertyInspectionEntry(index, name, type, "convex", new WzImageConvexInspection(points), depth, path);
    }

    private WzImagePropertyInspectionEntry ReadUolObjectValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        _ = imageEndOffset;
        SkipBytes(stream, 1);
        return new WzImagePropertyInspectionEntry(index, name, type, "uol", ReadImageString(stream, imageBaseOffset), depth, path);
    }

    private WzImagePropertyInspectionEntry ReadRawDataObjectValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        var version = ReadByte(stream);
        if (version == 1 && ReadByte(stream) == 0x01)
        {
            ReadMiniProperty(stream, imageBaseOffset, imageEndOffset, depth, path, out var childCount, out var children);
            return ReadRawDataPayload(stream, imageEndOffset, index, name, type, depth, path, version, childCount, children);
        }

        return ReadRawDataPayload(stream, imageEndOffset, index, name, type, depth, path, version, null, null);
    }

    private WzImagePropertyInspectionEntry ReadVideoObjectValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        SkipBytes(stream, 1);
        int? childCount = null;
        List<WzImagePropertyInspectionEntry>? children = null;
        if (ReadByte(stream) == 0x01)
        {
            ReadMiniProperty(stream, imageBaseOffset, imageEndOffset, depth, path, out childCount, out children);
        }

        var video = WzImagePayloadInspectionReader.ReadVideo(stream, imageEndOffset);
        return new WzImagePropertyInspectionEntry(index, name, type, "video", video, depth, path, childCount, children);
    }

    private WzImagePropertyInspectionEntry ReadSoundObjectValue(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path)
    {
        var version = ReadByte(stream);
        int? childCount = null;
        List<WzImagePropertyInspectionEntry>? children = null;
        if (version == 1 && ReadByte(stream) == 0x01)
        {
            ReadMiniProperty(stream, imageBaseOffset, imageEndOffset, depth, path, out childCount, out children);
        }

        var sound = WzImagePayloadInspectionReader.ReadSound(stream, imageEndOffset, version);
        return new WzImagePropertyInspectionEntry(index, name, type, "sound", sound, depth, path, childCount, children);
    }

    private WzImagePropertyInspectionEntry ReadRawDataPayload(
        Stream stream,
        long imageEndOffset,
        int index,
        string? name,
        byte type,
        int depth,
        string? path,
        int version,
        int? childCount,
        List<WzImagePropertyInspectionEntry>? children)
    {
        var rawData = WzImagePayloadInspectionReader.ReadRawData(stream, imageEndOffset, version);
        return new WzImagePropertyInspectionEntry(index, name, type, "rawData", rawData, depth, path, childCount, children);
    }

    private void ReadMiniProperty(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int depth,
        string? path,
        out int? childCount,
        out List<WzImagePropertyInspectionEntry>? children)
    {
        var parsedChildren = ReadPropertyEntries(stream, imageBaseOffset, imageEndOffset, depth + 1, path ?? string.Empty, out childCount);
        children = depth + 1 < maxPropertyDepth ? parsedChildren : null;
    }

    private static WzImageVectorInspection ReadVectorInspection(Stream stream)
    {
        return new WzImageVectorInspection(ReadCompressedInt32(stream), ReadCompressedInt32(stream));
    }

    private static string? CombinePath(string parentPath, string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.IsNullOrEmpty(parentPath) ? null : parentPath;
        }

        return string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";
    }

    private string? ReadImageString(Stream stream, long imageBaseOffset)
    {
        var flag = ReadByte(stream);
        return flag switch
        {
            0x00 => ReadString(stream),
            0x01 => ReadStringAt(stream, imageBaseOffset + ReadInt32LittleEndian(stream)),
            0x04 => SkipNullImageString(stream),
            _ => throw new InvalidDataException($"Unexpected image string flag 0x{flag:X2}.")
        };
    }

    private string ReadString(Stream stream)
    {
        var size = ReadSByte(stream);
        if (size < 0)
        {
            var byteCount = size == sbyte.MinValue ? ReadInt32LittleEndian(stream) : -size;
            return stringDecryptor.Decode(ReadBytes(stream, byteCount), unicode: false);
        }

        if (size > 0)
        {
            var charCount = size == sbyte.MaxValue ? ReadInt32LittleEndian(stream) : size;
            return stringDecryptor.Decode(ReadBytes(stream, charCount * sizeof(char)), unicode: true);
        }

        return string.Empty;
    }

    private static string? SkipNullImageString(Stream stream)
    {
        SkipBytes(stream, 8);
        return null;
    }

    private string ReadStringAt(Stream stream, long offset)
    {
        if (offset < 0)
        {
            throw new InvalidDataException($"Cannot read a string from a negative offset: {offset}.");
        }

        var position = stream.Position;
        stream.Position = offset;
        var value = ReadString(stream);
        stream.Position = position;
        return value;
    }

}
