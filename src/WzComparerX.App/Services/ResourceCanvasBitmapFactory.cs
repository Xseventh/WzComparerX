using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using WzComparerX.Core;

namespace WzComparerX.App.Services;

internal static class ResourceCanvasBitmapFactory
{
    public static WriteableBitmap Create(ResourceCanvasImageDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!string.Equals(document.PixelFormat, "bgra8888", StringComparison.Ordinal))
        {
            throw new NotSupportedException($"Unsupported canvas pixel format: {document.PixelFormat}.");
        }

        var bitmap = new WriteableBitmap(
            new PixelSize(document.Width, document.Height),
            new Vector(96, 96),
            PixelFormats.Bgra8888,
            AlphaFormat.Unpremul);
        using var framebuffer = bitmap.Lock();
        var sourceStride = document.Stride;
        for (var y = 0; y < document.Height; y++)
        {
            var sourceOffset = y * sourceStride;
            var destination = framebuffer.Address + (y * framebuffer.RowBytes);
            Marshal.Copy(document.Pixels, sourceOffset, destination, sourceStride);
        }

        return bitmap;
    }
}
