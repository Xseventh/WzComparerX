using Avalonia.Media.Imaging;
using WzComparerX.Core;

namespace WzComparerX.App.ViewModels;

public sealed class ResourceCanvasPreviewViewModel : IDisposable
{
    public ResourceCanvasPreviewViewModel(ResourceCanvasImageDocument document, Bitmap? bitmap)
    {
        SourcePath = document.SourcePath;
        Selector = document.Selector;
        ValuePath = document.ValuePath;
        Width = document.Width;
        Height = document.Height;
        Format = document.Format;
        Bitmap = bitmap;
        Scale = CalculateScale(document.Width, document.Height);
    }

    public string SourcePath { get; }

    public string Selector { get; }

    public string? ValuePath { get; }

    public int Width { get; }

    public int Height { get; }

    public int Format { get; }

    public int Scale { get; }

    public double DisplayWidth => Width * Scale;

    public double DisplayHeight => Height * Scale;

    public Bitmap? Bitmap { get; }

    public string Title => ValuePath is null
        ? $"{Selector} ({Width}x{Height})"
        : $"{Selector}/{ValuePath} ({Width}x{Height})";

    public void Dispose()
    {
        Bitmap?.Dispose();
    }

    private static int CalculateScale(int width, int height)
    {
        var longestSide = Math.Max(width, height);
        if (longestSide <= 0)
        {
            return 1;
        }

        const int targetLongestSide = 160;
        const int maxScale = 16;
        return Math.Clamp(targetLongestSide / longestSide, 1, maxScale);
    }
}
