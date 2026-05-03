using WzComparerX.WzLib;

namespace WzComparerX.Core;

internal static class WzMsImageInspectionLoader
{
    public static async Task<WzMsImageInspectionContext> LoadAsync(
        string path,
        string selector,
        WzStringEncryptionKind? stringKey,
        int maxPropertyDepth,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        var containerInspection = await new WzMsContainerInspectionReader().ReadAsync(path, cancellationToken);
        var entry = FindImageEntry(containerInspection, selector);
        var payload = await new WzMsImagePayloadReader().ReadAsync(
            path,
            containerInspection,
            entry,
            cancellationToken);

        var selectedStringKey = stringKey ?? WzStringEncryptionKind.None;
        var imageReader = new WzImageInspectionReader(
            new WzStringDecryptor(selectedStringKey),
            maxPropertyDepth);
        var imageInspection = imageReader.Read(
            payload,
            CreateImageHeader(containerInspection, entry),
            CreateImageEntry(entry),
            entry.Path);
        if (!imageInspection.IsValid)
        {
            await payload.DisposeAsync();
            throw new InvalidDataException($"Invalid MS/MN image selection: {selector}.");
        }

        payload.Position = 0;
        return new WzMsImageInspectionContext(
            containerInspection,
            entry,
            imageInspection,
            selectedStringKey,
            payload);
    }

    public static WzMsContainerEntryInspection? TryFindImageEntry(
        WzMsContainerInspection inspection,
        string selector)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        return inspection.Entries.FirstOrDefault(candidate =>
            string.Equals(candidate.Path, selector, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.Name, selector, StringComparison.OrdinalIgnoreCase));
    }

    private static WzMsContainerEntryInspection FindImageEntry(
        WzMsContainerInspection inspection,
        string selector)
    {
        var entry = TryFindImageEntry(inspection, selector);
        if (entry is not null)
        {
            return entry;
        }

        throw new InvalidDataException($"Image entry not found: {selector}.");
    }

    private static WzPackageHeader CreateImageHeader(
        WzMsContainerInspection inspection,
        WzMsContainerEntryInspection entry)
    {
        return new WzPackageHeader(
            WzPackageFormat.Pkg1,
            "MS",
            inspection.Header.SourcePath,
            string.Empty,
            HeaderSize: 0,
            DataSize: entry.Size,
            FileSize: entry.Size,
            DirectoryStartPosition: 0);
    }

    private static WzDirectoryEntryInspection CreateImageEntry(WzMsContainerEntryInspection entry)
    {
        return new WzDirectoryEntryInspection(
            entry.Index,
            0x04,
            WzDirectoryEntryKind.Image,
            entry.Name,
            entry.Size,
            entry.Checksum,
            HashOffsetPosition: 0,
            HashOffset: 0,
            Offset: 0,
            Path: entry.Path);
    }
}

internal sealed record WzMsImageInspectionContext(
    WzMsContainerInspection ContainerInspection,
    WzMsContainerEntryInspection EntryInspection,
    WzImageInspection ImageInspection,
    WzStringEncryptionKind StringKey,
    Stream PayloadStream) : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        return PayloadStream.DisposeAsync();
    }
}
