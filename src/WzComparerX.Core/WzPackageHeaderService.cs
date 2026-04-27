using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class WzPackageHeaderService
{
    private readonly WzPackageHeaderReader reader;

    public WzPackageHeaderService(WzPackageHeaderReader? reader = null)
    {
        this.reader = reader ?? new WzPackageHeaderReader();
    }

    public Task<WzPackageHeader> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        return reader.ReadAsync(path, cancellationToken);
    }
}
