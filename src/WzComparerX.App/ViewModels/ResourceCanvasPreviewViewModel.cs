using System.Globalization;
using Avalonia.Media.Imaging;
using WzComparerX.Core;

namespace WzComparerX.App.ViewModels;

public sealed class ResourceCanvasPreviewViewModel : ViewModelBase, IDisposable
{
    private const int MaxScale = 16;
    private const double ViewportFillRatio = 0.9;

    private int? manualScale;
    private double autoScale;

    public ResourceCanvasPreviewViewModel(ResourceCanvasImageDocument document, Bitmap? bitmap)
    {
        SourcePath = document.SourcePath;
        Selector = document.Selector;
        ValuePath = document.ValuePath;
        Width = document.Width;
        Height = document.Height;
        Format = document.Format;
        Bitmap = bitmap;
        autoScale = CalculateFallbackAutoScale(document.Width, document.Height);
    }

    public string SourcePath { get; }

    public string Selector { get; }

    public string? ValuePath { get; }

    public int Width { get; }

    public int Height { get; }

    public int Format { get; }

    public double AutoScale => autoScale;

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
        manualScale = scale is null ? null : Math.Clamp(scale.Value, 1, MaxScale);
        OnPropertyChanged(nameof(Scale));
        OnPropertyChanged(nameof(DisplayWidth));
        OnPropertyChanged(nameof(DisplayHeight));
        OnPropertyChanged(nameof(ScaleLabel));
    }

    public void SetViewportSize(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var nextAutoScale = CalculateViewportAutoScale(Width, Height, width, height);
        if (Math.Abs(nextAutoScale - autoScale) < 0.0001)
        {
            return;
        }

        autoScale = nextAutoScale;
        OnPropertyChanged(nameof(AutoScale));
        if (manualScale is null)
        {
            OnPropertyChanged(nameof(Scale));
            OnPropertyChanged(nameof(DisplayWidth));
            OnPropertyChanged(nameof(DisplayHeight));
            OnPropertyChanged(nameof(ScaleLabel));
        }
    }

    private static double CalculateFallbackAutoScale(int width, int height)
    {
        var longestSide = Math.Max(width, height);
        if (longestSide <= 0)
        {
            return 1;
        }

        const int targetSmallLongestSide = 320;
        const int shrinkThresholdLongestSide = 1024;
        const int targetLargeLongestSide = 960;
        if (longestSide > shrinkThresholdLongestSide)
        {
            return (double)targetLargeLongestSide / longestSide;
        }

        var integerScale = Math.Floor((double)targetSmallLongestSide / longestSide);
        return Math.Clamp(integerScale, 1, MaxScale);
    }

    private static double CalculateViewportAutoScale(int imageWidth, int imageHeight, double viewportWidth, double viewportHeight)
    {
        if (imageWidth <= 0 || imageHeight <= 0)
        {
            return 1;
        }

        var targetWidth = Math.Max(1, viewportWidth * ViewportFillRatio);
        var targetHeight = Math.Max(1, viewportHeight * ViewportFillRatio);
        var fitScale = Math.Min(targetWidth / imageWidth, targetHeight / imageHeight);
        if (fitScale >= 1)
        {
            return Math.Clamp(Math.Floor(fitScale), 1, MaxScale);
        }

        return Math.Clamp(fitScale, 0.01, 1);
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
