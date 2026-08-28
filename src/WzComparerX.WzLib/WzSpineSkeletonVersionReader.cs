using System.Text;

namespace WzComparerX.WzLib;

public static class WzSpineSkeletonVersionReader
{
    public static string? ReadBinaryVersion(ReadOnlySpan<byte> data)
    {
        if (TryReadVersionAt(data, 8, out var version))
        {
            return version;
        }

        var offset = 0;
        if (TryReadString(data, ref offset, out _) &&
            TryReadString(data, ref offset, out var legacyVersion) &&
            IsVersion(legacyVersion))
        {
            return legacyVersion;
        }

        return null;
    }

    private static bool TryReadVersionAt(ReadOnlySpan<byte> data, int offset, out string? version)
    {
        version = null;
        if (!TryReadString(data, ref offset, out var candidate) || !IsVersion(candidate))
        {
            return false;
        }

        version = candidate;
        return true;
    }

    private static bool TryReadString(ReadOnlySpan<byte> data, ref int offset, out string? value)
    {
        value = null;
        if (!TryReadVarUInt32(data, ref offset, out var encodedLength))
        {
            return false;
        }

        if (encodedLength == 0)
        {
            return true;
        }

        if (encodedLength > int.MaxValue)
        {
            return false;
        }

        var byteLength = (int)encodedLength - 1;
        if (byteLength < 0 || byteLength > data.Length - offset)
        {
            return false;
        }

        value = Encoding.UTF8.GetString(data.Slice(offset, byteLength));
        offset += byteLength;
        return true;
    }

    private static bool TryReadVarUInt32(ReadOnlySpan<byte> data, ref int offset, out uint value)
    {
        value = 0;
        for (var shift = 0; shift < 35; shift += 7)
        {
            if ((uint)offset >= (uint)data.Length)
            {
                return false;
            }

            var current = data[offset++];
            value |= (uint)(current & 0x7f) << shift;
            if ((current & 0x80) == 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsVersion(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && Version.TryParse(value, out _);
    }
}
