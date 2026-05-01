using WzComparerX.Core;

namespace WzComparerX.App.ViewModels;

public static class ResourceDetailsProjection
{
    public static IReadOnlyList<ResourceMetadataItemViewModel> CreateDocumentMetadata(
        ResourceInspectionDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        List<ResourceMetadataItemViewModel> items =
        [
            new("source", document.SourcePath),
            new("format", document.Format),
        ];

        foreach (var item in document.DebugMetadata ?? [])
        {
            items.Add(ResourceMetadataItemViewModel.FromMetadata(item));
        }

        return items;
    }

    public static IReadOnlyList<ResourceMetadataItemViewModel> CreateSelectionMetadata(
        ResourceInspectionNodeViewModel? node)
    {
        if (node is null)
        {
            return [];
        }

        List<ResourceMetadataItemViewModel> items =
        [
            new("name", node.Name),
            new("kind", node.Kind),
        ];
        AddOptional(items, "path", node.Path);
        AddOptional(items, "value", node.DisplayValue);
        items.AddRange(node.DebugMetadata);
        return items;
    }

    public static IReadOnlyList<ResourceDiagnosticViewModel> CreateSelectionDiagnostics(
        ResourceInspectionNodeViewModel? node)
    {
        return node?.Diagnostics.ToArray() ?? [];
    }

    private static void AddOptional(
        ICollection<ResourceMetadataItemViewModel> items,
        string name,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            items.Add(new ResourceMetadataItemViewModel(name, value));
        }
    }
}
