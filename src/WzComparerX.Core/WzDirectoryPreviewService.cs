using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class WzDirectoryPreviewService
{
    private readonly WzDirectoryPreviewReader reader;

    public WzDirectoryPreviewService(WzDirectoryPreviewReader? reader = null)
    {
        this.reader = reader ?? new WzDirectoryPreviewReader();
    }

    public Task<WzDirectoryPreview> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        return reader.ReadAsync(path, cancellationToken);
    }
}
