using Avalonia.Media.Imaging;
using WzComparerX.Core;

namespace WzComparerX.App.ViewModels;

public sealed class ResourceCanvasPreviewViewModel : ResourceBitmapPreviewViewModel
{
    public ResourceCanvasPreviewViewModel(
        ResourceCanvasImageDocument document,
        Bitmap? bitmap,
        double initialScale = DefaultScale)
        : base(
            document.SourcePath,
            document.Selector,
            document.ValuePath,
            document.Width,
            document.Height,
            document.Format,
            bitmap,
            initialScale)
    {
    }

    public override string Title => ValuePath is null
        ? $"{Selector} ({Width}x{Height})"
        : $"{Selector}/{ValuePath} ({Width}x{Height})";
}
