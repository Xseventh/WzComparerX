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
        var context = await WzImageInspectionLoader.LoadAsync(
            path,
            selector,
            options.StringKey,
            options.MaxPropertyDepth,
            cancellationToken);
        var target = SelectCanvas(
            context.ImageInspection,
            selector,
            valueSelector,
            useFirstCanvasFallback);
        ValidateCanvas(target.Value, target.Path);

        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = File.OpenRead(path);
        WzImageCanvasBitmap bitmap;
        try
        {
            bitmap = new WzImageCanvasPayloadDecoder().Decode(stream, target.Value);
        }
        catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException or NotSupportedException)
        {
            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewDecodeFailed(target.Path));
        }

        var pixels = ConvertToBgra8888(bitmap, target.Path);
        return new ResourceCanvasImageDocument(
            context.ImageInspection.Header.SourcePath,
            context.ImageInspection.Selector,
            target.Path,
            bitmap.Width,
            bitmap.Height,
            bitmap.Format,
            "bgra8888",
            pixels);
    }

    private static void ValidateCanvas(WzImageCanvasInspection canvas, string? path)
    {
        if (canvas.CompressionKind != WzImageCanvasCompressionKind.Zlib)
        {
            throw new ResourceCanvasImageException(
                ResourceInspectionDiagnostics.CanvasPreviewCompressionUnsupported(canvas.CompressionKind, path));
        }

        if (canvas.Format is not 1 and not 2)
        {
            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewFormatUnsupported(canvas.Format, path));
        }

        if (canvas.ActualScale != 1)
        {
            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewScaleUnsupported(canvas.Scale, path));
        }
    }

    private static byte[] ConvertToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        return bitmap.Format switch
        {
            1 => ConvertBgra4444ToBgra8888(bitmap, path),
            2 => TrimOrCopy(bitmap.Pixels, checked(bitmap.Width * bitmap.Height * 4), path),
            _ => throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewFormatUnsupported(bitmap.Format, path))
        };
    }

    private static byte[] ConvertBgra4444ToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var source = TrimOrCopy(bitmap.Pixels, checked(bitmap.Width * bitmap.Height * 2), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        for (var i = 0; i < source.Length; i++)
        {
            var value = source[i];
            var low = value & 0x0f;
            var high = value & 0xf0;
            destination[i * 2] = (byte)(low | (low << 4));
            destination[(i * 2) + 1] = (byte)(high | (high >> 4));
        }

        return destination;
    }

    private static byte[] TrimOrCopy(byte[] source, int expectedLength, string? path)
    {
        if (source.Length < expectedLength)
        {
            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewDecodeFailed(path));
        }

        return source.Length == expectedLength ? source : source[..expectedLength];
    }

    private static CanvasImageTarget SelectCanvas(
        WzImageInspection inspection,
        string? selector,
        string? valueSelector,
        bool useFirstCanvasFallback)
    {
        if (string.IsNullOrWhiteSpace(valueSelector))
        {
            if (inspection.ObjectValue is WzImageCanvasInspection rootCanvas)
            {
                return new CanvasImageTarget(rootCanvas, null);
            }

            if (useFirstCanvasFallback)
            {
                var firstCanvas = inspection.Properties?
                    .SelectMany(Flatten)
                    .FirstOrDefault(property => property.Value is WzImageCanvasInspection);
                if (firstCanvas?.Value is WzImageCanvasInspection firstCanvasValue)
                {
                    return new CanvasImageTarget(firstCanvasValue, firstCanvas.Path);
                }
            }

            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewValueRequired(selector));
        }

        var matches = inspection.Properties?
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
        return match.Value is WzImageCanvasInspection canvas
            ? new CanvasImageTarget(canvas, match.Path ?? valueSelector)
            : throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewValueUnsupported(valueSelector, selector));
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

    private sealed record CanvasImageTarget(WzImageCanvasInspection Value, string? Path);
}
