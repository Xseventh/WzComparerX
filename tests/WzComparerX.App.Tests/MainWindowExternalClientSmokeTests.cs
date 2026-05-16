using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using WzComparerX.App.ViewModels;
using WzComparerX.App.Views;
using WzComparerX.Tests;

namespace WzComparerX.App.Tests;

public class MainWindowExternalClientSmokeTests
{
    [AvaloniaFact]
    public async Task MainWindow_OptionalExternalClientMapPackageGroupSmoke()
    {
        var map1Path = ExternalClientSmokeData.FindFirstFile("Map", "Map", "Map1", "Map1.wz");
        if (map1Path is null)
        {
            return;
        }

        var viewModel = new MainWindowViewModel
        {
            KeyText = "auto"
        };

        await viewModel.OpenPathAsync(map1Path);
        var window = CreateWindow(viewModel, width: 1280, height: 800);

        try
        {
            var root = Assert.Single(viewModel.RootNodes);
            Assert.Equal("Map1.wz", root.Name);
            Assert.Equal("package", root.Kind);
            Assert.Contains(root.Children, child => child.Name == "_Canvas");
            var mergedImage = Assert.Single(root.Children, child => child.Name == "100000000.img");
            Assert.EndsWith("Map1_000.wz/100000000.img", mergedImage.Path, StringComparison.Ordinal);

            using (var frame = CaptureFrame(window))
            {
                SaveScreenshotArtifact(frame, "gms-map1-package-group.png");
                AssertPngCanBeSaved(frame);
            }

            viewModel.SelectedNode = mergedImage;
            await WaitForImageContentAsync(viewModel);
            Assert.Equal("100000000.img", Assert.Single(viewModel.ImageContentNodes).Name);
            Assert.Contains(Flatten(Assert.Single(viewModel.ImageContentNodes)), node => node.Name == "miniMap");

            using (var frame = CaptureFrame(window))
            {
                SaveScreenshotArtifact(frame, "gms-map1-img-content.png");
                AssertPngCanBeSaved(frame);
            }

            var miniMap = Flatten(Assert.Single(viewModel.ImageContentNodes))
                .First(node => node.Name == "miniMap");
            var canvasLink = Flatten(miniMap)
                .First(node => node.Name == "_outlink");
            Assert.Equal("string", canvasLink.Kind);
            Assert.Contains("Map/Map/Map1/_Canvas/100000000.img", canvasLink.DisplayValue);
            var placeholderCanvas = Flatten(miniMap)
                .First(node => node.Kind == "canvas");
            Assert.Equal(1, int.Parse(placeholderCanvas.DisplayValue?.Split('x')[0] ?? "0", CultureInfo.InvariantCulture));

            viewModel.SelectedImageContentNode = canvasLink;
            await WaitForCanvasPreviewAsync(viewModel);

            var tabControl = window.FindControl<TabControl>("DetailsTabControl");
            Assert.NotNull(tabControl);
            tabControl.SelectedIndex = 2;

            using (var frame = CaptureFrame(window))
            {
                SaveScreenshotArtifact(frame, "gms-map1-canvas-preview.png");
                AssertPngCanBeSaved(frame);
            }

            Assert.True(viewModel.CanvasPreview?.Width > 1);
            Assert.True(viewModel.CanvasPreview?.Height > 1);
            Assert.Equal("100000000.img", viewModel.CanvasPreview?.Selector);
        }
        finally
        {
            viewModel.CanvasPreview?.Dispose();
            window.Close();
        }
    }

    private static MainWindow CreateWindow(
        MainWindowViewModel viewModel,
        double width,
        double height)
    {
        var window = new MainWindow
        {
            DataContext = viewModel,
            Width = width,
            Height = height
        };

        window.Show();
        return window;
    }

    private static Bitmap CaptureFrame(MainWindow window)
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);
        Dispatcher.UIThread.RunJobs();
        var frame = window.CaptureRenderedFrame();
        return Assert.IsAssignableFrom<Bitmap>(frame);
    }

    private static void AssertPngCanBeSaved(Bitmap frame)
    {
        using var stream = new MemoryStream();
        frame.Save(stream);
        var bytes = stream.ToArray();
        Assert.True(bytes.Length > 1024);
        Assert.Equal([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a], bytes[..8]);
    }

    private static void SaveScreenshotArtifact(Bitmap frame, string fileName)
    {
        var directory = Environment.GetEnvironmentVariable("WCX_EXTERNAL_CLIENT_UI_SMOKE_SCREENSHOT_DIR") ??
            Environment.GetEnvironmentVariable("WCX_GMS_UI_SMOKE_SCREENSHOT_DIR") ??
            Environment.GetEnvironmentVariable("WCX_HEADLESS_SCREENSHOT_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        frame.Save(Path.Combine(directory, fileName));
    }

    private static IEnumerable<ResourceInspectionNodeViewModel> Flatten(ResourceInspectionNodeViewModel node)
    {
        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var descendant in Flatten(child))
            {
                yield return descendant;
            }
        }
    }

    private static async Task WaitForImageContentAsync(MainWindowViewModel viewModel)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (viewModel.HasImageContent)
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.Fail("IMG content was not loaded.");
    }

    private static async Task WaitForCanvasPreviewAsync(MainWindowViewModel viewModel)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (viewModel.HasCanvasPreview)
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.Fail("Canvas preview was not loaded.");
    }
}
