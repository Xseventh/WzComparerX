namespace WzComparerX.WzLib;

internal sealed record WzPkg2DirectoryProfile(
    string Name,
    int WzVersion,
    uint HashVersion,
    uint Hash1,
    uint Pkg2StringKey)
{
    private const uint OffsetMagic = 0x1A2B3C4D;

    public int DecryptEntryCount(int encryptedEntryCount)
    {
        unchecked
        {
            var mixedHash = CalculateMixedHash(Hash1, HashVersion);
            return (int)((uint)encryptedEntryCount ^
                ((Hash1 << 16) + (mixedHash & 0x7fffffffu) - (0x21524111u * HashVersion)));
        }
    }

    public uint CalculateOffset(uint hashOffsetPosition, uint hashedOffset, uint headerSize)
    {
        unchecked
        {
            var preHash = Hash1 ^ HashVersion;
            var mixedHash = CalculateMixedHash(Hash1, HashVersion);
            var offset = hashOffsetPosition - headerSize;
            offset = ~offset;
            offset *= preHash + (mixedHash ^ 0xA7E3C093u);
            offset -= 0x581C3F6Du;
            offset ^= Hash1 * 0x01010101u;
            offset ^= mixedHash * 0x9E3779B9u;
            offset = RotateLeft(offset, (int)((preHash ^ mixedHash) & 0x1f));
            offset ^= ~hashedOffset;
            offset += headerSize;
            return offset;
        }
    }

    public static bool TryDetect(
        WzPackageHeader header,
        ReadOnlySpan<byte> firstPkg2StringBytes,
        out WzPkg2DirectoryProfile profile)
    {
        profile = default!;
        if (header.Hash1 is not uint hash1 ||
            header.Hash2 is not uint hash2 ||
            firstPkg2StringBytes.Length < 8 ||
            (firstPkg2StringBytes.Length & 1) != 0)
        {
            return false;
        }

        var keyByte1 = firstPkg2StringBytes[1];
        var keyByte2 = firstPkg2StringBytes[3];
        var keyByte3 = firstPkg2StringBytes[5];
        if (firstPkg2StringBytes[7] != 0)
        {
            return false;
        }

        for (var keyByte0 = 0; keyByte0 <= byte.MaxValue; keyByte0++)
        {
            var key = (uint)keyByte0 |
                ((uint)keyByte1 << 8) |
                ((uint)keyByte2 << 16) |
                ((uint)keyByte3 << 24);
            var name = DecodePkg2String(firstPkg2StringBytes, key);
            if (!IsLegalNodeName(name))
            {
                continue;
            }

            var hashVersion = RecoverHashVersionFromKmst1199Key(hash1, key);
            if (VerifyV5(hash1, hash2, hashVersion))
            {
                profile = new WzPkg2DirectoryProfile(
                    "pkg2_kmst1200",
                    WzVersion: 1200,
                    hashVersion,
                    hash1,
                    key);
                return true;
            }

            if (VerifyV4(hash1, hash2, hashVersion))
            {
                profile = new WzPkg2DirectoryProfile(
                    "pkg2_kmst1199",
                    WzVersion: 1199,
                    hashVersion,
                    hash1,
                    key);
                return true;
            }
        }

        return false;
    }

    public static string DecodePkg2String(ReadOnlySpan<byte> bytes, uint key)
    {
        if ((bytes.Length & 1) != 0)
        {
            throw new InvalidDataException("PKG2 directory string bytes must be UTF-16 aligned.");
        }

        var output = new char[bytes.Length / sizeof(char)];
        Span<byte> keyBytes =
        [
            (byte)key,
            (byte)(key >> 8),
            (byte)(key >> 8),
            (byte)(key >> 16),
            (byte)(key >> 16),
            (byte)(key >> 24),
            (byte)(key >> 24),
            0x00
        ];

        for (var i = 0; i < output.Length; i++)
        {
            var byteIndex = i * sizeof(char);
            var low = bytes[byteIndex] ^ keyBytes[byteIndex & 7];
            var high = bytes[byteIndex + 1] ^ keyBytes[(byteIndex + 1) & 7];
            output[i] = (char)(low | (high << 8));
        }

        return new string(output);
    }

    private static uint RecoverHashVersionFromKmst1199Key(uint hash1, uint key)
    {
        unchecked
        {
            var inner = InvertMix(key) ^ 0x4F4CB34Au;
            var baseHash = InvertMix(inner);
            return baseHash ^ hash1 ^ 0x6D4C3B2Au;
        }
    }

    private static uint CalculateMixedHash(uint hash1, uint hashVersion)
    {
        unchecked
        {
            var preHash = hash1 ^ hashVersion;
            return Mix(preHash ^ 0x6D4C3B2Au) ^ 0x91E10DA5u;
        }
    }

    private static bool VerifyV4(uint hash1, uint hash2, uint hashVersion)
    {
        unchecked
        {
            var preHash = hash1 ^ hashVersion;
            var mixedHash = CalculateMixedHash(hash1, hashVersion);
            var left = hash1 ^ ((mixedHash & 0xffffu) + hashVersion + OffsetMagic);
            left = RotateLeft(left, (int)(((mixedHash ^ hashVersion) & 0x0fu) + (hash1 & 0x0fu)));
            return (left ^ (preHash + mixedHash)) == ~hash2;
        }
    }

    private static bool VerifyV5(uint hash1, uint hash2, uint hashVersion)
    {
        unchecked
        {
            var preHash = hash1 ^ hashVersion;
            var mixedHash = CalculateMixedHash(hash1, hashVersion);
            var left = hash1 ^ ((mixedHash & 0xffffu) + hashVersion + OffsetMagic);
            left = RotateLeft(left, (int)(((mixedHash ^ hashVersion) & 0x0fu) + (hash1 & 0x0fu)));
            return (left ^ (preHash + mixedHash) ^ 0x2A2C818Bu) == hash2;
        }
    }

    private static uint Mix(uint value)
    {
        unchecked
        {
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value;
        }
    }

    private static uint InvertMix(uint value)
    {
        unchecked
        {
            value = UnxorShiftRight(value, 16);
            value *= 0x43021123u;
            value = UnxorShiftRight(value, 15);
            value *= 0x1D69E2A5u;
            value = UnxorShiftRight(value, 16);
            return value;
        }
    }

    private static uint UnxorShiftRight(uint value, int shift)
    {
        for (var current = shift; current < 32; current *= 2)
        {
            value ^= value >> current;
        }

        return value;
    }

    private static uint RotateLeft(uint value, int distance)
    {
        return (value << distance) | (value >> (32 - distance));
    }

    private static bool IsLegalNodeName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (value.EndsWith(".img", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var character in value)
        {
            if (character < 0x20 || character > 0x7e)
            {
                return false;
            }
        }

        return true;
    }
}
