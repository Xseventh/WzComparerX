using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class ResourceCanvasImageService
{
    private const int MaxLinkResolutionDepth = 16;
    private static readonly string[] CanvasLinkPropertyOrder = ["source", "_inlink", "_outlink", "link"];

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
            CanvasImageDiagnosticSurface.Preview,
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
            CanvasImageDiagnosticSurface.Preview,
            cancellationToken);
    }

    public async Task<ResourceCanvasImageDocument> LoadForExportAsync(
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
            CanvasImageDiagnosticSurface.Export,
            cancellationToken);
    }

    private static async Task<ResourceCanvasImageDocument> LoadCoreAsync(
        string path,
        string selector,
        string? valueSelector,
        bool useFirstCanvasFallback,
        ResourceInspectionOptions? options,
        CanvasImageDiagnosticSurface diagnosticSurface,
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
            diagnosticSurface,
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
                throw new ResourceCanvasImageException(ToDiagnostic(ex, diagnosticSurface));
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

    private static ResourceInspectionDiagnostic ToDiagnostic(
        WzImageCanvasBitmapDecodeException exception,
        CanvasImageDiagnosticSurface diagnosticSurface)
    {
        return exception.Kind switch
        {
            WzImageCanvasBitmapDecodeFailureKind.CompressionUnsupported =>
                diagnosticSurface == CanvasImageDiagnosticSurface.Export
                    ? ResourceInspectionDiagnostics.ExportCanvasCompressionUnsupported(exception.CompressionKind!.Value, exception.Path)
                    : ResourceInspectionDiagnostics.CanvasPreviewCompressionUnsupported(exception.CompressionKind!.Value, exception.Path),
            WzImageCanvasBitmapDecodeFailureKind.FormatUnsupported =>
                diagnosticSurface == CanvasImageDiagnosticSurface.Export
                    ? ResourceInspectionDiagnostics.ExportCanvasFormatUnsupported(exception.Format!.Value, exception.Path)
                    : ResourceInspectionDiagnostics.CanvasPreviewFormatUnsupported(exception.Format!.Value, exception.Path),
            WzImageCanvasBitmapDecodeFailureKind.ScaleUnsupported =>
                diagnosticSurface == CanvasImageDiagnosticSurface.Export
                    ? ResourceInspectionDiagnostics.ExportCanvasScaleUnsupported(exception.Scale!.Value, exception.Path)
                    : ResourceInspectionDiagnostics.CanvasPreviewScaleUnsupported(exception.Scale!.Value, exception.Path),
            _ => diagnosticSurface == CanvasImageDiagnosticSurface.Export
                ? ResourceInspectionDiagnostics.ExportCanvasDecodeFailed(exception.Path)
                : ResourceInspectionDiagnostics.CanvasPreviewDecodeFailed(exception.Path)
        };
    }

    private static async Task<CanvasImageTarget> SelectCanvasAsync(
        CanvasImageInspectionContext context,
        string? selector,
        string? valueSelector,
        bool useFirstCanvasFallback,
        ResourceInspectionOptions options,
        CanvasImageDiagnosticSurface diagnosticSurface,
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

            throw new ResourceCanvasImageException(ValueRequired(diagnosticSurface, selector));
        }

        var matches = context.ImageInspection.Properties?
            .SelectMany(Flatten)
            .Where(property => string.Equals(property.Path, valueSelector, StringComparison.Ordinal))
            .ToArray() ?? [];

        if (matches.Length == 0)
        {
            throw new ResourceCanvasImageException(ValueNotFound(diagnosticSurface, valueSelector, selector));
        }

        if (matches.Length > 1)
        {
            throw new ResourceCanvasImageException(ValueAmbiguous(diagnosticSurface, valueSelector, selector));
        }

        var match = matches[0];
        if (match.Value is WzImageCanvasInspection canvas)
        {
            return CanvasImageTarget.Local(context, canvas, match.Path ?? valueSelector);
        }

        if (IsCanvasLinkProperty(match))
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
                LinkUnresolved(
                    diagnosticSurface,
                    valueSelector,
                    selector,
                    ResourceInspectionLinkResolver.GetLinkKind(match) ?? match.Name ?? "link",
                    ResourceInspectionLinkResolver.GetLinkedTarget(match) ?? string.Empty));
        }

        throw new ResourceCanvasImageException(ValueUnsupported(diagnosticSurface, valueSelector, selector));
    }

    private static ResourceInspectionDiagnostic ValueRequired(
        CanvasImageDiagnosticSurface diagnosticSurface,
        string? selector)
    {
        return diagnosticSurface == CanvasImageDiagnosticSurface.Export
            ? ResourceInspectionDiagnostics.ExportValueRequired(ResourceExportKind.Canvas, selector)
            : ResourceInspectionDiagnostics.CanvasPreviewValueRequired(selector);
    }

    private static ResourceInspectionDiagnostic ValueNotFound(
        CanvasImageDiagnosticSurface diagnosticSurface,
        string valueSelector,
        string? selector)
    {
        return diagnosticSurface == CanvasImageDiagnosticSurface.Export
            ? ResourceInspectionDiagnostics.ExportValueNotFound(valueSelector, selector)
            : ResourceInspectionDiagnostics.CanvasPreviewValueNotFound(valueSelector, selector);
    }

    private static ResourceInspectionDiagnostic ValueAmbiguous(
        CanvasImageDiagnosticSurface diagnosticSurface,
        string valueSelector,
        string? selector)
    {
        return diagnosticSurface == CanvasImageDiagnosticSurface.Export
            ? ResourceInspectionDiagnostics.ExportValueAmbiguous(valueSelector, selector)
            : ResourceInspectionDiagnostics.CanvasPreviewValueAmbiguous(valueSelector, selector);
    }

    private static ResourceInspectionDiagnostic ValueUnsupported(
        CanvasImageDiagnosticSurface diagnosticSurface,
        string valueSelector,
        string? selector)
    {
        return diagnosticSurface == CanvasImageDiagnosticSurface.Export
            ? ResourceInspectionDiagnostics.ExportValueUnsupported(ResourceExportKind.Canvas, valueSelector, selector)
            : ResourceInspectionDiagnostics.CanvasPreviewValueUnsupported(valueSelector, selector);
    }

    private static ResourceInspectionDiagnostic LinkUnresolved(
        CanvasImageDiagnosticSurface diagnosticSurface,
        string valueSelector,
        string? selector,
        string linkKind,
        string linkedTarget)
    {
        return diagnosticSurface == CanvasImageDiagnosticSurface.Export
            ? ResourceInspectionDiagnostics.ExportValueLinkUnresolved(valueSelector, selector, linkKind, linkedTarget)
            : ResourceInspectionDiagnostics.CanvasPreviewLinkUnresolved(valueSelector, selector, linkKind, linkedTarget);
    }

    private static async Task<CanvasImageTarget?> ResolveCanvasLinkAsync(
        CanvasImageInspectionContext context,
        WzImagePropertyInspectionEntry linkProperty,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken)
    {
        return await ResolveCanvasLinkAsync(
            context,
            linkProperty,
            options,
            cancellationToken,
            new HashSet<string>(StringComparer.Ordinal),
            depth: 0,
            contextIsOwned: false);
    }

    private static async Task<CanvasImageTarget?> ResolveCanvasLinkAsync(
        CanvasImageInspectionContext context,
        WzImagePropertyInspectionEntry linkProperty,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken,
        HashSet<string> visited,
        int depth,
        bool contextIsOwned)
    {
        if (depth >= MaxLinkResolutionDepth)
        {
            return null;
        }

        var linkKind = ResourceInspectionLinkResolver.GetLinkKind(linkProperty);
        var linkedTarget = ResourceInspectionLinkResolver.GetLinkedTarget(linkProperty);
        if (string.IsNullOrWhiteSpace(linkKind) || string.IsNullOrWhiteSpace(linkedTarget))
        {
            return null;
        }

        var visitKey = string.Join(
            '\0',
            context.SourcePath,
            context.ImageInspection.Selector,
            linkProperty.Path,
            linkKind,
            linkedTarget);
        if (!visited.Add(visitKey))
        {
            return null;
        }

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

        return await ResolveCanvasTargetAsync(
            context,
            resolvedTarget,
            options,
            cancellationToken,
            visited,
            depth + 1,
            contextIsOwned);
    }

    private static async Task<CanvasImageTarget?> ResolveCanvasTargetAsync(
        CanvasImageInspectionContext currentContext,
        ResourceInspectionResolvedLinkTarget resolvedTarget,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken,
        HashSet<string> visited,
        int depth,
        bool currentContextIsOwned)
    {
        if (string.Equals(resolvedTarget.PackagePath, currentContext.SourcePath, StringComparison.Ordinal) &&
            string.Equals(resolvedTarget.ImageSelector, currentContext.ImageInspection.Selector, StringComparison.Ordinal))
        {
            return await ResolveCanvasInContextAsync(
                currentContext,
                resolvedTarget.ValuePath,
                options,
                cancellationToken,
                visited,
                depth,
                currentContextIsOwned);
        }

        var linkedContext = await CanvasImageInspectionContext.LoadAsync(
            resolvedTarget.PackagePath,
            resolvedTarget.ImageSelector,
            options,
            cancellationToken);

        try
        {
            var linkedTarget = await ResolveCanvasInContextAsync(
                linkedContext,
                resolvedTarget.ValuePath,
                options,
                cancellationToken,
                visited,
                depth,
                contextIsOwned: true);

            if (linkedTarget is null || !linkedTarget.UsesContext(linkedContext))
            {
                await linkedContext.DisposeAsync();
            }

            return linkedTarget;
        }
        catch
        {
            await linkedContext.DisposeAsync();
            throw;
        }
    }

    private static async Task<CanvasImageTarget?> ResolveCanvasInContextAsync(
        CanvasImageInspectionContext context,
        string? valuePath,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken,
        HashSet<string> visited,
        int depth,
        bool contextIsOwned)
    {
        if (string.IsNullOrWhiteSpace(valuePath))
        {
            return context.ImageInspection.ObjectValue is WzImageCanvasInspection rootCanvas
                ? CanvasImageTarget.Create(context, rootCanvas, null, contextIsOwned)
                : null;
        }

        var property = FindProperty(context.ImageInspection, valuePath);
        if (property is null)
        {
            return null;
        }

        if (IsCanvasLinkProperty(property))
        {
            return await ResolveCanvasLinkAsync(
                context,
                property,
                options,
                cancellationToken,
                visited,
                depth + 1,
                contextIsOwned);
        }

        if (property.Value is not WzImageCanvasInspection canvas)
        {
            return null;
        }

        var childLink = FindCanvasChildLinkProperty(property);
        if (childLink is not null)
        {
            var linkedTarget = await ResolveCanvasLinkAsync(
                context,
                childLink,
                options,
                cancellationToken,
                visited,
                depth + 1,
                contextIsOwned);
            if (linkedTarget is not null)
            {
                return linkedTarget;
            }
        }

        return CanvasImageTarget.Create(context, canvas, property.Path ?? valuePath, contextIsOwned);
    }

    private static WzImagePropertyInspectionEntry? FindProperty(WzImageInspection inspection, string valuePath)
    {
        return inspection.Properties?
            .SelectMany(Flatten)
            .FirstOrDefault(property =>
                string.Equals(property.Path, valuePath, StringComparison.Ordinal)) ?? null;
    }

    private static WzImagePropertyInspectionEntry? FindCanvasChildLinkProperty(WzImagePropertyInspectionEntry canvasProperty)
    {
        if (canvasProperty.Children is null)
        {
            return null;
        }

        foreach (var linkName in CanvasLinkPropertyOrder)
        {
            var linkProperty = canvasProperty.Children.FirstOrDefault(child =>
                string.Equals(child.Name, linkName, StringComparison.Ordinal) &&
                IsCanvasLinkProperty(child));
            if (linkProperty is not null)
            {
                return linkProperty;
            }
        }

        return null;
    }

    private static bool IsCanvasLinkProperty(WzImagePropertyInspectionEntry property)
    {
        return ResourceInspectionLinkResolver.GetLinkKind(property) is not null;
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

        public static CanvasImageTarget Create(
            CanvasImageInspectionContext context,
            WzImageCanvasInspection value,
            string? path,
            bool ownsContext)
        {
            return new CanvasImageTarget(context, value, path, ownsContext);
        }

        public bool UsesContext(CanvasImageInspectionContext context)
        {
            return ReferenceEquals(Context, context);
        }

        public ValueTask DisposeAsync()
        {
            return ownedContext?.DisposeAsync() ?? ValueTask.CompletedTask;
        }
    }

    private enum CanvasImageDiagnosticSurface
    {
        Preview,
        Export
    }
}
