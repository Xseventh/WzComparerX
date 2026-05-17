using System.Globalization;
using Avalonia.Media.Imaging;
using WzComparerX.Core;

namespace WzComparerX.App.ViewModels;

public sealed class ResourceCanvasPreviewViewModel : ViewModelBase, IDisposable
{
    public const double DefaultScale = 1;
    public const double MinScale = 0.01;
    public const double MaxScale = 16;

    private const double ViewportFillRatio = 0.9;

    private double scale;

    public ResourceCanvasPreviewViewModel(
        ResourceCanvasImageDocument document,
        Bitmap? bitmap,
        double initialScale = DefaultScale)
    {
        SourcePath = document.SourcePath;
        Selector = document.Selector;
        ValuePath = document.ValuePath;
        Width = document.Width;
        Height = document.Height;
        Format = document.Format;
        Bitmap = bitmap;
        scale = NormalizeScale(initialScale);
    }

    public string SourcePath { get; }

    public string Selector { get; }

    public string? ValuePath { get; }

    public int Width { get; }

    public int Height { get; }

    public int Format { get; }

    public double Scale => scale;

    public double DisplayWidth => Width * Scale;

    public double DisplayHeight => Height * Scale;

    public string ScaleLabel => FormatScale(Scale);

    public Bitmap? Bitmap { get; }

    public string Title => ValuePath is null
        ? $"{Selector} ({Width}x{Height})"
        : $"{Selector}/{ValuePath} ({Width}x{Height})";

    public void Dispose()
    {
        Bitmap?.Dispose();
    }

    public void SetScale(double scale)
    {
        this.scale = NormalizeScale(scale);
        OnPropertyChanged(nameof(Scale));
        OnPropertyChanged(nameof(DisplayWidth));
        OnPropertyChanged(nameof(DisplayHeight));
        OnPropertyChanged(nameof(ScaleLabel));
    }

    public double CalculateViewportFitScale(double viewportWidth, double viewportHeight)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            return DefaultScale;
        }

        return CalculateViewportFitScale(Width, Height, viewportWidth, viewportHeight);
    }

    public static double CalculateViewportFitScale(
        int imageWidth,
        int imageHeight,
        double viewportWidth,
        double viewportHeight)
    {
        if (imageWidth <= 0 || imageHeight <= 0 || viewportWidth <= 0 || viewportHeight <= 0)
        {
            return DefaultScale;
        }

        var targetWidth = Math.Max(1, viewportWidth * ViewportFillRatio);
        var targetHeight = Math.Max(1, viewportHeight * ViewportFillRatio);
        var fitScale = Math.Min(targetWidth / imageWidth, targetHeight / imageHeight);
        if (fitScale >= 1)
        {
            return NormalizeScale(Math.Floor(fitScale));
        }

        return NormalizeScale(fitScale);
    }

    private static double NormalizeScale(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return DefaultScale;
        }

        return Math.Clamp(value, MinScale, MaxScale);
    }

    private static string FormatScale(double scale)
    {
        if (Math.Abs(scale - 0.25) < 0.0001)
        {
            return "0.25x";
        }

        if (Math.Abs(scale - 0.5) < 0.0001)
        {
            return "0.5x";
        }

        if (scale >= 1)
        {
            return scale.ToString("0.##", CultureInfo.InvariantCulture) + "x";
        }

        var percent = Math.Max(1, (int)Math.Round(scale * 100, MidpointRounding.AwayFromZero));
        return percent.ToString(CultureInfo.InvariantCulture) + "%";
    }
}
