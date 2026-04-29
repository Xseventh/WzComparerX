using System.Globalization;
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

    public double AutoScale { get; }

    public double Scale => manualScale ?? AutoScale;

    public double DisplayWidth => Width * Scale;

    public double DisplayHeight => Height * Scale;

    public string ScaleLabel => manualScale is null ? $"Auto ({FormatScale(Scale)})" : $"{Scale:0}x";

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

    private static double CalculateAutoScale(int width, int height)
    {
        var longestSide = Math.Max(width, height);
        if (longestSide <= 0)
        {
            return 1;
        }

        const int targetSmallLongestSide = 320;
        const int shrinkThresholdLongestSide = 1024;
        const int targetLargeLongestSide = 960;
        const int maxScale = 16;
        if (longestSide > shrinkThresholdLongestSide)
        {
            return (double)targetLargeLongestSide / longestSide;
        }

        var integerScale = Math.Floor((double)targetSmallLongestSide / longestSide);
        return Math.Clamp(integerScale, 1, maxScale);
    }

    private static string FormatScale(double scale)
    {
        if (scale >= 1)
        {
            return scale.ToString("0", CultureInfo.InvariantCulture) + "x";
        }

        var percent = Math.Max(1, (int)Math.Round(scale * 100, MidpointRounding.AwayFromZero));
        return percent.ToString(CultureInfo.InvariantCulture) + "%";
    }
}
