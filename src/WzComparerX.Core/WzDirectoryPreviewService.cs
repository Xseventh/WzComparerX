using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class WzDirectoryPreviewService
{
    private readonly WzDirectoryPreviewReader reader;

    public WzDirectoryPreviewService(WzDirectoryPreviewReader? reader = null)
    {
        this.reader = reader ?? new WzDirectoryPreviewReader();
    }

    public WzDirectoryPreviewService(WzStringEncryptionKind stringEncryptionKind)
        : this(new WzDirectoryPreviewReader(stringDecryptor: new WzStringDecryptor(stringEncryptionKind)))
    {
    }

    public Task<WzDirectoryPreview> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        return reader.ReadAsync(path, cancellationToken);
    }
}
