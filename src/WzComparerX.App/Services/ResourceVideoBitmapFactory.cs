using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using WzComparerX.Rendering;

namespace WzComparerX.App.Services;

internal static class ResourceVideoBitmapFactory
{
    public static WriteableBitmap Create(ResourceVideoSequenceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.PixelFormat != WzVideoPixelFormat.Bgra8888)
        {
            throw new NotSupportedException($"Unsupported video pixel format: {document.PixelFormat}.");
        }

        var bitmap = new WriteableBitmap(
            new PixelSize(document.Width, document.Height),
            new Vector(96, 96),
            PixelFormats.Bgra8888,
            AlphaFormat.Unpremul);

        if (document.Frames.Count > 0)
        {
            CopyFrame(bitmap, document.Frames[0]);
        }

        return bitmap;
    }

    public static void CopyFrame(WriteableBitmap bitmap, WzDecodedVideoFrame frame)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.PixelFormat != WzVideoPixelFormat.Bgra8888)
        {
            throw new NotSupportedException($"Unsupported video frame pixel format: {frame.PixelFormat}.");
        }

        using var framebuffer = bitmap.Lock();
        for (var y = 0; y < frame.Height; y++)
        {
            var sourceOffset = y * frame.Stride;
            var destination = framebuffer.Address + (y * framebuffer.RowBytes);
            Marshal.Copy(frame.Pixels, sourceOffset, destination, frame.Width * 4);
        }
    }
}
