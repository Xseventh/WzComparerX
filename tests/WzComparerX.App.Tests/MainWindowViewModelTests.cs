using WzComparerX.App.Services;
using WzComparerX.App.ViewModels;
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
    public async Task LoadAsync_ReportsInvalidDepthWithoutClearingExistingTree()
    {
        var viewModel = new MainWindowViewModel
        {
            PathText = FixturePath("basic-tree.json")
        };
        await viewModel.LoadAsync();

        viewModel.DepthText = "65";
        await viewModel.LoadAsync();

        Assert.Equal("Depth must be between 0 and 64.", viewModel.StatusMessage);
        Assert.Equal("error: Depth must be between 0 and 64.", viewModel.ActivityLog[0].Title);
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
    public async Task OpenSelectedPackageAsync_LoadsPackageFromFolderInspection()
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
            Assert.True(viewModel.OpenSelectedPackageCommand.CanExecute(null));
            await viewModel.OpenSelectedPackageAsync();

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
            KeyText = "none",
            DepthText = "1"
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
    public async Task InspectImageAsync_LoadsManualSelector()
    {
        var path = AppTestFixtures.MaterializeHexFixture("canvas-zlib.pkg1.hex", ".wz");
        var viewModel = new MainWindowViewModel
        {
            KeyText = "none",
            DepthText = "1"
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
    [InlineData("-1")]
    [InlineData("65")]
    [InlineData("abc")]
    public void ResourceInspectionOptionParser_RejectsUnsupportedDepth(string value)
    {
        Assert.False(ResourceInspectionOptionParser.TryParseDepth(value, out _));
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

    [Fact]
    public void ImageSelector_TreatsBlankSelectorAsDirectoryInspection()
    {
        Assert.Null(ResourceImageSelector.Normalize("  ", "Base_000.wz"));
    }

    private static string FixturePath(string name)
    {
        return AppTestFixtures.FixturePath(name);
    }
}
