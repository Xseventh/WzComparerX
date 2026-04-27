namespace WzComparerX.WzLib;

public sealed record WzPkg1VersionDetection(int WzVersion, uint HashVersion);

public static class WzPkg1VersionDetector
{
    private const int MissingEncryptedVersionWzVersion = 777;

    public static WzPkg1VersionDetection? Detect(
        WzPackageHeader header,
        IReadOnlyList<WzDirectoryEntryInspection> entries,
        long directoryEndPosition)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(entries);

        if (header.Format != WzPackageFormat.Pkg1)
        {
            return null;
        }

        if (header.IsEncryptedVersionMissing)
        {
            return Create(MissingEncryptedVersionWzVersion);
        }

        if (header.EncryptedVersion is not int encryptedVersion || entries.Count == 0)
        {
            return null;
        }

        for (var wzVersion = 0; wzVersion < short.MaxValue; wzVersion++)
        {
            var hashVersion = WzPkg1VersionHash.CalculateHashVersion(wzVersion);
            if (WzPkg1VersionHash.CalculateEncryptedVersion(hashVersion) != encryptedVersion)
            {
                continue;
            }

            if (ValidateOffsets(header, entries, directoryEndPosition, hashVersion))
            {
                return new WzPkg1VersionDetection(wzVersion, hashVersion);
            }
        }

        return null;
    }

    private static WzPkg1VersionDetection Create(int wzVersion)
    {
        return new WzPkg1VersionDetection(
            wzVersion,
            WzPkg1VersionHash.CalculateHashVersion(wzVersion));
    }

    private static bool ValidateOffsets(
        WzPackageHeader header,
        IReadOnlyList<WzDirectoryEntryInspection> entries,
        long directoryEndPosition,
        uint hashVersion)
    {
        var ranges = new List<(long Start, long End)>(entries.Count);

        foreach (var entry in entries)
        {
            if (entry.DataSize < 0)
            {
                return false;
            }

            var offset = CalculateOffset(header, entry, hashVersion);
            switch (entry.Kind)
            {
                case WzDirectoryEntryKind.Image:
                    var imageEnd = offset + entry.DataSize;
                    if (offset < directoryEndPosition || imageEnd > header.FileSize)
                    {
                        return false;
                    }

                    ranges.Add((offset, imageEnd));
                    break;

                case WzDirectoryEntryKind.Directory:
                    var directoryEnd = offset + 1;
                    if (offset < header.DirectoryStartPosition || directoryEnd > directoryEndPosition)
                    {
                        return false;
                    }

                    ranges.Add((offset, directoryEnd));
                    break;
            }
        }

        ranges.Sort(static (left, right) => left.Start.CompareTo(right.Start));
        for (var i = 1; i < ranges.Count; i++)
        {
            if (ranges[i - 1].End > ranges[i].Start)
            {
                return false;
            }
        }

        return true;
    }

    internal static uint CalculateOffset(
        WzPackageHeader header,
        WzDirectoryEntryInspection entry,
        uint hashVersion)
    {
        return WzPkg1OffsetCalculator.CalculateOffset(
            checked((uint)entry.HashOffsetPosition),
            entry.HashOffset,
            checked((uint)header.HeaderSize),
            hashVersion);
    }
}
