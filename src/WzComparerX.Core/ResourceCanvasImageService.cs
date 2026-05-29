using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class ResourceCanvasImageService
{
    public async Task<ResourceCanvasImageDocument> LoadAsync(
        string path,
        string selector,
        string? valueSelector,
        ResourceInspectionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await LoadCoreAsync(
            path,
            selector,
            valueSelector,
            useFirstCanvasFallback: false,
            options,
            cancellationToken);
    }

    public async Task<ResourceCanvasImageDocument> LoadFirstAsync(
        string path,
        string selector,
        ResourceInspectionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await LoadCoreAsync(
            path,
            selector,
            valueSelector: null,
            useFirstCanvasFallback: true,
            options,
            cancellationToken);
    }

    private static async Task<ResourceCanvasImageDocument> LoadCoreAsync(
        string path,
        string selector,
        string? valueSelector,
        bool useFirstCanvasFallback,
        ResourceInspectionOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        options ??= new ResourceInspectionOptions();
        await using var context = await CanvasImageInspectionContext.LoadAsync(
            path,
            selector,
            options,
            cancellationToken);
        var target = await SelectCanvasAsync(
            context,
            selector,
            valueSelector,
            useFirstCanvasFallback,
            options,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        await using (target)
        {
            WzImageCanvasBgraBitmap bitmap;
            try
            {
                bitmap = new WzImageCanvasBitmapDecoder().DecodeToBgra8888(
                    target.SourceStream,
                    target.Value,
                    target.Path);
            }
            catch (WzImageCanvasBitmapDecodeException ex)
            {
                throw new ResourceCanvasImageException(ToPreviewDiagnostic(ex));
            }

            return new ResourceCanvasImageDocument(
                target.SourcePath,
                target.Selector,
                target.Path,
                bitmap.Width,
                bitmap.Height,
                bitmap.Format,
                bitmap.PixelFormat,
                bitmap.Pixels);
        }
    }

    private static ResourceInspectionDiagnostic ToPreviewDiagnostic(WzImageCanvasBitmapDecodeException exception)
    {
        return exception.Kind switch
        {
            WzImageCanvasBitmapDecodeFailureKind.CompressionUnsupported =>
                ResourceInspectionDiagnostics.CanvasPreviewCompressionUnsupported(exception.CompressionKind!.Value, exception.Path),
            WzImageCanvasBitmapDecodeFailureKind.FormatUnsupported =>
                ResourceInspectionDiagnostics.CanvasPreviewFormatUnsupported(exception.Format!.Value, exception.Path),
            WzImageCanvasBitmapDecodeFailureKind.ScaleUnsupported =>
                ResourceInspectionDiagnostics.CanvasPreviewScaleUnsupported(exception.Scale!.Value, exception.Path),
            _ => ResourceInspectionDiagnostics.CanvasPreviewDecodeFailed(exception.Path)
        };
    }

    private static async Task<CanvasImageTarget> SelectCanvasAsync(
        CanvasImageInspectionContext context,
        string? selector,
        string? valueSelector,
        bool useFirstCanvasFallback,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(valueSelector))
        {
            if (context.ImageInspection.ObjectValue is WzImageCanvasInspection rootCanvas)
            {
                return CanvasImageTarget.Local(context, rootCanvas, null);
            }

            if (useFirstCanvasFallback)
            {
                var firstCanvas = context.ImageInspection.Properties?
                    .SelectMany(Flatten)
                    .FirstOrDefault(property => property.Value is WzImageCanvasInspection);
                if (firstCanvas?.Value is WzImageCanvasInspection firstCanvasValue)
                {
                    return CanvasImageTarget.Local(context, firstCanvasValue, firstCanvas.Path);
                }
            }

            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewValueRequired(selector));
        }

        var matches = context.ImageInspection.Properties?
            .SelectMany(Flatten)
            .Where(property => string.Equals(property.Path, valueSelector, StringComparison.Ordinal))
            .ToArray() ?? [];

        if (matches.Length == 0)
        {
            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewValueNotFound(valueSelector, selector));
        }

        if (matches.Length > 1)
        {
            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewValueAmbiguous(valueSelector, selector));
        }

        var match = matches[0];
        if (match.Value is WzImageCanvasInspection canvas)
        {
            return CanvasImageTarget.Local(context, canvas, match.Path ?? valueSelector);
        }

        if (match.Value is string linkValue && IsCanvasLinkProperty(match))
        {
            var linkedTarget = await ResolveCanvasLinkAsync(
                context,
                match,
                options,
                cancellationToken);
            if (linkedTarget is not null)
            {
                return linkedTarget;
            }

            throw new ResourceCanvasImageException(
                ResourceInspectionDiagnostics.CanvasPreviewLinkUnresolved(
                    valueSelector,
                    selector,
                    match.Name ?? "link",
                    ResourceInspectionLinkResolver.NormalizeLinkTarget(linkValue) ?? string.Empty));
        }

        throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewValueUnsupported(valueSelector, selector));
    }

    private static async Task<CanvasImageTarget?> ResolveCanvasLinkAsync(
        CanvasImageInspectionContext context,
        WzImagePropertyInspectionEntry linkProperty,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken)
    {
        var resolvedTarget = await ResourceInspectionLinkResolver.ResolveAsync(
            context.SourcePath,
            context.ImageInspection,
            linkProperty,
            options.StringKey,
            cancellationToken);
        if (resolvedTarget is null)
        {
            return null;
        }

        if (string.Equals(resolvedTarget.PackagePath, context.SourcePath, StringComparison.Ordinal) &&
            string.Equals(resolvedTarget.ImageSelector, context.ImageInspection.Selector, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(resolvedTarget.ValuePath))
            {
                return context.ImageInspection.ObjectValue is WzImageCanvasInspection localRootCanvas
                    ? CanvasImageTarget.Local(context, localRootCanvas, null)
                    : null;
            }

            var localCanvas = FindCanvasProperty(context.ImageInspection, resolvedTarget.ValuePath);
            return localCanvas?.Value is WzImageCanvasInspection canvas
                ? CanvasImageTarget.Local(context, canvas, localCanvas.Path ?? resolvedTarget.ValuePath)
                : null;
        }

        var linkedContext = await CanvasImageInspectionContext.LoadAsync(
            resolvedTarget.PackagePath,
            resolvedTarget.ImageSelector,
            options,
            cancellationToken);
        var linkedCanvasProperty = string.IsNullOrWhiteSpace(resolvedTarget.ValuePath)
            ? null
            : FindCanvasProperty(linkedContext.ImageInspection, resolvedTarget.ValuePath);
        if (linkedCanvasProperty?.Value is WzImageCanvasInspection linkedCanvas)
        {
            return CanvasImageTarget.Linked(
                linkedContext,
                linkedCanvas,
                linkedCanvasProperty.Path ?? resolvedTarget.ValuePath);
        }

        if (string.IsNullOrWhiteSpace(resolvedTarget.ValuePath) &&
            linkedContext.ImageInspection.ObjectValue is WzImageCanvasInspection linkedRootCanvas)
        {
            return CanvasImageTarget.Linked(linkedContext, linkedRootCanvas, null);
        }

        await linkedContext.DisposeAsync();
        return null;
    }

    private static WzImagePropertyInspectionEntry? FindCanvasProperty(WzImageInspection inspection, string valuePath)
    {
        return inspection.Properties?
            .SelectMany(Flatten)
            .FirstOrDefault(property =>
                property.Value is WzImageCanvasInspection &&
                string.Equals(property.Path, valuePath, StringComparison.Ordinal)) ?? null;
    }

    private static bool IsCanvasLinkProperty(WzImagePropertyInspectionEntry property)
    {
        return property.Kind == "string" &&
            (string.Equals(property.Name, "source", StringComparison.Ordinal) ||
             string.Equals(property.Name, "_inlink", StringComparison.Ordinal) ||
             string.Equals(property.Name, "_outlink", StringComparison.Ordinal));
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

    private sealed class CanvasImageInspectionContext : IAsyncDisposable
    {
        private CanvasImageInspectionContext(
            string sourcePath,
            WzImageInspection imageInspection,
            Stream sourceStream)
        {
            SourcePath = sourcePath;
            ImageInspection = imageInspection;
            SourceStream = sourceStream;
        }

        public string SourcePath { get; }

        public WzImageInspection ImageInspection { get; }

        public Stream SourceStream { get; }

        public static async Task<CanvasImageInspectionContext> LoadAsync(
            string path,
            string selector,
            ResourceInspectionOptions options,
            CancellationToken cancellationToken)
        {
            if (MsMnContainerKind.IsPath(path))
            {
                var msContext = await WzMsImageInspectionLoader.LoadAsync(
                    path,
                    selector,
                    options.StringKey,
                    options.MaxPropertyDepth,
                    cancellationToken);
                return new CanvasImageInspectionContext(
                    msContext.ContainerInspection.Header.SourcePath,
                    msContext.ImageInspection,
                    msContext.PayloadStream);
            }

            var context = await WzImageInspectionLoader.LoadAsync(
                path,
                selector,
                options.StringKey,
                options.MaxPropertyDepth,
                cancellationToken);
            return new CanvasImageInspectionContext(
                context.DirectoryInspection.Header.SourcePath,
                context.ImageInspection,
                File.OpenRead(context.DirectoryInspection.Header.SourcePath));
        }

        public ValueTask DisposeAsync()
        {
            return SourceStream.DisposeAsync();
        }
    }

    private sealed class CanvasImageTarget : IAsyncDisposable
    {
        private readonly CanvasImageInspectionContext? ownedContext;

        private CanvasImageTarget(
            CanvasImageInspectionContext context,
            WzImageCanvasInspection value,
            string? path,
            bool ownsContext)
        {
            Context = context;
            Value = value;
            Path = path;
            ownedContext = ownsContext ? context : null;
        }

        public string SourcePath => Context.SourcePath;

        public string Selector => Context.ImageInspection.Selector;

        public Stream SourceStream => Context.SourceStream;

        public WzImageCanvasInspection Value { get; }

        public string? Path { get; }

        private CanvasImageInspectionContext Context { get; }

        public static CanvasImageTarget Local(
            CanvasImageInspectionContext context,
            WzImageCanvasInspection value,
            string? path)
        {
            return new CanvasImageTarget(context, value, path, ownsContext: false);
        }

        public static CanvasImageTarget Linked(
            CanvasImageInspectionContext context,
            WzImageCanvasInspection value,
            string? path)
        {
            return new CanvasImageTarget(context, value, path, ownsContext: true);
        }

        public ValueTask DisposeAsync()
        {
            return ownedContext?.DisposeAsync() ?? ValueTask.CompletedTask;
        }
    }

}
