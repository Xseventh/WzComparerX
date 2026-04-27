using System.Buffers.Binary;
using System.Text;

namespace WzComparerX.WzLib;

internal sealed class WzImageLuaInspectionReader(
    WzStringDecryptor stringDecryptor,
    int maxPropertyDepth)
{
    public WzImageInspection Read(
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

    public static bool IsLuaEntry(WzDirectoryEntryInspection entry)
    {
        return (entry.Path ?? entry.Name)?.EndsWith(".lua", StringComparison.OrdinalIgnoreCase) == true;
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
            var inspection = new WzImageLuaInspection(payload.Length, CreateLuaSnippet(script), script);
            entries.Add(new WzImagePropertyInspectionEntry(entries.Count, null, flag, "lua", inspection));
        }

        return entries;
    }

    private static string CreateLuaSnippet(string script)
    {
        const int maxLength = 80;
        var normalized = script.ReplaceLineEndings("\\n");
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static int ReadCompressedInt32(Stream stream)
    {
        var value = ReadSByte(stream);
        return value == sbyte.MinValue ? ReadInt32LittleEndian(stream) : value;
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

    private static int ReadInt32LittleEndian(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadInt32LittleEndian(bytes);
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
