using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class ResourceDocumentService
{
    private readonly SyntheticResourceDocumentReader syntheticReader;

    public ResourceDocumentService(SyntheticResourceDocumentReader? syntheticReader = null)
    {
        this.syntheticReader = syntheticReader ?? new SyntheticResourceDocumentReader();
    }

    public async Task<ResourceDocument> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        var rawDocument = await syntheticReader.ReadAsync(path, cancellationToken);
        return new ResourceDocument(Guid.NewGuid(), rawDocument.SourcePath, rawDocument.Root);
    }
}

