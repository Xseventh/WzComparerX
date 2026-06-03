using WzComparerX.App.ViewModels;
using WzComparerX.Core;
using WzComparerX.Rendering;

namespace WzComparerX.App.Services;

public sealed class ResourceVideoPreviewWorkflow
{
    private readonly ResourceVideoSequenceService videoSequenceService;

    public ResourceVideoPreviewWorkflow(ResourceVideoSequenceService? videoSequenceService = null)
    {
        this.videoSequenceService = videoSequenceService ?? new ResourceVideoSequenceService();
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

    public Task<ResourceVideoSequenceDocument> LoadAsync(
        ResourceInspectionNodeViewModel node,
        ResourceImageSelectorTarget target,
        ResourceInspectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(target);

        return videoSequenceService.LoadAsync(
            target.PackagePath,
            target.Selector,
            node.Path,
            options,
            WzVideoDecodeOptions.Default);
    }

    private static bool IsPreviewNode(ResourceInspectionNodeViewModel? node)
    {
        return node?.Kind == "video";
    }
}
