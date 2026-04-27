using System.Globalization;

namespace WzComparerX.WzLib;

public static class WzPkg1VersionHash
{
    public static uint CalculateHashVersion(int wzVersion)
    {
        var version = wzVersion.ToString(CultureInfo.InvariantCulture);
        uint hash = 0;

        foreach (var value in version)
        {
            hash <<= 5;
            hash += value + 1u;
        }

        return hash;
    }

    public static int CalculateEncryptedVersion(uint hashVersion)
    {
        return 0xff
            ^ (int)((hashVersion >> 24) & 0xff)
            ^ (int)((hashVersion >> 16) & 0xff)
            ^ (int)((hashVersion >> 8) & 0xff)
            ^ (int)(hashVersion & 0xff);
    }
}
