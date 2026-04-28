using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using WzComparerX.App.ViewModels;

namespace WzComparerX.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void BrowsePathButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open resource package",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("WZ packages and fixtures")
                {
                    Patterns = ["*.wz", "*.json"],
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
}
