using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class ResourceVideoTargetService
{
    public async Task<ResourceVideoTarget> LoadAsync(
        string path,
        string selector,
        string? valueSelector,
        ResourceInspectionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        options ??= new ResourceInspectionOptions();
        ResourceImageInspectionContext? context = await ResourceImageInspectionContext.LoadAsync(
            path,
            selector,
            options,
            cancellationToken);

        try
        {
            var target = SelectVideo(context, selector, valueSelector);
            context = null;
            return target;
        }
        finally
        {
            if (context is not null)
            {
                await context.DisposeAsync();
            }
        }
    }

    private static ResourceVideoTarget SelectVideo(
        ResourceImageInspectionContext context,
        string selector,
        string? valueSelector)
    {
        if (string.IsNullOrWhiteSpace(valueSelector))
        {
            if (context.ImageInspection.ObjectValue is WzImageVideoInspection rootVideo)
            {
                return new ResourceVideoTarget(context, rootVideo, null);
            }

            throw new ResourceVideoTargetException(
                ResourceInspectionDiagnostics.VideoPreviewValueRequired(selector));
        }

        var matches = context.ImageInspection.Properties?
            .SelectMany(Flatten)
            .Where(property => string.Equals(property.Path, valueSelector, StringComparison.Ordinal))
            .ToArray() ?? [];

        if (matches.Length == 0)
        {
            throw new ResourceVideoTargetException(
                ResourceInspectionDiagnostics.VideoPreviewValueNotFound(valueSelector, selector));
        }

        if (matches.Length > 1)
        {
            throw new ResourceVideoTargetException(
                ResourceInspectionDiagnostics.VideoPreviewValueAmbiguous(valueSelector, selector));
        }

        var match = matches[0];
        if (match.Value is not WzImageVideoInspection video)
        {
            throw new ResourceVideoTargetException(
                ResourceInspectionDiagnostics.VideoPreviewValueUnsupported(valueSelector, selector));
        }

        return new ResourceVideoTarget(context, video, match.Path ?? valueSelector);
    }

    private static IEnumerable<WzImagePropertyInspectionEntry> Flatten(WzImagePropertyInspectionEntry property)
    {
        yield return property;
        if (property.Children is null)
        {
            yield break;
        }

        foreach (var child in property.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }
}
