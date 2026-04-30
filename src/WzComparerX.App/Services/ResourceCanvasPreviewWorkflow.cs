using WzComparerX.App.ViewModels;
using WzComparerX.Core;

namespace WzComparerX.App.Services;

public sealed class ResourceCanvasPreviewWorkflow
{
    private readonly ResourceCanvasImageService canvasImageService;

    public ResourceCanvasPreviewWorkflow(ResourceCanvasImageService? canvasImageService = null)
    {
        this.canvasImageService = canvasImageService ?? new ResourceCanvasImageService();
    }

    public bool CanLoad(
        ResourceInspectionNodeViewModel? node,
        ResourceImageSelectorTarget? target,
        bool isBusy)
    {
        return IsPreviewNode(node) &&
            !isBusy &&
            target is not null &&
            !string.Equals(Path.GetExtension(target.PackagePath), ".json", StringComparison.OrdinalIgnoreCase) &&
            File.Exists(target.PackagePath);
    }

    public string GetIdleStatus(ResourceInspectionNodeViewModel? node)
    {
        return IsPreviewNode(node)
            ? "Select a Canvas node or link to preview."
            : "Select a Canvas node to preview.";
    }

    public Task<ResourceCanvasImageDocument> LoadAsync(
        ResourceInspectionNodeViewModel node,
        ResourceImageSelectorTarget target,
        ResourceInspectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(target);

        var valueSelector = GetValueSelector(node);
        return valueSelector is null
            ? canvasImageService.LoadFirstAsync(target.PackagePath, target.Selector, options)
            : canvasImageService.LoadAsync(target.PackagePath, target.Selector, valueSelector, options);
    }

    private static bool IsPreviewNode(ResourceInspectionNodeViewModel? node)
    {
        return node is not null &&
            (node.Kind == "canvas" ||
             IsRootCanvasImageNode(node) ||
             IsCanvasLinkNode(node));
    }

    private static string? GetValueSelector(ResourceInspectionNodeViewModel node)
    {
        return node.Kind == "canvas" || IsCanvasLinkNode(node) ? node.Path : null;
    }

    private static bool IsRootCanvasImageNode(ResourceInspectionNodeViewModel node)
    {
        return node.Kind == "image" &&
            node.DebugMetadata.Any(item =>
                item.Name == "valueType" &&
                string.Equals(item.Value, "canvas", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsCanvasLinkNode(ResourceInspectionNodeViewModel node)
    {
        return node.Kind == "string" &&
            (string.Equals(node.Name, "source", StringComparison.Ordinal) ||
             string.Equals(node.Name, "_inlink", StringComparison.Ordinal) ||
             string.Equals(node.Name, "_outlink", StringComparison.Ordinal));
    }
}
