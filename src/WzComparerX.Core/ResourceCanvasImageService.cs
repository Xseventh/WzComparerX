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
        ValidateCanvas(target.Value, target.Path);

        cancellationToken.ThrowIfCancellationRequested();
        await using (target)
        {
            WzImageCanvasBitmap bitmap;
            try
            {
                bitmap = new WzImageCanvasPayloadDecoder().Decode(target.SourceStream, target.Value);
            }
            catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException or NotSupportedException)
            {
                throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewDecodeFailed(target.Path));
            }

            var pixels = ConvertToBgra8888(bitmap, target.Path);
            return new ResourceCanvasImageDocument(
                target.SourcePath,
                target.Selector,
                target.Path,
                bitmap.Width,
                bitmap.Height,
                bitmap.Format,
                "bgra8888",
                pixels);
        }
    }

    private static void ValidateCanvas(WzImageCanvasInspection canvas, string? path)
    {
        if (canvas.CompressionKind != WzImageCanvasCompressionKind.Zlib)
        {
            throw new ResourceCanvasImageException(
                ResourceInspectionDiagnostics.CanvasPreviewCompressionUnsupported(canvas.CompressionKind, path));
        }

        if (canvas.Format is not 1 and not 2 and not 257 and not 513 and not 1026 and not 2050)
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
            257 => ConvertBgra1555ToBgra8888(bitmap, path),
            513 => ConvertBgr565ToBgra8888(bitmap, path),
            1026 => ConvertDxt3ToBgra8888(bitmap, path),
            2050 => ConvertDxt5ToBgra8888(bitmap, path),
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

    private static byte[] ConvertDxt3ToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var blocksWide = (bitmap.Width + 3) / 4;
        var blocksHigh = (bitmap.Height + 3) / 4;
        var source = TrimOrCopy(bitmap.Pixels, checked(blocksWide * blocksHigh * 16), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        Span<byte> alphaTable = stackalloc byte[16];
        Span<byte> colorTable = stackalloc byte[16];

        for (var blockY = 0; blockY < blocksHigh; blockY++)
        {
            for (var blockX = 0; blockX < blocksWide; blockX++)
            {
                var block = source.AsSpan(((blockY * blocksWide) + blockX) * 16, 16);
                BuildDxt3AlphaTable(block[..8], alphaTable);
                BuildDxtColorTable(block, colorTable);
                var colorBits = ReadUInt32LittleEndian(block[12..16]);

                for (var y = 0; y < 4; y++)
                {
                    var destinationY = (blockY * 4) + y;
                    if (destinationY >= bitmap.Height)
                    {
                        continue;
                    }

                    for (var x = 0; x < 4; x++)
                    {
                        var destinationX = (blockX * 4) + x;
                        if (destinationX >= bitmap.Width)
                        {
                            continue;
                        }

                        var blockPixel = (y * 4) + x;
                        var colorIndex = (int)((colorBits >> (blockPixel * 2)) & 0x03);
                        var colorOffset = colorIndex * 4;
                        var destinationOffset = ((destinationY * bitmap.Width) + destinationX) * 4;
                        destination[destinationOffset] = colorTable[colorOffset];
                        destination[destinationOffset + 1] = colorTable[colorOffset + 1];
                        destination[destinationOffset + 2] = colorTable[colorOffset + 2];
                        destination[destinationOffset + 3] = alphaTable[blockPixel];
                    }
                }
            }
        }

        return destination;
    }

    private static byte[] ConvertDxt5ToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var blocksWide = (bitmap.Width + 3) / 4;
        var blocksHigh = (bitmap.Height + 3) / 4;
        var source = TrimOrCopy(bitmap.Pixels, checked(blocksWide * blocksHigh * 16), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        Span<byte> alphaTable = stackalloc byte[8];
        Span<byte> colorTable = stackalloc byte[16];

        for (var blockY = 0; blockY < blocksHigh; blockY++)
        {
            for (var blockX = 0; blockX < blocksWide; blockX++)
            {
                var block = source.AsSpan(((blockY * blocksWide) + blockX) * 16, 16);
                BuildDxt5AlphaTable(block[0], block[1], alphaTable);
                BuildDxtColorTable(block, colorTable);
                var alphaBits = ReadUInt48LittleEndian(block[2..8]);
                var colorBits = ReadUInt32LittleEndian(block[12..16]);

                for (var y = 0; y < 4; y++)
                {
                    var destinationY = (blockY * 4) + y;
                    if (destinationY >= bitmap.Height)
                    {
                        continue;
                    }

                    for (var x = 0; x < 4; x++)
                    {
                        var destinationX = (blockX * 4) + x;
                        if (destinationX >= bitmap.Width)
                        {
                            continue;
                        }

                        var blockPixel = (y * 4) + x;
                        var alphaIndex = (int)((alphaBits >> (blockPixel * 3)) & 0x07);
                        var colorIndex = (int)((colorBits >> (blockPixel * 2)) & 0x03);
                        var colorOffset = colorIndex * 4;
                        var destinationOffset = ((destinationY * bitmap.Width) + destinationX) * 4;
                        destination[destinationOffset] = colorTable[colorOffset];
                        destination[destinationOffset + 1] = colorTable[colorOffset + 1];
                        destination[destinationOffset + 2] = colorTable[colorOffset + 2];
                        destination[destinationOffset + 3] = alphaTable[alphaIndex];
                    }
                }
            }
        }

        return destination;
    }

    private static void BuildDxt3AlphaTable(ReadOnlySpan<byte> block, Span<byte> alphaTable)
    {
        for (var i = 0; i < 16; i += 2)
        {
            var value = block[i / 2];
            var low = value & 0x0f;
            var high = value >> 4;
            alphaTable[i] = (byte)(low | (low << 4));
            alphaTable[i + 1] = (byte)(high | (high << 4));
        }
    }

    private static void BuildDxt5AlphaTable(byte alpha0, byte alpha1, Span<byte> alphaTable)
    {
        alphaTable[0] = alpha0;
        alphaTable[1] = alpha1;
        if (alpha0 > alpha1)
        {
            for (var i = 2; i < 8; i++)
            {
                alphaTable[i] = (byte)((((8 - i) * alpha0) + ((i - 1) * alpha1) + 3) / 7);
            }
        }
        else
        {
            for (var i = 2; i < 6; i++)
            {
                alphaTable[i] = (byte)((((6 - i) * alpha0) + ((i - 1) * alpha1) + 2) / 5);
            }

            alphaTable[6] = 0;
            alphaTable[7] = byte.MaxValue;
        }
    }

    private static void BuildDxtColorTable(ReadOnlySpan<byte> block, Span<byte> colorTable)
    {
        var color0 = ReadUInt16LittleEndian(block[8..10]);
        var color1 = ReadUInt16LittleEndian(block[10..12]);
        WriteRgb565AsBgra(color0, colorTable[0..4]);
        WriteRgb565AsBgra(color1, colorTable[4..8]);

        if (color0 > color1)
        {
            InterpolateBgra(colorTable[0..4], colorTable[4..8], colorTable[8..12], firstWeight: 2, secondWeight: 1, divisor: 3, bias: 1);
            InterpolateBgra(colorTable[0..4], colorTable[4..8], colorTable[12..16], firstWeight: 1, secondWeight: 2, divisor: 3, bias: 1);
        }
        else
        {
            InterpolateBgra(colorTable[0..4], colorTable[4..8], colorTable[8..12], firstWeight: 1, secondWeight: 1, divisor: 2, bias: 0);
            colorTable[12] = 0;
            colorTable[13] = 0;
            colorTable[14] = 0;
            colorTable[15] = byte.MaxValue;
        }
    }

    private static void WriteRgb565AsBgra(ushort value, Span<byte> destination)
    {
        destination[0] = Expand5To8(value & 0x1f);
        destination[1] = Expand6To8((value >> 5) & 0x3f);
        destination[2] = Expand5To8((value >> 11) & 0x1f);
        destination[3] = byte.MaxValue;
    }

    private static void InterpolateBgra(
        ReadOnlySpan<byte> first,
        ReadOnlySpan<byte> second,
        Span<byte> destination,
        int firstWeight,
        int secondWeight,
        int divisor,
        int bias)
    {
        for (var i = 0; i < 3; i++)
        {
            destination[i] = (byte)(((first[i] * firstWeight) + (second[i] * secondWeight) + bias) / divisor);
        }

        destination[3] = byte.MaxValue;
    }

    private static byte[] ConvertBgra1555ToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var source = TrimOrCopy(bitmap.Pixels, checked(bitmap.Width * bitmap.Height * 2), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        for (var sourceIndex = 0; sourceIndex < source.Length; sourceIndex += 2)
        {
            var value = source[sourceIndex] | (source[sourceIndex + 1] << 8);
            var destinationIndex = (sourceIndex / 2) * 4;
            destination[destinationIndex] = Expand5To8(value & 0x1f);
            destination[destinationIndex + 1] = Expand5To8((value >> 5) & 0x1f);
            destination[destinationIndex + 2] = Expand5To8((value >> 10) & 0x1f);
            destination[destinationIndex + 3] = (value & 0x8000) != 0 ? byte.MaxValue : (byte)0;
        }

        return destination;
    }

    private static byte[] ConvertBgr565ToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var source = TrimOrCopy(bitmap.Pixels, checked(bitmap.Width * bitmap.Height * 2), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        for (var sourceIndex = 0; sourceIndex < source.Length; sourceIndex += 2)
        {
            var value = source[sourceIndex] | (source[sourceIndex + 1] << 8);
            var destinationIndex = (sourceIndex / 2) * 4;
            destination[destinationIndex] = Expand5To8(value & 0x1f);
            destination[destinationIndex + 1] = Expand6To8((value >> 5) & 0x3f);
            destination[destinationIndex + 2] = Expand5To8((value >> 11) & 0x1f);
            destination[destinationIndex + 3] = byte.MaxValue;
        }

        return destination;
    }

    private static byte Expand5To8(int value)
    {
        return (byte)((value << 3) | (value >> 2));
    }

    private static byte Expand6To8(int value)
    {
        return (byte)((value << 2) | (value >> 4));
    }

    private static ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> bytes)
    {
        return (ushort)(bytes[0] | (bytes[1] << 8));
    }

    private static uint ReadUInt32LittleEndian(ReadOnlySpan<byte> bytes)
    {
        return (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
    }

    private static ulong ReadUInt48LittleEndian(ReadOnlySpan<byte> bytes)
    {
        ulong value = 0;
        for (var i = 0; i < 6; i++)
        {
            value |= (ulong)bytes[i] << (i * 8);
        }

        return value;
    }

    private static byte[] TrimOrCopy(byte[] source, int expectedLength, string? path)
    {
        if (source.Length < expectedLength)
        {
            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewDecodeFailed(path));
        }

        return source.Length == expectedLength ? source : source[..expectedLength];
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
            if (IsMsContainerPath(path))
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
                File.OpenRead(path));
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

    private static bool IsMsContainerPath(string path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".ms", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".mn", StringComparison.OrdinalIgnoreCase);
    }

}
