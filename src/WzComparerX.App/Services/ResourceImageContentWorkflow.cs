using WzComparerX.App.ViewModels;
using WzComparerX.Core;

namespace WzComparerX.App.Services;

public sealed class ResourceImageContentWorkflow
{
    private readonly ResourceInspectionService inspectionService;

    public ResourceImageContentWorkflow(ResourceInspectionService inspectionService)
    {
        this.inspectionService = inspectionService;
    }

    public bool CanLoadSelectedImage(
        string currentPackagePath,
        string currentFormat,
        bool isBusy,
        ResourceInspectionNodeViewModel? node)
    {
        if (isBusy ||
            node?.Kind != "image" ||
            IsSyntheticFormat(currentFormat))
        {
            return false;
        }

        return ResolveSelectedTarget(currentPackagePath, node) is not null;
    }

    public bool CanLoadManualImage(
        string currentPackagePath,
        string currentFormat,
        bool isBusy,
        string? selector)
    {
        var path = currentPackagePath.Trim();
        if (isBusy ||
            path.Length == 0 ||
            Directory.Exists(path) ||
            IsSyntheticFormat(currentFormat) ||
            string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return ResolveManualTarget(path, selector) is not null;
    }

    public ResourceImageSelectorTarget? ResolveSelectedTarget(
        string currentPackagePath,
        ResourceInspectionNodeViewModel? node)
    {
        return node?.Kind == "image"
            ? ResourceImageSelector.Resolve(currentPackagePath.Trim(), node.Path ?? node.Name)
            : null;
    }

    public ResourceImageSelectorTarget? ResolveManualTarget(
        string currentPackagePath,
        string? selector)
    {
        return ResourceImageSelector.Resolve(currentPackagePath.Trim(), selector);
    }

    public bool IsCurrentTarget(
        ResourceImageSelectorTarget? current,
        ResourceImageSelectorTarget target)
    {
        return current is not null &&
            PathsEqual(current.PackagePath, target.PackagePath) &&
            string.Equals(current.Selector, target.Selector, StringComparison.Ordinal);
    }

    public Task<ResourceInspectionDocument> LoadAsync(
        ResourceImageSelectorTarget target,
        ResourceInspectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(options);

        return inspectionService.InspectAsync(target.PackagePath, target.Selector, options);
    }

    private static bool IsSyntheticFormat(string currentFormat)
    {
        return string.Equals(currentFormat, "synthetic", StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(left),
                Path.GetFullPath(right),
                StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }
    }
}
