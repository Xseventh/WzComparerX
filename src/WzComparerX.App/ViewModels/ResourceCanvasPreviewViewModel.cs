using Avalonia.Media.Imaging;
using WzComparerX.Core;

namespace WzComparerX.App.ViewModels;

public sealed class ResourceCanvasPreviewViewModel : ViewModelBase, IDisposable
{
    private int? manualScale;

    public ResourceCanvasPreviewViewModel(ResourceCanvasImageDocument document, Bitmap? bitmap)
    {
        SourcePath = document.SourcePath;
        Selector = document.Selector;
        ValuePath = document.ValuePath;
        Width = document.Width;
        Height = document.Height;
        Format = document.Format;
        Bitmap = bitmap;
        AutoScale = CalculateAutoScale(document.Width, document.Height);
    }

    public string SourcePath { get; }

    public string Selector { get; }

    public string? ValuePath { get; }

    public int Width { get; }

    public int Height { get; }

    public int Format { get; }

    public int AutoScale { get; }

    public int Scale => manualScale ?? AutoScale;

    public double DisplayWidth => Width * Scale;

    public double DisplayHeight => Height * Scale;

    public string ScaleLabel => manualScale is null ? $"Auto ({Scale}x)" : $"{Scale}x";

    public Bitmap? Bitmap { get; }

    public string Title => ValuePath is null
        ? $"{Selector} ({Width}x{Height})"
        : $"{Selector}/{ValuePath} ({Width}x{Height})";

    public void Dispose()
    {
        Bitmap?.Dispose();
    }

    public void SetScale(int? scale)
    {
        manualScale = scale is null ? null : Math.Clamp(scale.Value, 1, 16);
        OnPropertyChanged(nameof(Scale));
        OnPropertyChanged(nameof(DisplayWidth));
        OnPropertyChanged(nameof(DisplayHeight));
        OnPropertyChanged(nameof(ScaleLabel));
    }

    private static int CalculateAutoScale(int width, int height)
    {
        var longestSide = Math.Max(width, height);
        if (longestSide <= 0)
        {
            return 1;
        }

        const int targetLongestSide = 320;
        const int maxScale = 16;
        return Math.Clamp(targetLongestSide / longestSide, 1, maxScale);
    }
}
