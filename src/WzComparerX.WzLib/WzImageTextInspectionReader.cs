using System.Globalization;
using System.Text;

namespace WzComparerX.WzLib;

internal sealed class WzImageTextInspectionReader(int maxPropertyDepth)
{
    public bool TryRead(
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

        return new WzImageInspection(
            header,
            selector,
            entry,
            "Property",
            propertyCount,
            properties,
            new WzImageTextInspection("v1", Encoding.UTF8.GetByteCount(text), text));
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

        return new WzImageInspection(
            header,
            selector,
            entry,
            "Property",
            propertyCount,
            properties,
            new WzImageTextInspection("v2", Encoding.UTF8.GetByteCount(text), text));
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

    private static string? CombinePath(string parentPath, string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.IsNullOrEmpty(parentPath) ? null : parentPath;
        }

        return string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";
    }

    private static string[] SplitLines(string text)
    {
        return text.ReplaceLineEndings("\n").Split('\n');
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
}
