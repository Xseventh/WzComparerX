using WzComparerX.WzLib;

namespace WzComparerX.WzLib.Tests;

public class WzPkg1OffsetCalculatorTests
{
    [Fact]
    public void CalculateHashVersion_UsesPkg1VersionHashAlgorithm()
    {
        var hashVersion = WzPkg1VersionHash.CalculateHashVersion(777);

        Assert.Equal(59192u, hashVersion);
        Assert.Equal(0x20, WzPkg1VersionHash.CalculateEncryptedVersion(hashVersion));
    }

    [Fact]
    public void CalculateOffset_UsesPkg1OffsetAlgorithm()
    {
        var offset = WzPkg1OffsetCalculator.CalculateOffset(
            hashOffsetPosition: 24,
            hashedOffset: 0x22c1230c,
            headerSize: 16,
            hashVersion: 59192);

        Assert.Equal(16u, offset);
    }
}
