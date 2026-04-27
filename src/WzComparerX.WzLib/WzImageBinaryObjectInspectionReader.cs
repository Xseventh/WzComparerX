using static WzComparerX.WzLib.WzImageBinaryReaderPrimitives;

namespace WzComparerX.WzLib;

internal sealed class WzImageBinaryObjectInspectionReader
{
    private readonly WzImageBinaryPropertyInspectionReader propertyReader;
    private readonly WzImageBinaryStringReader stringReader;

    public WzImageBinaryObjectInspectionReader(WzStringDecryptor stringDecryptor, int maxPropertyDepth)
    {
        stringReader = new WzImageBinaryStringReader(stringDecryptor);
        propertyReader = new WzImageBinaryPropertyInspectionReader(
            stringReader,
            maxPropertyDepth,
            ReadObjectPropertyValue);
    }

    public string ReadObjectTypeName(Stream stream, long imageBaseOffset)
    {
        return stringReader.ReadObjectTypeName(stream, imageBaseOffset);
    }

    public List<WzImagePropertyInspectionEntry>? ReadPropertyEntries(
        Stream stream,
        long imageBaseOffset,
        long imageEndOffset,
        int depth,
        string parentPath,
        out int? propertyCount)
    {
        return propertyReader.ReadPropertyEntries(stream, imageBaseOffset, imageEndOffset, depth, parentPath, out propertyCount);
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
        var nestedProperties = propertyReader.ReadNestedObjectProperties(stream, imageBaseOffset, imageEndOffset, depth, path);

        return new WzImagePropertyInspectionEntry(
            index,
            name,
            type,
            "object",
            "Property",
            depth,
            path,
            nestedProperties.ChildCount,
            nestedProperties.Children);
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
            var miniProperty = propertyReader.ReadMiniProperties(stream, imageBaseOffset, imageEndOffset, depth, path);
            childCount = miniProperty.ChildCount;
            children = miniProperty.Children;
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
        return new WzImagePropertyInspectionEntry(index, name, type, "uol", stringReader.ReadImageString(stream, imageBaseOffset), depth, path);
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
            var miniProperty = propertyReader.ReadMiniProperties(stream, imageBaseOffset, imageEndOffset, depth, path);
            return ReadRawDataPayload(
                stream,
                imageEndOffset,
                index,
                name,
                type,
                depth,
                path,
                version,
                miniProperty.ChildCount,
                miniProperty.Children);
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
            var miniProperty = propertyReader.ReadMiniProperties(stream, imageBaseOffset, imageEndOffset, depth, path);
            childCount = miniProperty.ChildCount;
            children = miniProperty.Children;
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
            var miniProperty = propertyReader.ReadMiniProperties(stream, imageBaseOffset, imageEndOffset, depth, path);
            childCount = miniProperty.ChildCount;
            children = miniProperty.Children;
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

    private static WzImageVectorInspection ReadVectorInspection(Stream stream)
    {
        return new WzImageVectorInspection(ReadCompressedInt32(stream), ReadCompressedInt32(stream));
    }
}
