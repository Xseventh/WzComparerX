using WzComparerX.App.Services;
using WzComparerX.App.ViewModels;
using WzComparerX.Core;
using WzComparerX.WzLib;

namespace WzComparerX.App.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public async Task LoadAsync_ProjectsSyntheticInspectionTree()
    {
        var viewModel = new MainWindowViewModel
        {
            PathText = FixturePath("basic-tree.json")
        };

        await viewModel.LoadAsync();

        var root = Assert.Single(viewModel.RootNodes);
        Assert.Equal("basic-tree", root.Name);
        Assert.Equal("directory", root.Kind);
        Assert.True(root.IsExpanded);
        Assert.All(root.Children, child => Assert.False(child.IsExpanded));
        Assert.Equal("Loaded synthetic: basic-tree.json", viewModel.StatusMessage);
        Assert.Equal("success: Loaded synthetic: basic-tree.json", viewModel.ActivityLog[0].Title);
        Assert.Equal("info: Loading basic-tree.json", viewModel.ActivityLog[1].Title);
        Assert.Contains(viewModel.DocumentMetadata, item => item.Name == "format" && item.Value == "synthetic");

        var image = root.Children[0].Children[0].Children[0];
        viewModel.SelectedNode = image;

        Assert.Equal("00002000.img", viewModel.SelectedNode.Name);
        Assert.Contains(viewModel.SelectedMetadata, item => item.Name == "kind" && item.Value == "image");
        Assert.Empty(viewModel.SelectedDiagnostics);
    }

    [Fact]
    public async Task LoadAsync_ReportsInvalidKeyWithoutClearingExistingTree()
    {
        var viewModel = new MainWindowViewModel
        {
            PathText = FixturePath("basic-tree.json")
        };
        await viewModel.LoadAsync();

        viewModel.KeyText = "invalid";
        await viewModel.LoadAsync();

        Assert.Equal("Unknown string key: invalid", viewModel.StatusMessage);
        Assert.Equal("error: Unknown string key: invalid", viewModel.ActivityLog[0].Title);
        Assert.NotEmpty(viewModel.RootNodes);
    }

    [Fact]
    public async Task OpenPathAsync_ClearsImageSelectorAndLoadsPath()
    {
        var viewModel = new MainWindowViewModel
        {
            SelectorText = "old.img"
        };

        await viewModel.OpenPathAsync(FixturePath("basic-tree.json"));

        Assert.Equal(string.Empty, viewModel.SelectorText);
        Assert.Equal("Loaded synthetic: basic-tree.json", viewModel.StatusMessage);
        Assert.Single(viewModel.RootNodes);
    }

    [Fact]
    public async Task OpenPackageAsync_LoadsPackageFromFolderInspection()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-app-folder-");
        var packagePath = Path.Combine(directory.FullName, "Base.wz");
        File.WriteAllBytes(packagePath, AppTestFixtures.CreatePkg1());
        var viewModel = new MainWindowViewModel();

        try
        {
            await viewModel.OpenPathAsync(directory.FullName);

            var folderRoot = Assert.Single(viewModel.RootNodes);
            Assert.Equal("folder", folderRoot.Kind);
            var package = Assert.Single(folderRoot.Children);
            Assert.Equal("package", package.Kind);

            viewModel.SelectedNode = package;
            Assert.True(viewModel.OpenPackageCommand.CanExecute(null));
            await viewModel.OpenPackageAsync();

            Assert.Equal(packagePath, viewModel.PathText);
            Assert.Equal(string.Empty, viewModel.SelectorText);
            Assert.Equal("Loaded pkg1: Base.wz", viewModel.StatusMessage);
            Assert.Equal("package", Assert.Single(viewModel.RootNodes).Kind);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task ActivateSelectedNodeAsync_OpensPackageNode()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-app-activate-");
        var packagePath = Path.Combine(directory.FullName, "Base.wz");
        File.WriteAllBytes(packagePath, AppTestFixtures.CreatePkg1());
        var viewModel = new MainWindowViewModel();

        try
        {
            await viewModel.OpenPathAsync(directory.FullName);
            viewModel.SelectedNode = Assert.Single(Assert.Single(viewModel.RootNodes).Children);

            Assert.True(viewModel.ActivateSelectedNodeCommand.CanExecute(null));
            await viewModel.ActivateSelectedNodeAsync();

            Assert.Equal(packagePath, viewModel.PathText);
            Assert.Equal("Loaded pkg1: Base.wz", viewModel.StatusMessage);
            Assert.Equal("package", Assert.Single(viewModel.RootNodes).Kind);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task ActivateSelectedNodeAsync_InspectsImageNode()
    {
        var path = AppTestFixtures.MaterializeHexFixture("canvas-zlib.pkg1.hex", ".wz");
        var viewModel = new MainWindowViewModel
        {
            KeyText = "none"
        };

        try
        {
            await viewModel.OpenPathAsync(path);

            var package = Assert.Single(viewModel.RootNodes);
            Assert.Equal("package", package.Kind);
            var image = Assert.Single(package.Children);
            Assert.Equal("Canvas.img", image.Name);
            Assert.Equal("image", image.Kind);

            viewModel.SelectedNode = image;
            Assert.True(viewModel.ActivateSelectedNodeCommand.CanExecute(null));
            await viewModel.ActivateSelectedNodeAsync();

            var inspectedImage = Assert.Single(viewModel.RootNodes);
            Assert.Equal("Canvas.img", viewModel.SelectorText);
            Assert.Equal("image", inspectedImage.Kind);
            Assert.Equal("Canvas.img", inspectedImage.Name);
            Assert.Equal("Property", inspectedImage.DisplayValue);
            Assert.Contains(viewModel.DocumentMetadata, item => item.Name == "selector" && item.Value == "Canvas.img");
            Assert.Equal("Loaded pkg1: " + Path.GetFileName(path), viewModel.StatusMessage);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task OpenPackageAsync_ReturnsFromImageInspectionToPackage()
    {
        var path = AppTestFixtures.MaterializeHexFixture("canvas-zlib.pkg1.hex", ".wz");
        var viewModel = new MainWindowViewModel
        {
            KeyText = "none"
        };

        try
        {
            await viewModel.OpenPathAsync(path);
            viewModel.SelectedNode = Assert.Single(Assert.Single(viewModel.RootNodes).Children);
            await viewModel.InspectImageAsync();

            Assert.Equal("image", Assert.Single(viewModel.RootNodes).Kind);
            Assert.Equal("Canvas.img", viewModel.SelectorText);
            Assert.True(viewModel.OpenPackageCommand.CanExecute(null));

            await viewModel.OpenPackageAsync();

            var package = Assert.Single(viewModel.RootNodes);
            Assert.Equal("package", package.Kind);
            Assert.Equal(string.Empty, viewModel.SelectorText);
            Assert.DoesNotContain(viewModel.DocumentMetadata, item => item.Name == "selector");
            Assert.False(viewModel.OpenPackageCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectImageAsync_LoadsManualSelector()
    {
        var path = AppTestFixtures.MaterializeHexFixture("canvas-zlib.pkg1.hex", ".wz");
        var viewModel = new MainWindowViewModel
        {
            KeyText = "none"
        };

        try
        {
            await viewModel.OpenPathAsync(path);

            viewModel.SelectorText = "Canvas.img";
            Assert.True(viewModel.InspectImageCommand.CanExecute(null));
            await viewModel.InspectImageAsync();

            var inspectedImage = Assert.Single(viewModel.RootNodes);
            Assert.Equal("image", inspectedImage.Kind);
            Assert.Equal("Canvas.img", inspectedImage.Name);
            Assert.Equal("Property", inspectedImage.DisplayValue);
            Assert.Contains(viewModel.DocumentMetadata, item => item.Name == "selector" && item.Value == "Canvas.img");
            Assert.False(viewModel.InspectImageCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectImageAsync_LoadsSelectedImageFromLinkedPackage()
    {
        var workspace = CreateLinkedCanvasWorkspace();
        var viewModel = new MainWindowViewModel
        {
            KeyText = "none"
        };

        try
        {
            await viewModel.OpenPathAsync(workspace.BasePath);
            var basePackage = Assert.Single(viewModel.RootNodes);
            var linkedDirectory = Assert.Single(basePackage.Children, child => child.Name == "Linked");
            var linkedPackage = Assert.Single(linkedDirectory.Children, child => child.Name == "Linked.wz");
            var image = Assert.Single(linkedPackage.Children, child => child.Name == "Canvas.img");

            Assert.Equal($"{workspace.LinkedPath}/Canvas.img", image.Path);
            viewModel.SelectedNode = image;
            Assert.True(viewModel.InspectImageCommand.CanExecute(null));
            await viewModel.InspectImageAsync();

            var inspectedImage = Assert.Single(viewModel.RootNodes);
            Assert.Equal(workspace.LinkedPath, viewModel.PathText);
            Assert.Equal("Canvas.img", viewModel.SelectorText);
            Assert.Equal("image", inspectedImage.Kind);
            Assert.Equal("Canvas.img", inspectedImage.Name);
            Assert.Equal("Property", inspectedImage.DisplayValue);
        }
        finally
        {
            workspace.Dispose();
        }
    }

    [Fact]
    public async Task InspectImageAsync_LoadsManualSelectorWithEmbeddedPackagePath()
    {
        var workspace = CreateLinkedCanvasWorkspace();
        var viewModel = new MainWindowViewModel
        {
            KeyText = "none"
        };

        try
        {
            await viewModel.OpenPathAsync(workspace.BasePath);

            viewModel.SelectorText = $"{workspace.LinkedPath}/Canvas.img";
            Assert.True(viewModel.InspectImageCommand.CanExecute(null));
            await viewModel.InspectImageAsync();

            var inspectedImage = Assert.Single(viewModel.RootNodes);
            Assert.Equal(workspace.LinkedPath, viewModel.PathText);
            Assert.Equal("Canvas.img", viewModel.SelectorText);
            Assert.Equal("image", inspectedImage.Kind);
        }
        finally
        {
            workspace.Dispose();
        }
    }

    [Fact]
    public async Task SelectingImageNode_LoadsFirstCanvasPreview()
    {
        var path = AppTestFixtures.MaterializeHexFixture("canvas-zlib.pkg1.hex", ".wz");
        var viewModel = new MainWindowViewModel(
            document => new ResourceCanvasPreviewViewModel(document, bitmap: null))
        {
            KeyText = "none"
        };

        try
        {
            await viewModel.OpenPathAsync(path);
            var image = Assert.Single(Assert.Single(viewModel.RootNodes).Children);

            viewModel.SelectedNode = image;
            await WaitForCanvasPreviewAsync(viewModel);

            Assert.Equal(path, viewModel.PathText);
            Assert.Equal(string.Empty, viewModel.SelectorText);
            Assert.True(viewModel.HasCanvasPreview);
            Assert.NotNull(viewModel.CanvasPreview);
            Assert.Equal("Canvas.img", viewModel.CanvasPreview.Selector);
            Assert.Equal("icon", viewModel.CanvasPreview.ValuePath);
            Assert.Equal(16, viewModel.CanvasPreview.Scale);
            Assert.Equal(32, viewModel.CanvasPreview.DisplayWidth);
            Assert.Equal(16, viewModel.CanvasPreview.DisplayHeight);
            Assert.Equal("Loaded Canvas preview: Canvas.img/icon (2x1)", viewModel.CanvasPreviewStatus);
        }
        finally
        {
            viewModel.CanvasPreview?.Dispose();
            File.Delete(path);
        }
    }

    [Fact]
    public async Task SelectingImageNodeFromLinkedPackage_LoadsFirstCanvasPreviewWithoutChangingCurrentTree()
    {
        var workspace = CreateLinkedCanvasWorkspace();
        var viewModel = new MainWindowViewModel(
            document => new ResourceCanvasPreviewViewModel(document, bitmap: null))
        {
            KeyText = "none"
        };

        try
        {
            await viewModel.OpenPathAsync(workspace.BasePath);
            var basePackage = Assert.Single(viewModel.RootNodes);
            var linkedDirectory = Assert.Single(basePackage.Children, child => child.Name == "Linked");
            var linkedPackage = Assert.Single(linkedDirectory.Children, child => child.Name == "Linked.wz");
            var image = Assert.Single(linkedPackage.Children, child => child.Name == "Canvas.img");

            viewModel.SelectedNode = image;
            await WaitForCanvasPreviewAsync(viewModel);

            Assert.Equal(workspace.BasePath, viewModel.PathText);
            Assert.Equal(string.Empty, viewModel.SelectorText);
            Assert.Equal("Canvas.img", viewModel.CanvasPreview?.Selector);
            Assert.Equal("icon", viewModel.CanvasPreview?.ValuePath);
            Assert.Equal("package", Assert.Single(viewModel.RootNodes).Kind);
        }
        finally
        {
            viewModel.CanvasPreview?.Dispose();
            workspace.Dispose();
        }
    }

    [Fact]
    public async Task SelectingCanvasNode_LoadsCanvasPreview()
    {
        var path = AppTestFixtures.MaterializeHexFixture("canvas-zlib.pkg1.hex", ".wz");
        var viewModel = new MainWindowViewModel(
            document => new ResourceCanvasPreviewViewModel(document, bitmap: null))
        {
            KeyText = "none"
        };

        try
        {
            await viewModel.OpenPathAsync(path);
            viewModel.SelectedNode = Assert.Single(Assert.Single(viewModel.RootNodes).Children);
            await viewModel.InspectImageAsync();

            var canvas = Assert.Single(Assert.Single(viewModel.RootNodes).Children);
            Assert.Equal("canvas", canvas.Kind);
            viewModel.SelectedNode = canvas;
            await WaitForCanvasPreviewAsync(viewModel);

            Assert.True(viewModel.HasCanvasPreview);
            Assert.NotNull(viewModel.CanvasPreview);
            Assert.Equal("Canvas.img", viewModel.CanvasPreview.Selector);
            Assert.Equal("icon", viewModel.CanvasPreview.ValuePath);
            Assert.Equal(2, viewModel.CanvasPreview.Width);
            Assert.Equal(1, viewModel.CanvasPreview.Height);
            Assert.Equal("Loaded Canvas preview: Canvas.img/icon (2x1)", viewModel.CanvasPreviewStatus);
            Assert.Contains(viewModel.ActivityLog, item => item.Title == "success: Loaded Canvas preview: Canvas.img/icon (2x1)");
        }
        finally
        {
            viewModel.CanvasPreview?.Dispose();
            File.Delete(path);
        }
    }

    [Fact]
    public void CanvasPreviewViewModel_ScalesSmallImagesForDisplay()
    {
        var document = new ResourceCanvasImageDocument(
            SourcePath: "Canvas.wz",
            Selector: "Canvas.img",
            ValuePath: "icon",
            Width: 56,
            Height: 70,
            Format: 1,
            PixelFormat: "bgra8888",
            Pixels: []);
        var preview = new ResourceCanvasPreviewViewModel(document, bitmap: null);

        Assert.Equal(2, preview.Scale);
        Assert.Equal(112, preview.DisplayWidth);
        Assert.Equal(140, preview.DisplayHeight);
    }

    [Theory]
    [InlineData("auto", null)]
    [InlineData("", null)]
    [InlineData("none", WzStringEncryptionKind.None)]
    [InlineData("noop", WzStringEncryptionKind.None)]
    [InlineData("kms", WzStringEncryptionKind.Kms)]
    [InlineData("gms", WzStringEncryptionKind.Gms)]
    public void ResourceInspectionOptionParser_ParsesSupportedStringKeys(
        string value,
        WzStringEncryptionKind? expected)
    {
        Assert.True(ResourceInspectionOptionParser.TryParseStringKey(value, out var actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("Base_000.wz/StandardPDD.img", "Base_000.wz", "StandardPDD.img")]
    [InlineData("base_000.wz/StandardPDD.img", "Base_000.wz", "StandardPDD.img")]
    [InlineData("String.wz/Eqp.img", "Base.wz", "String.wz/Eqp.img")]
    [InlineData("StandardPDD.img", "Base_000.wz", "StandardPDD.img")]
    [InlineData("  StandardPDD.img  ", "Base_000.wz", "StandardPDD.img")]
    public void ImageSelector_NormalizesOnlyPackageRootPrefix(
        string selector,
        string packageRoot,
        string expected)
    {
        Assert.Equal(expected, ResourceImageSelector.Normalize(selector, packageRoot));
    }

    [Theory]
    [InlineData("/tmp/Data/Linked/Linked.wz/Canvas.img", "/tmp/Base/Base.wz", "/tmp/Data/Linked/Linked.wz", "Canvas.img")]
    [InlineData("/tmp/Data/Linked/Linked.wz/Folder/Canvas.img", "/tmp/Base/Base.wz", "/tmp/Data/Linked/Linked.wz", "Folder/Canvas.img")]
    [InlineData("Base_000.wz/StandardPDD.img", "/tmp/Base/Base_000.wz", "/tmp/Base/Base_000.wz", "StandardPDD.img")]
    [InlineData("StandardPDD.img", "/tmp/Base/Base_000.wz", "/tmp/Base/Base_000.wz", "StandardPDD.img")]
    public void ImageSelector_ResolvesPackagePathAndSelector(
        string selector,
        string currentPackagePath,
        string expectedPackagePath,
        string expectedSelector)
    {
        var target = Assert.IsType<ResourceImageSelectorTarget>(
            ResourceImageSelector.Resolve(currentPackagePath, selector));

        Assert.Equal(expectedPackagePath, target.PackagePath);
        Assert.Equal(expectedSelector, target.Selector);
    }

    [Fact]
    public void ImageSelector_TreatsBlankSelectorAsDirectoryInspection()
    {
        Assert.Null(ResourceImageSelector.Normalize("  ", "Base_000.wz"));
    }

    private static string FixturePath(string name)
    {
        return AppTestFixtures.FixturePath(name);
    }

    private static LinkedCanvasWorkspace CreateLinkedCanvasWorkspace()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-linked-canvas-");
        var baseDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Base"));
        var linkedDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Linked"));
        var basePath = Path.Combine(baseDirectory.FullName, "Base.wz");
        var linkedPath = Path.Combine(linkedDirectory.FullName, "Linked.wz");
        var linkedFixturePath = AppTestFixtures.MaterializeHexFixture("canvas-zlib.pkg1.hex", ".wz");
        try
        {
            File.WriteAllBytes(basePath, AppTestFixtures.CreatePkg1DirectoryStubPackage("Linked"));
            File.Copy(linkedFixturePath, linkedPath);
        }
        finally
        {
            File.Delete(linkedFixturePath);
        }

        return new LinkedCanvasWorkspace(directory, basePath, linkedPath);
    }

    private sealed class LinkedCanvasWorkspace : IDisposable
    {
        private readonly DirectoryInfo directory;

        public LinkedCanvasWorkspace(DirectoryInfo directory, string basePath, string linkedPath)
        {
            this.directory = directory;
            BasePath = basePath;
            LinkedPath = linkedPath;
        }

        public string BasePath { get; }

        public string LinkedPath { get; }

        public void Dispose()
        {
            directory.Delete(recursive: true);
        }
    }

    private static async Task WaitForCanvasPreviewAsync(MainWindowViewModel viewModel)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (viewModel.HasCanvasPreview)
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.Fail($"Canvas preview was not loaded. Status: {viewModel.CanvasPreviewStatus}");
    }
}
