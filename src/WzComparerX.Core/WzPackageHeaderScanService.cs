using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class WzPackageHeaderScanService
{
    private readonly WzPackageHeaderReader reader;

    public WzPackageHeaderScanService(WzPackageHeaderReader? reader = null)
    {
        this.reader = reader ?? new WzPackageHeaderReader();
    }

    public async Task<IReadOnlyList<WzPackageHeader>> ScanAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var files = ResolveFiles(path);
        var headers = new List<WzPackageHeader>(files.Count);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            headers.Add(await reader.ReadAsync(file, cancellationToken));
        }

        return headers;
    }

    private static IReadOnlyList<string> ResolveFiles(string path)
    {
        if (File.Exists(path))
        {
            return [Path.GetFullPath(path)];
        }

        if (Directory.Exists(path))
        {
            return Directory
                .EnumerateFiles(path, "*.wz", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal)
                .Select(Path.GetFullPath)
                .ToArray();
        }

        throw new FileNotFoundException($"File or directory not found: {path}", path);
    }
}
