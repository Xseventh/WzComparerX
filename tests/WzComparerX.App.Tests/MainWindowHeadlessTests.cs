using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WzComparerX.App.ViewModels;
using WzComparerX.App.Views;

namespace WzComparerX.App.Tests;

public class MainWindowHeadlessTests
{
    [AvaloniaFact]
    public void MainWindow_CreatesExpectedUiShell()
    {
        var viewModel = new MainWindowViewModel();
        var window = new MainWindow
        {
            DataContext = viewModel
        };
        try
        {
            window.Show();

            Assert.Same(viewModel, window.DataContext);
            Assert.NotNull(FindControl<TreeView>(window));
            Assert.NotNull(FindTab(window, "Selection"));
            Assert.NotNull(FindTab(window, "Diagnostics"));
            Assert.NotNull(window.FindControl<Control>("ActivityLogPanel"));
            Assert.NotNull(window.FindControl<ItemsControl>("ActivityLogList"));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_CapturesNonBlankFrameAfterSyntheticLoad()
    {
        var viewModel = new MainWindowViewModel
        {
            PathText = FixturePath("basic-tree.json")
        };
        await viewModel.LoadAsync();
        var window = CreateWindow(viewModel);

        try
        {
            using var frame = CaptureFrame(window);
            Assert.Equal(new PixelSize(1100, 720), frame.PixelSize);
            AssertPngCanBeSaved(frame);
            SaveScreenshotArtifact(frame, "main-window-synthetic-1100x720.png");
            AssertRenderedContentIsNotBlank(frame);
            AssertActivityLogIsVisible(window);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_CanSwitchDetailsTabsInHeadless()
    {
        var viewModel = new MainWindowViewModel
        {
            PathText = FixturePath("basic-tree.json")
        };
        await viewModel.LoadAsync();
        var window = CreateWindow(viewModel);

        try
        {
            var tabControl = window.FindControl<TabControl>("DetailsTabControl");
            Assert.NotNull(tabControl);
            Assert.Equal(0, tabControl.SelectedIndex);

            tabControl.SelectedIndex = 1;
            using var frame = CaptureFrame(window);

            Assert.Equal(1, tabControl.SelectedIndex);
            AssertPngCanBeSaved(frame);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_TreeSelectionUpdatesSelectionPanelInHeadless()
    {
        var viewModel = new MainWindowViewModel
        {
            PathText = FixturePath("basic-tree.json")
        };
        await viewModel.LoadAsync();
        var window = CreateWindow(viewModel);

        try
        {
            var tree = window.FindControl<TreeView>("ResourcesTree");
            Assert.NotNull(tree);
            var child = viewModel.RootNodes[0].Children[0];

            tree.SelectedItem = child;
            using var frame = CaptureFrame(window);

            Assert.Same(child, viewModel.SelectedNode);
            Assert.Contains(viewModel.SelectedMetadata, item => item.Name == "name" && item.Value == "Character.wz");
            Assert.Contains(viewModel.SelectedMetadata, item => item.Name == "kind" && item.Value == "directory");
            AssertPngCanBeSaved(frame);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_MouseClickOnTreeNodeUpdatesSelectionPanelInHeadless()
    {
        var viewModel = new MainWindowViewModel
        {
            PathText = FixturePath("basic-tree.json")
        };
        await viewModel.LoadAsync();
        var window = CreateWindow(viewModel);

        try
        {
            using var initialFrame = CaptureFrame(window);
            AssertPngCanBeSaved(initialFrame);

            var child = viewModel.RootNodes[0].Children[0];
            var item = FindTreeViewItem(window, child);
            Assert.NotNull(item);
            var topLeft = item.TranslatePoint(new Point(0, 0), window);
            Assert.True(topLeft.HasValue);
            var clickPoint = topLeft.Value + new Point(24, item.Bounds.Height / 2);

            window.MouseDown(clickPoint, MouseButton.Left);
            window.MouseUp(clickPoint, MouseButton.Left);
            using var frame = CaptureFrame(window);

            Assert.Same(child, viewModel.SelectedNode);
            Assert.Contains(viewModel.SelectedMetadata, item => item.Name == "name" && item.Value == "Character.wz");
            Assert.Contains(viewModel.SelectedMetadata, item => item.Name == "kind" && item.Value == "directory");
            AssertPngCanBeSaved(frame);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_ShowsInvalidPathFailureStateInHeadless()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"wcx-missing-{Guid.NewGuid():N}.wz");
        var viewModel = new MainWindowViewModel
        {
            PathText = missingPath
        };
        await viewModel.LoadAsync();
        var window = CreateWindow(viewModel);

        try
        {
            using var frame = CaptureFrame(window);
            AssertPngCanBeSaved(frame);

            Assert.Empty(viewModel.RootNodes);
            Assert.Empty(viewModel.DocumentMetadata);
            Assert.Equal("error", viewModel.ActivityLog[0].Kind);
            AssertStatusText(window, viewModel.StatusMessage);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_ShowsFolderOpenStateInHeadless()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-app-headless-folder-");
        var packagePath = Path.Combine(directory.FullName, "Base.wz");
        File.WriteAllBytes(packagePath, AppTestFixtures.CreatePkg1());
        var viewModel = new MainWindowViewModel();

        try
        {
            await viewModel.OpenPathAsync(directory.FullName);
            var window = CreateWindow(viewModel);

            try
            {
                var folderRoot = Assert.Single(viewModel.RootNodes);
                var package = Assert.Single(folderRoot.Children);
                var tree = window.FindControl<TreeView>("ResourcesTree");
                Assert.NotNull(tree);
                tree.SelectedItem = package;
                using var frame = CaptureFrame(window);

                Assert.Equal("folder", folderRoot.Kind);
                Assert.Equal("package", package.Kind);
                AssertStatusText(window, $"Loaded folder: {directory.Name}");
                AssertButtonEnabled(window, "OpenSelectedPackageButton", expected: true);
                AssertButtonEnabled(window, "InspectSelectedImageButton", expected: false);
                AssertPngCanBeSaved(frame);
            }
            finally
            {
                window.Close();
            }
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_ShowsPackageOpenStateInHeadless()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-app-headless-package-");
        var packagePath = Path.Combine(directory.FullName, "Base.wz");
        File.WriteAllBytes(packagePath, AppTestFixtures.CreatePkg1());
        var viewModel = new MainWindowViewModel();

        try
        {
            await viewModel.OpenPathAsync(directory.FullName);
            viewModel.SelectedNode = Assert.Single(Assert.Single(viewModel.RootNodes).Children);
            await viewModel.OpenSelectedPackageAsync();
            var window = CreateWindow(viewModel);

            try
            {
                using var frame = CaptureFrame(window);
                var pathTextBox = window.FindControl<TextBox>("PathTextBox");

                Assert.Equal(packagePath, pathTextBox?.Text);
                AssertStatusText(window, "Loaded pkg1: Base.wz");
                Assert.Equal("package", Assert.Single(viewModel.RootNodes).Kind);
                AssertButtonEnabled(window, "OpenSelectedPackageButton", expected: false);
                AssertButtonEnabled(window, "InspectSelectedImageButton", expected: false);
                AssertPngCanBeSaved(frame);
            }
            finally
            {
                window.Close();
            }
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_ShowsImageInspectionStateInHeadless()
    {
        var packagePath = AppTestFixtures.MaterializeHexFixture("canvas-zlib.pkg1.hex", ".wz");
        var viewModel = new MainWindowViewModel
        {
            KeyText = "none",
            DepthText = "1"
        };

        try
        {
            await viewModel.OpenPathAsync(packagePath);
            viewModel.SelectedNode = Assert.Single(Assert.Single(viewModel.RootNodes).Children);
            await viewModel.InspectSelectedImageAsync();
            var window = CreateWindow(viewModel);

            try
            {
                using var frame = CaptureFrame(window);
                var selectorTextBox = window.FindControl<TextBox>("SelectorTextBox");

                Assert.Equal("Canvas.img", selectorTextBox?.Text);
                AssertStatusText(window, $"Loaded pkg1: {Path.GetFileName(packagePath)}");
                Assert.Equal("image", Assert.Single(viewModel.RootNodes).Kind);
                Assert.Contains(viewModel.DocumentMetadata, item => item.Name == "selector" && item.Value == "Canvas.img");
                AssertButtonEnabled(window, "OpenSelectedPackageButton", expected: false);
                AssertButtonEnabled(window, "InspectSelectedImageButton", expected: false);
                AssertPngCanBeSaved(frame);
            }
            finally
            {
                window.Close();
            }
        }
        finally
        {
            File.Delete(packagePath);
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_KeepsPrimaryControlsInsideInitialViewport()
    {
        var viewModel = new MainWindowViewModel
        {
            PathText = FixturePath("basic-tree.json")
        };
        await viewModel.LoadAsync();
        var window = CreateWindow(viewModel);

        try
        {
            using var frame = CaptureFrame(window);
            Assert.Equal(new PixelSize(1100, 720), frame.PixelSize);

            AssertPrimaryControlsInsideViewport(window);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_KeepsPrimaryControlsInsideCompactViewport()
    {
        var viewModel = new MainWindowViewModel
        {
            PathText = FixturePath("basic-tree.json")
        };
        await viewModel.LoadAsync();
        var window = CreateWindow(viewModel, width: 900, height: 640);

        try
        {
            using var frame = CaptureFrame(window);
            Assert.Equal(new PixelSize(900, 640), frame.PixelSize);

            AssertPrimaryControlsInsideViewport(window);
        }
        finally
        {
            window.Close();
        }
    }

    private static void AssertPrimaryControlsInsideViewport(MainWindow window)
    {
        AssertControlInsideViewport(window, "PathTextBox");
        AssertControlInsideViewport(window, "BrowseButton");
        AssertControlInsideViewport(window, "LoadButton");
        AssertControlInsideViewport(window, "SelectorTextBox");
        AssertControlInsideViewport(window, "OpenSelectedPackageButton");
        AssertControlInsideViewport(window, "InspectSelectedImageButton");
        AssertControlInsideViewport(window, "ResourcesTree");
        AssertControlInsideViewport(window, "DetailsTabControl");
        AssertControlInsideViewport(window, "ActivityLogPanel");
    }

    private static void AssertActivityLogIsVisible(MainWindow window)
    {
        var activityLogList = window.FindControl<ItemsControl>("ActivityLogList");
        Assert.NotNull(activityLogList);
        Assert.True(activityLogList.Bounds.Width > 0);
        Assert.True(activityLogList.Bounds.Height > 0);
    }

    private static void AssertStatusText(MainWindow window, string expected)
    {
        var status = window.FindControl<TextBlock>("StatusTextBlock");
        Assert.NotNull(status);
        Assert.Equal(expected, status.Text);
    }

    private static void AssertButtonEnabled(MainWindow window, string name, bool expected)
    {
        var button = window.FindControl<Button>(name);
        Assert.NotNull(button);
        Assert.Equal(expected, button.IsEffectivelyEnabled);
    }

    private static MainWindow CreateWindow(
        MainWindowViewModel viewModel,
        double width = 1100,
        double height = 720)
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

    private static void AssertRenderedContentIsNotBlank(Bitmap frame)
    {
        using var framebuffer = new TestFramebuffer(frame.PixelSize);
        frame.CopyPixels(framebuffer);

        var colors = new HashSet<int>();
        for (var y = 0; y < framebuffer.Size.Height; y++)
        {
            var rowOffset = y * framebuffer.RowBytes;
            for (var x = 0; x < framebuffer.Size.Width; x++)
            {
                var offset = rowOffset + (x * 4);
                var color = BitConverter.ToInt32(framebuffer.Buffer, offset);
                colors.Add(color);
                if (colors.Count > 16)
                {
                    break;
                }
            }
        }

        Assert.True(colors.Count > 16, $"Expected varied rendered pixels, but only found {colors.Count} colors.");
    }

    private static void SaveScreenshotArtifact(Bitmap frame, string fileName)
    {
        var directory = Environment.GetEnvironmentVariable("WCX_HEADLESS_SCREENSHOT_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        frame.Save(Path.Combine(directory, fileName));
    }

    private static void AssertControlInsideViewport(MainWindow window, string name)
    {
        var control = window.FindControl<Control>(name);
        Assert.NotNull(control);

        var topLeft = control.TranslatePoint(new Point(0, 0), window);
        var bottomRight = control.TranslatePoint(
            new Point(control.Bounds.Width, control.Bounds.Height),
            window);

        Assert.True(topLeft.HasValue, $"{name} did not have a visual position.");
        Assert.True(bottomRight.HasValue, $"{name} did not have a visual size.");

        var viewport = new Rect(window.Bounds.Size);
        Assert.True(viewport.Contains(topLeft.Value), $"{name} starts outside the window: {topLeft.Value}.");
        Assert.True(viewport.Contains(bottomRight.Value), $"{name} ends outside the window: {bottomRight.Value}.");
    }

    private static T? FindControl<T>(Control root)
        where T : Control
    {
        if (root is T match)
        {
            return match;
        }

        foreach (var child in root.GetVisualChildren().OfType<Control>())
        {
            var matchInChild = FindControl<T>(child);
            if (matchInChild is not null)
            {
                return matchInChild;
            }
        }

        return null;
    }

    private static TabItem? FindTab(Control root, string header)
    {
        if (root is TabItem tabItem &&
            string.Equals(tabItem.Header?.ToString(), header, StringComparison.Ordinal))
        {
            return tabItem;
        }

        foreach (var child in root.GetVisualChildren().OfType<Control>())
        {
            var match = FindTab(child, header);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static TreeViewItem? FindTreeViewItem(Control root, object dataContext)
    {
        if (root is TreeViewItem treeViewItem &&
            ReferenceEquals(treeViewItem.DataContext, dataContext))
        {
            return treeViewItem;
        }

        foreach (var child in root.GetVisualChildren().OfType<Control>())
        {
            var match = FindTreeViewItem(child, dataContext);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static string FixturePath(string name)
    {
        return AppTestFixtures.FixturePath(name);
    }

    private sealed class TestFramebuffer : ILockedFramebuffer
    {
        private readonly GCHandle handle;

        public TestFramebuffer(PixelSize size)
        {
            Size = size;
            RowBytes = size.Width * 4;
            Buffer = new byte[RowBytes * size.Height];
            handle = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
        }

        public byte[] Buffer { get; }

        public IntPtr Address => handle.AddrOfPinnedObject();

        public PixelSize Size { get; }

        public int RowBytes { get; }

        public Vector Dpi { get; } = new(96, 96);

        public PixelFormat Format => PixelFormats.Bgra8888;

        public AlphaFormat AlphaFormat => AlphaFormat.Premul;

        public void Dispose()
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }
}
