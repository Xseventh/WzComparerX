using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class ResourceFolderInspectionService
{
    private readonly WzPackageHeaderScanService headerScanService;
    private readonly WzMsContainerInspectionReader msContainerReader;

    public ResourceFolderInspectionService(
        WzPackageHeaderScanService? headerScanService = null,
        WzMsContainerInspectionReader? msContainerReader = null)
    {
        this.headerScanService = headerScanService ?? new WzPackageHeaderScanService();
        this.msContainerReader = msContainerReader ?? new WzMsContainerInspectionReader();
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

        var packages = new List<ResourceInspectionNode>();
        foreach (var packagePath in EnumerateResourcePackagePaths(fullPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            packages.Add(await InspectPackageAsync(packagePath, cancellationToken));
        }

        var rootName = Path.GetFileName(Path.TrimEndingDirectorySeparator(fullPath));
        if (string.IsNullOrWhiteSpace(rootName))
        {
            rootName = fullPath;
        }

        var root = new ResourceInspectionNode(
            rootName,
            "folder",
            fullPath,
            $"{packages.Count} packages",
            packages);

        return new ResourceInspectionDocument(
            fullPath,
            "folder",
            root,
            [
                new ResourceInspectionMetadata("sourceKind", "folder"),
                new ResourceInspectionMetadata("packageCount", packages.Count)
            ]);
    }

    private async Task<ResourceInspectionNode> InspectPackageAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (IsWzPath(path))
        {
            var header = (await headerScanService.ScanAsync(path, cancellationToken))[0];
            return ProjectWzPackage(header);
        }

        return await InspectMsMnPackageAsync(path, cancellationToken);
    }

    private async Task<ResourceInspectionNode> InspectMsMnPackageAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var containerKind = MsMnContainerKind.FromPath(path);
        try
        {
            var inspection = await msContainerReader.ReadAsync(path, cancellationToken);
            return new ResourceInspectionNode(
                Path.GetFileName(inspection.Header.SourcePath),
                "package",
                inspection.Header.SourcePath,
                containerKind,
                DebugMetadata: BuildMsPackageMetadata(inspection, containerKind),
                Identity: new ResourceInspectionIdentity(PackagePath: inspection.Header.SourcePath));
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException or EndOfStreamException)
        {
            var fullPath = Path.GetFullPath(path);
            var diagnostic = ResourceInspectionDiagnostics.MsContainerInspectionUnsupported(fullPath);
            return new ResourceInspectionNode(
                Path.GetFileName(fullPath),
                "package",
                fullPath,
                "invalid",
                DebugMetadata:
                [
                    new ResourceInspectionMetadata("valid", false),
                    new ResourceInspectionMetadata("format", containerKind),
                    new ResourceInspectionMetadata("containerKind", containerKind)
                ],
                Diagnostics: [diagnostic],
                Identity: new ResourceInspectionIdentity(PackagePath: fullPath));
        }
    }

    private static ResourceInspectionNode ProjectWzPackage(WzPackageHeader header)
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

    private static IEnumerable<string> EnumerateResourcePackagePaths(string path)
    {
        return Directory
            .EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Where(IsResourcePackagePath)
            .Order(StringComparer.Ordinal)
            .Select(Path.GetFullPath);
    }

    private static bool IsResourcePackagePath(string path)
    {
        return (IsWzPath(path) && !IsListFilePath(path)) || MsMnContainerKind.IsPath(path);
    }

    private static bool IsWzPath(string path)
    {
        return string.Equals(Path.GetExtension(path), ".wz", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsListFilePath(string path)
    {
        return string.Equals(Path.GetFileName(path), "List.wz", StringComparison.OrdinalIgnoreCase);
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

    private static IReadOnlyList<ResourceInspectionMetadata> BuildMsPackageMetadata(
        WzMsContainerInspection inspection,
        string containerKind)
    {
        return
        [
            new ResourceInspectionMetadata("valid", true),
            new ResourceInspectionMetadata("format", containerKind),
            new ResourceInspectionMetadata("containerKind", containerKind),
            new ResourceInspectionMetadata("version", inspection.Header.Version),
            new ResourceInspectionMetadata("entryCount", inspection.Header.EntryCount),
            new ResourceInspectionMetadata("fileSize", inspection.Header.FileSize),
            new ResourceInspectionMetadata("entryStartPosition", inspection.Header.EntryStartPosition),
            new ResourceInspectionMetadata("dataStartPosition", inspection.Header.DataStartPosition)
        ];
    }
}
