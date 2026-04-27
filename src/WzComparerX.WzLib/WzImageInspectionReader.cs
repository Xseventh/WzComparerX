using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace WzComparerX.WzLib;

public sealed class WzImageInspectionReader
{
    public const int MaxPropertyInspectionDepth = 64;

    private readonly WzStringDecryptor stringDecryptor;
    private readonly int maxPropertyDepth;

    public WzImageInspectionReader(WzStringDecryptor? stringDecryptor = null, int maxPropertyDepth = 1)
    {
        if (maxPropertyDepth is < 0 or > MaxPropertyInspectionDepth)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxPropertyDepth),
                $"Property inspection depth must be between 0 and {MaxPropertyInspectionDepth}.");
        }

        this.stringDecryptor = stringDecryptor ?? new WzStringDecryptor();
        this.maxPropertyDepth = maxPropertyDepth;
    }

    public WzImageInspection Read(
        Stream stream,
        WzPackageHeader header,
        WzDirectoryEntryInspection entry,
        string selector)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        if (entry.Kind != WzDirectoryEntryKind.Image)
        {
            throw new InvalidDataException($"Selected entry is not an image: {entry.Path ?? entry.Name ?? entry.Index.ToString()}.");
        }

        if (entry.Offset is not long offset)
        {
            throw new InvalidDataException("Selected image entry does not have a calculated payload offset.");
        }

        if (offset < 0 || offset >= stream.Length)
        {
            throw new InvalidDataException($"Selected image offset is outside the file: {offset}.");
        }

        var imageEndOffset = offset + entry.DataSize;
        if (imageEndOffset > stream.Length)
        {
            throw new InvalidDataException($"Selected image extends past the file: {imageEndOffset}.");
        }

        stream.Position = offset;
        if (TryReadTextImageInspection(stream, header, entry, selector, imageEndOffset, out var textInspection))
        {
            return textInspection;
        }

        if (IsLuaEntry(entry))
        {
            return ReadLuaInspection(stream, header, entry, selector, imageEndOffset);
        }

        var objectType = ReadImageObjectTypeName(stream, offset);
        int? propertyCount = null;
        IReadOnlyList<WzImagePropertyInspectionEntry>? properties = null;
        object? objectValue = null;
        if (objectType == "Property" && maxPropertyDepth > 0)
        {
            properties = ReadPropertyEntries(stream, offset, imageEndOffset, depth: 0, parentPath: string.Empty, out propertyCount);
        }
        else if (maxPropertyDepth > 0)
        {
            var objectInspection = ReadTopLevelObjectValue(stream, offset, imageEndOffset, objectType);
            objectValue = objectInspection?.Value;
            propertyCount = objectInspection?.ChildCount;
            properties = objectInspection?.Children;
        }

        return new WzImageInspection(header, selector, entry, objectType, propertyCount, properties, objectValue);
    }

    private bool TryReadTextImageInspection(
        Stream stream,
        WzPackageHeader header,
        WzDirectoryEntryInspection entry,
        string selector,
        long imageEndOffset,
        out WzImageInspection inspection)
    {
        inspection = default!;
        var startPosition = stream.Position;
        var signatureBytes = ReadBytes(stream, (int)Math.Min(9, imageEndOffset - startPosition));
        stream.Position = startPosition;

        if (signatureBytes.AsSpan().StartsWith("#Property"u8))
        {
            inspection = ReadTextPropertyV1Inspection(stream, header, entry, selector, imageEndOffset);
            return true;
        }

        if (signatureBytes.AsSpan().StartsWith("Root"u8))
        {
            inspection = ReadTextPropertyV2Inspection(stream, header, entry, selector, imageEndOffset);
            return true;
        }

        return false;
    }

    private WzImageInspection ReadTextPropertyV1Inspection(
        Stream stream,
        WzPackageHeader header,
        WzDirectoryEntryInspection entry,
        string selector,
        long imageEndOffset)
    {
        var text = ReadUtf8Text(stream, imageEndOffset);
        var parser = new TextImageV1Parser(maxPropertyDepth);
        int? propertyCount = null;
        IReadOnlyList<WzImagePropertyInspectionEntry>? properties = null;
        if (maxPropertyDepth > 0)
        {
            properties = parser.Parse(text, out var count);
            propertyCount = count;
        }

        return new WzImageInspection(header, selector, entry, "Property", propertyCount, properties);
    }

    private WzImageInspection ReadTextPropertyV2Inspection(
        Stream stream,
        WzPackageHeader header,
        WzDirectoryEntryInspection entry,
        string selector,
        long imageEndOffset)
    {
        var text = ReadUtf8Text(stream, imageEndOffset);
        var parser = new TextImageV2Parser(maxPropertyDepth);
        int? propertyCount = null;
        IReadOnlyList<WzImagePropertyInspectionEntry>? properties = null;
        if (maxPropertyDepth > 0)
        {
            properties = parser.Parse(text, out var count);
            propertyCount = count;
        }

        return new WzImageInspection(header, selector, entry, "Property", propertyCount, properties);
    }

    private WzImageInspection ReadLuaInspection(
        Stream stream,
        WzPackageHeader header,
        WzDirectoryEntryInspection entry,
        string selector,
        long imageEndOffset)
    {
        const string objectType = "Lua";
        int? propertyCount = null;
        IReadOnlyList<WzImagePropertyInspectionEntry>? properties = null;
        object? objectValue = null;

        if (maxPropertyDepth > 0)
        {
            var luaEntries = ReadLuaEntries(stream, imageEndOffset);
            propertyCount = luaEntries.Count;
            properties = luaEntries;
            if (luaEntries.Count == 1)
            {
                objectValue = luaEntries[0].Value;
            }
        }

        return new WzImageInspection(header, selector, entry, objectType, propertyCount, properties, objectValue);
    }

    private List<WzImagePropertyInspectionEntry> ReadLuaEntries(Stream stream, long imageEndOffset)
    {
        var entries = new List<WzImagePropertyInspectionEntry>();
        while (stream.Position < imageEndOffset)
        {
            var flag = ReadByte(stream);
            if (flag != 0x01)
            {
                throw new InvalidDataException($"Unknown Lua flag 0x{flag:X2}.");
            }

            var length = ReadCompressedInt32(stream);
            if (length < 0)
            {
                throw new InvalidDataException($"Cannot read a negative Lua payload length: {length}.");
            }

            if (stream.Position + length > imageEndOffset)
            {
                throw new InvalidDataException($"Lua payload extends past the image stream: {stream.Position + length}.");
            }

            var payload = stringDecryptor.DecryptPayload(ReadBytes(stream, length));
            var script = Encoding.UTF8.GetString(payload);
            var inspection = new WzImageLuaInspection(payload.Length, CreateLuaSnippet(script));
            entries.Add(new WzImagePropertyInspectionEntry(entries.Count, null, flag, "lua", inspection));
        }

        return entries;
    }

    private static bool IsLuaEntry(WzDirectoryEntryInspection entry)
    {
        return (entry.Path ?? entry.Name)?.EndsWith(".lua", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string CreateLuaSnippet(string script)
    {
        const int maxLength = 80;
        var normalized = script.ReplaceLineEndings("\\n");
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string ReadUtf8Text(Stream stream, long imageEndOffset)
    {
        var length = imageEndOffset - stream.Position;
        if (length > int.MaxValue)
        {
            throw new InvalidDataException($"Text image is too large to inspect: {length} bytes.");
        }

        return Encoding.UTF8.GetString(ReadBytes(stream, (int)length));
    }

    private sealed class TextImageV1Parser(int maxPropertyDepth)
    {
        private const byte TextValueType = 0xff;

        private string[] lines = [];
        private int position;

        public List<WzImagePropertyInspectionEntry> Parse(string text, out int count)
        {
            lines = SplitLines(text);
            position = 0;
            if (lines.Length == 0 || lines[0].TrimEnd() != "#Property")
            {
                throw new InvalidDataException("Text IMG v1 is missing #Property signature.");
            }

            position = 1;
            var entries = ParseEntries(depth: 0, parentPath: string.Empty);
            count = entries.Count;
            return entries;
        }

        private List<WzImagePropertyInspectionEntry> ParseEntries(int depth, string parentPath)
        {
            var entries = new List<WzImagePropertyInspectionEntry>();
            while (position < lines.Length)
            {
                var line = lines[position++].Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (line == "}")
                {
                    break;
                }

                var equalsIndex = line.IndexOf('=');
                if (equalsIndex < 0)
                {
                    throw new InvalidDataException($"Text IMG v1 line is missing '=': {line}");
                }

                var name = line[..equalsIndex].Trim();
                var rawValue = line[(equalsIndex + 1)..].Trim();
                var path = CombinePath(parentPath, name);
                if (rawValue == "{")
                {
                    var children = ParseEntries(depth + 1, path ?? string.Empty);
                    entries.Add(new WzImagePropertyInspectionEntry(
                        entries.Count,
                        name,
                        TextValueType,
                        "object",
                        "Property",
                        depth,
                        path,
                        children.Count,
                        depth + 1 < maxPropertyDepth ? children : null));
                    continue;
                }

                entries.Add(CreateTextValueEntry(entries.Count, name, rawValue, depth, path));
            }

            return entries;
        }
    }

    private sealed class TextImageV2Parser(int maxPropertyDepth)
    {
        private const byte TextValueType = 0xff;

        private string[] lines = [];
        private int position;

        public List<WzImagePropertyInspectionEntry> Parse(string text, out int count)
        {
            lines = SplitLines(text);
            position = 0;
            var root = ReadNextNode();
            if (root.Indent != 0 || root.Name != "Root" || root.Kind != "object")
            {
                throw new InvalidDataException("Text IMG v2 is missing Root <Property> signature.");
            }

            var entries = ParseEntries(expectedIndent: 1, depth: 0, parentPath: string.Empty);
            count = entries.Count;
            return entries;
        }

        private List<WzImagePropertyInspectionEntry> ParseEntries(int expectedIndent, int depth, string parentPath)
        {
            var entries = new List<WzImagePropertyInspectionEntry>();
            while (position < lines.Length)
            {
                if (string.IsNullOrWhiteSpace(lines[position]))
                {
                    position++;
                    continue;
                }

                var node = PeekNode();
                if (node.Indent < expectedIndent)
                {
                    break;
                }

                if (node.Indent > expectedIndent)
                {
                    throw new InvalidDataException($"Unexpected text IMG v2 indent {node.Indent} for node {node.Name}.");
                }

                position++;
                var path = CombinePath(parentPath, node.Name);
                if (node.Kind == "object")
                {
                    var children = ParseEntries(expectedIndent + 1, depth + 1, path ?? string.Empty);
                    entries.Add(new WzImagePropertyInspectionEntry(
                        entries.Count,
                        node.Name,
                        TextValueType,
                        "object",
                        "Property",
                        depth,
                        path,
                        children.Count,
                        depth + 1 < maxPropertyDepth ? children : null));
                    continue;
                }

                entries.Add(new WzImagePropertyInspectionEntry(
                    entries.Count,
                    node.Name,
                    TextValueType,
                    node.Kind,
                    node.Value,
                    depth,
                    path));
            }

            return entries;
        }

        private TextNode PeekNode()
        {
            var savedPosition = position;
            var node = ReadNextNode();
            position = savedPosition;
            return node;
        }

        private TextNode ReadNextNode()
        {
            while (position < lines.Length && string.IsNullOrWhiteSpace(lines[position]))
            {
                position++;
            }

            if (position >= lines.Length)
            {
                throw new InvalidDataException("Unexpected end of text IMG v2 stream.");
            }

            return ParseNode(lines[position++]);
        }

        private static TextNode ParseNode(string line)
        {
            var indent = 0;
            while (indent < line.Length && line[indent] == '\t')
            {
                indent++;
            }

            var content = line[indent..];
            var firstSpace = content.IndexOf(' ');
            if (firstSpace < 0)
            {
                throw new InvalidDataException($"Text IMG v2 node is missing type: {line}");
            }

            var name = content[..firstSpace];
            var rest = content[(firstSpace + 1)..];
            var valueSeparator = rest.IndexOf('\t');
            var typeName = valueSeparator < 0 ? rest.Trim() : rest[..valueSeparator].Trim();
            var rawValue = valueSeparator < 0 ? null : rest[(valueSeparator + 1)..];
            var (kind, value) = ParseTypedValue(typeName, rawValue);
            return new TextNode(indent, name, kind, value);
        }
    }

    private sealed record TextNode(int Indent, string Name, string Kind, object? Value);

    private static WzImagePropertyInspectionEntry CreateTextValueEntry(
        int index,
        string name,
        string rawValue,
        int depth,
        string? path)
    {
        const byte textValueType = 0xff;
        if (rawValue.Length == 0)
        {
            return new WzImagePropertyInspectionEntry(index, name, textValueType, "null", Depth: depth, Path: path);
        }

        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return new WzImagePropertyInspectionEntry(index, name, textValueType, "int32", intValue, depth, path);
        }

        if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            return new WzImagePropertyInspectionEntry(index, name, textValueType, "int64", longValue, depth, path);
        }

        if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return new WzImagePropertyInspectionEntry(index, name, textValueType, "double", doubleValue, depth, path);
        }

        return new WzImagePropertyInspectionEntry(index, name, textValueType, "string", rawValue, depth, path);
    }

    private static (string Kind, object? Value) ParseTypedValue(string typeName, string? rawValue)
    {
        return typeName switch
        {
            "<Empty>" => ("null", null),
            "<I4>" => ("int32", int.Parse(rawValue ?? string.Empty, CultureInfo.InvariantCulture)),
            "<I8>" => ("int64", long.Parse(rawValue ?? string.Empty, CultureInfo.InvariantCulture)),
            "<R8>" => ("double", double.Parse(rawValue ?? string.Empty, CultureInfo.InvariantCulture)),
            "<String>" => ("string", rawValue ?? string.Empty),
            "<Vector>" => ("vector", ParseTextVector(rawValue ?? string.Empty)),
            "<Property>" => ("object", "Property"),
            _ => throw new InvalidDataException($"Unknown text IMG v2 node type: {typeName}.")
        };
    }

    private static WzImageVectorInspection ParseTextVector(string rawValue)
    {
        var commaIndex = rawValue.IndexOf(',');
        if (commaIndex < 0)
        {
            throw new InvalidDataException($"Text IMG vector is missing comma: {rawValue}");
        }

        var x = int.Parse(rawValue[..commaIndex], CultureInfo.InvariantCulture);
        var y = int.Parse(rawValue[(commaIndex + 1)..], CultureInfo.InvariantCulture);
        return new WzImageVectorInspection(x, y);
    }

    private static string[] SplitLines(string text)
    {
        return text.ReplaceLineEndings("\n").Split('\n');
    }

    private string ReadImageObjectTypeName(Stream stream, long imageBaseOffset)
    {
        var flag = ReadByte(stream);
        return flag switch
        {
            0x73 => ReadString(stream),
            0x1b => ReadStringAt(stream, imageBaseOffset + ReadInt32LittleEndian(stream)),
            _ => throw new InvalidDataException($"Unexpected image object type flag 0x{flag:X2}.")
        };
    }

    private List<WzImagePropertyInspectionEntry>? ReadPropertyEntries(
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

        var objectType = ReadImageObjectTypeName(stream, imageBaseOffset);
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

    private WzImagePropertyInspectionEntry? ReadTopLevelObjectValue(
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
        var canvas = new WzImageCanvasInspection(
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
        return new WzImagePropertyInspectionEntry(index, name, type, "canvas", canvas, depth, path, childCount, children);
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
            6656 => width * height * 16,
            _ => (int?)null
        };
        if (perPageLength is null)
        {
            return null;
        }

        var actualPages = pages > 0 ? pages : 1;
        return checked(perPageLength.Value * actualPages);
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
            var objectType = ReadImageObjectTypeName(stream, imageBaseOffset);
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
        var video = new WzImageVideoInspection(unknown, dataOffset, dataLength);
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
        var sound = new WzImageSoundInspection(
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
        var rawData = new WzImageRawDataInspection(version, dataOffset, dataLength);
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

    private static string ReadGuidString(Stream stream)
    {
        return new Guid(ReadBytes(stream, 16)).ToString();
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

    private static byte ReadByte(Stream stream)
    {
        var value = stream.ReadByte();
        if (value < 0)
        {
            throw new EndOfStreamException();
        }

        return (byte)value;
    }

    private static sbyte ReadSByte(Stream stream)
    {
        return unchecked((sbyte)ReadByte(stream));
    }

    private static short ReadInt16LittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(short)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadInt16LittleEndian(bytes);
    }

    private static int ReadInt32LittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadInt32LittleEndian(bytes);
    }

    private static long ReadInt64LittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadInt64LittleEndian(bytes);
    }

    private static int ReadCompressedInt32(Stream stream)
    {
        var value = ReadSByte(stream);
        return value == sbyte.MinValue ? ReadInt32LittleEndian(stream) : value;
    }

    private static long ReadCompressedInt64(Stream stream)
    {
        var value = ReadSByte(stream);
        return value == sbyte.MinValue ? ReadInt64LittleEndian(stream) : value;
    }

    private static float ReadCompressedSingle(Stream stream)
    {
        var value = ReadSByte(stream);
        return value == sbyte.MinValue ? ReadSingleLittleEndian(stream) : value;
    }

    private static float ReadSingleLittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(float)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadSingleLittleEndian(bytes);
    }

    private static double ReadDoubleLittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(double)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadDoubleLittleEndian(bytes);
    }

    private static byte[] ReadBytes(Stream stream, int count)
    {
        if (count < 0)
        {
            throw new InvalidDataException($"Cannot read a negative byte count: {count}.");
        }

        var bytes = new byte[count];
        stream.ReadExactly(bytes);
        return bytes;
    }

    private static void SkipBytes(Stream stream, int count)
    {
        if (count < 0)
        {
            throw new InvalidDataException($"Cannot skip a negative byte count: {count}.");
        }

        stream.Seek(count, SeekOrigin.Current);
    }
}
