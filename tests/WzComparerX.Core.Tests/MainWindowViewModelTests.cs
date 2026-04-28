using System.Buffers.Binary;
using System.Text;
using WzComparerX.App.ViewModels;

namespace WzComparerX.Core.Tests;

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
        Assert.Equal("Loaded synthetic: basic-tree.json", viewModel.StatusMessage);
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
        File.WriteAllBytes(packagePath, CreatePkg1());
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
        return Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "fixtures",
            "synthetic",
            name);
    }

    private static byte[] CreatePkg1()
    {
        byte[] encryptedVersion = [0x7b, 0x00];
        byte[] directoryData = [0x00];
        var header = CreateHeader("PKG1", "Copyright", dataSize: encryptedVersion.Length + directoryData.Length);
        return [.. header, .. encryptedVersion, .. directoryData];
    }

    private static byte[] CreateHeader(string signature, string copyright, long dataSize)
    {
        var copyrightBytes = Encoding.ASCII.GetBytes(copyright);
        var headerSize = 4 + sizeof(long) + sizeof(int) + copyrightBytes.Length;
        var bytes = new byte[headerSize];

        Encoding.ASCII.GetBytes(signature, bytes);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(4, sizeof(long)), dataSize);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12, sizeof(int)), headerSize);
        copyrightBytes.CopyTo(bytes.AsSpan(16));
        return bytes;
    }
}
