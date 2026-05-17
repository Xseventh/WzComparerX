using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using WzComparerX.App.ViewModels;

namespace WzComparerX.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        CanvasPreviewScrollViewer.PropertyChanged += (_, _) => UpdateCanvasPreviewViewport();
        DataContextChanged += (_, _) => UpdateCanvasPreviewViewport();
    }

    private async void OpenResourceFileMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open resource package",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Resource packages and fixtures")
                {
                    Patterns = ["*.wz", "*.ms", "*.mn", "*.json"],
                    MimeTypes = ["application/octet-stream", "application/json"],
                    AppleUniformTypeIdentifiers = ["public.data", "public.json"]
                },
                FilePickerFileTypes.All
            ]
        });

        var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.OpenPathAsync(path);
        }
    }

    private async void OpenResourceFolderMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open resource folder",
            AllowMultiple = false
        });

        var path = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.OpenPathAsync(path);
        }
    }

    private async void ResourcesTree_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel &&
            viewModel.ActivateSelectedNodeCommand.CanExecute(null))
        {
            await viewModel.ActivateSelectedNodeAsync();
        }
    }

    private void UpdateCanvasPreviewViewport()
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var bounds = CanvasPreviewScrollViewer.Bounds;
        viewModel.SetCanvasPreviewViewport(bounds.Width, bounds.Height);
    }
}
