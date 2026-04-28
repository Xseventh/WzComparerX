using System.Collections.ObjectModel;
using WzComparerX.Core;

namespace WzComparerX.App.ViewModels;

public sealed class ResourceInspectionNodeViewModel
{
    private ResourceInspectionNodeViewModel(
        string name,
        string kind,
        string? path,
        string? displayValue,
        bool isExpanded,
        IEnumerable<ResourceInspectionNodeViewModel> children,
        IEnumerable<ResourceMetadataItemViewModel> debugMetadata,
        IEnumerable<ResourceDiagnosticViewModel> diagnostics)
    {
        Name = name;
        Kind = kind;
        Path = path;
        DisplayValue = displayValue;
        IsExpanded = isExpanded;
        Children = new ObservableCollection<ResourceInspectionNodeViewModel>(children);
        DebugMetadata = new ObservableCollection<ResourceMetadataItemViewModel>(debugMetadata);
        Diagnostics = new ObservableCollection<ResourceDiagnosticViewModel>(diagnostics);
    }

    public string Name { get; }

    public string Kind { get; }

    public string? Path { get; }

    public string? DisplayValue { get; }

    public string Title => DisplayValue is null
        ? $"{Name} [{Kind}]"
        : $"{Name} [{Kind}] : {DisplayValue}";

    public bool IsExpanded { get; set; }

    public ObservableCollection<ResourceInspectionNodeViewModel> Children { get; }

    public ObservableCollection<ResourceMetadataItemViewModel> DebugMetadata { get; }

    public ObservableCollection<ResourceDiagnosticViewModel> Diagnostics { get; }

    public static ResourceInspectionNodeViewModel FromNode(ResourceInspectionNode node)
    {
        return FromNode(node, depth: 0);
    }

    private static ResourceInspectionNodeViewModel FromNode(ResourceInspectionNode node, int depth)
    {
        return new ResourceInspectionNodeViewModel(
            node.Name,
            node.Kind,
            node.Path,
            node.DisplayValue,
            isExpanded: depth == 0 && node.Children.Count > 0,
            node.Children.Select(child => FromNode(child, depth + 1)),
            (node.DebugMetadata ?? []).Select(ResourceMetadataItemViewModel.FromMetadata),
            (node.Diagnostics ?? []).Select(ResourceDiagnosticViewModel.FromDiagnostic));
    }
}
