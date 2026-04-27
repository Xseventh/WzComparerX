namespace WzComparerX.WzLib;

public static class WzPkg1OffsetCalculator
{
    public static uint CalculateOffset(uint hashOffsetPosition, uint hashedOffset, uint headerSize, uint hashVersion)
    {
        unchecked
        {
            var offset = hashOffsetPosition - headerSize;
            offset = ~offset;
            offset *= hashVersion;
            offset -= 0x581C3F6D;
            var distance = (int)offset & 0x1F;
            offset = RotateLeft(offset, distance);
            offset ^= hashedOffset;
            offset += headerSize * 2;
            return offset;
        }
    }

    private static uint RotateLeft(uint value, int distance)
    {
        return (value << distance) | (value >> (32 - distance));
    }
}
