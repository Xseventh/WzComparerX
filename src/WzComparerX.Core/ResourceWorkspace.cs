namespace WzComparerX.Core;

public sealed class ResourceWorkspace
{
    private readonly List<ResourceDocument> documents = new();

    public IReadOnlyList<ResourceDocument> Documents => documents;

    public void Add(ResourceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        documents.Add(document);
    }
}

