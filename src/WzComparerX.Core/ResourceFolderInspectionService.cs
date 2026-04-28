using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class ResourceFolderInspectionService
{
    private readonly WzPackageHeaderScanService headerScanService;

    public ResourceFolderInspectionService(WzPackageHeaderScanService? headerScanService = null)
    {
        this.headerScanService = headerScanService ?? new WzPackageHeaderScanService();
    }

    public async Task<ResourceInspectionDocument> InspectAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {fullPath}");
        }

        var headers = await headerScanService.ScanAsync(fullPath, cancellationToken);
        var rootName = Path.GetFileName(Path.TrimEndingDirectorySeparator(fullPath));
        if (string.IsNullOrWhiteSpace(rootName))
        {
            rootName = fullPath;
        }

        var root = new ResourceInspectionNode(
            rootName,
            "folder",
            fullPath,
            $"{headers.Count} packages",
            headers.Select(ProjectPackage).ToArray());

        return new ResourceInspectionDocument(
            fullPath,
            "folder",
            root,
            [
                new ResourceInspectionMetadata("sourceKind", "folder"),
                new ResourceInspectionMetadata("packageCount", headers.Count)
            ]);
    }

    private static ResourceInspectionNode ProjectPackage(WzPackageHeader header)
    {
        var format = header.Format.ToString().ToLowerInvariant();
        var displayValue = header.IsValid ? format : "invalid";
        return new ResourceInspectionNode(
            Path.GetFileName(header.SourcePath),
            "package",
            header.SourcePath,
            displayValue,
            DebugMetadata: BuildPackageMetadata(header));
    }

    private static IReadOnlyList<ResourceInspectionMetadata> BuildPackageMetadata(WzPackageHeader header)
    {
        return
        [
            new ResourceInspectionMetadata("valid", header.IsValid),
            new ResourceInspectionMetadata("format", header.Format.ToString().ToLowerInvariant()),
            new ResourceInspectionMetadata("dataSize", header.DataSize),
            new ResourceInspectionMetadata("fileSize", header.FileSize),
            new ResourceInspectionMetadata("directoryStartPosition", header.DirectoryStartPosition),
            new ResourceInspectionMetadata("encryptedVersion", header.EncryptedVersion)
        ];
    }
}
