namespace WzComparerX.WzLib;

internal sealed class WzImageBinaryInspectionReader
{
    private readonly int maxPropertyDepth;
    private readonly WzImageBinaryObjectInspectionReader objectReader;

    public WzImageBinaryInspectionReader(WzStringDecryptor stringDecryptor, int maxPropertyDepth)
    {
        this.maxPropertyDepth = maxPropertyDepth;
        objectReader = new WzImageBinaryObjectInspectionReader(stringDecryptor, maxPropertyDepth);
    }

    public WzImageInspection Read(
        Stream stream,
        WzPackageHeader header,
        WzDirectoryEntryInspection entry,
        string selector,
        long imageBaseOffset,
        long imageEndOffset)
    {
        var objectType = objectReader.ReadObjectTypeName(stream, imageBaseOffset);
        int? propertyCount = null;
        IReadOnlyList<WzImagePropertyInspectionEntry>? properties = null;
        object? objectValue = null;
        if (objectType == "Property" && maxPropertyDepth > 0)
        {
            properties = objectReader.ReadPropertyEntries(
                stream,
                imageBaseOffset,
                imageEndOffset,
                depth: 0,
                parentPath: string.Empty,
                out propertyCount);
        }
        else if (maxPropertyDepth > 0)
        {
            var objectInspection = objectReader.ReadTopLevelObjectValue(stream, imageBaseOffset, imageEndOffset, objectType);
            objectValue = objectInspection?.Value;
            propertyCount = objectInspection?.ChildCount;
            properties = objectInspection?.Children;
        }

        return new WzImageInspection(header, selector, entry, objectType, propertyCount, properties, objectValue);
    }
}
