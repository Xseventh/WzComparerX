using WzComparerX.App.Services;
using WzComparerX.App.ViewModels;
using WzComparerX.Core;
using WzComparerX.Tests;
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
    public async Task ActivateSelectedNodeAsync_LoadsPackageFromFolderInspection()
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
            Assert.True(viewModel.ActivateSelectedNodeCommand.CanExecute(null));
            await viewModel.ActivateSelectedNodeAsync();

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
    public async Task ActivateSelectedNodeAsync_LoadsImageContentWithoutReplacingResourceTree()
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

            var inspectedImage = Assert.Single(viewModel.ImageContentNodes);
            Assert.Equal("Canvas.img", viewModel.SelectorText);
            Assert.Equal("package", Assert.Single(viewModel.RootNodes).Kind);
            Assert.Equal("image", inspectedImage.Kind);
            Assert.Equal("Canvas.img", inspectedImage.Name);
            Assert.Equal("Property", inspectedImage.DisplayValue);
            Assert.Contains(viewModel.SelectedMetadata, item => item.Name == "selector" && item.Value == "Canvas.img");
            Assert.Equal("Loaded pkg1: " + Path.GetFileName(path), viewModel.StatusMessage);
            Assert.Equal("Loaded IMG: Canvas.img", viewModel.ImageContentStatus);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task SelectingImageNode_LoadsImageContentAndKeepsManualLoadDisabled()
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
            await WaitForImageContentAsync(viewModel);

            Assert.Equal("package", Assert.Single(viewModel.RootNodes).Kind);
            Assert.Equal("image", Assert.Single(viewModel.ImageContentNodes).Kind);
            Assert.Equal(string.Empty, viewModel.SelectorText);
            Assert.False(viewModel.LoadImageCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task SelectingMsImageNode_LoadsImageContent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"Skill_00002-{Guid.NewGuid():N}.ms");
        var imageBytes = MsContainerFixture.CreatePropertyImage(
            MsContainerFixture.CreateScalarProperty("level", 7));
        await File.WriteAllBytesAsync(
            path,
            MsContainerFixture.CreateV2(
                Path.GetFileName(path),
                new MsContainerFixture.Entry(
                    "Skill/1000.img",
                    0,
                    imageBytes.Length,
                    1024,
                    Flags: 7,
                    Payload: imageBytes)),
            TestContext.Current.CancellationToken);
        var viewModel = new MainWindowViewModel
        {
            KeyText = "none"
        };

        try
        {
            await viewModel.OpenPathAsync(path);
            var package = Assert.Single(viewModel.RootNodes);
            var skill = Assert.Single(package.Children);
            var image = Assert.Single(skill.Children);

            viewModel.SelectedNode = image;
            await WaitForImageContentAsync(viewModel);

            Assert.Equal("ms", package.DisplayValue);
            Assert.EndsWith($"{Path.GetFileName(path)}/Skill/1000.img", image.Path, StringComparison.Ordinal);
            var inspectedImage = Assert.Single(viewModel.ImageContentNodes);
            Assert.Equal("Skill/1000.img", inspectedImage.Name);
            Assert.Equal("Property", inspectedImage.DisplayValue);
            Assert.Equal("Loaded IMG: Skill/1000.img", viewModel.ImageContentStatus);
            var level = Assert.Single(inspectedImage.Children);
            Assert.Equal("level", level.Name);
            Assert.Equal("int32", level.Kind);
            Assert.Equal("7", level.DisplayValue);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadImageAsync_LoadsManualSelector()
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
            Assert.True(viewModel.LoadImageCommand.CanExecute(null));
            await viewModel.LoadImageAsync();

            var inspectedImage = Assert.Single(viewModel.ImageContentNodes);
            Assert.Equal("package", Assert.Single(viewModel.RootNodes).Kind);
            Assert.Equal("image", inspectedImage.Kind);
            Assert.Equal("Canvas.img", inspectedImage.Name);
            Assert.Equal("Property", inspectedImage.DisplayValue);
            Assert.Contains(viewModel.SelectedMetadata, item => item.Name == "selector" && item.Value == "Canvas.img");
            Assert.True(viewModel.LoadImageCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task SelectingImageNode_LoadsSelectedImageFromLinkedPackage()
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
            await WaitForImageContentAsync(viewModel);

            var inspectedImage = Assert.Single(viewModel.ImageContentNodes);
            Assert.Equal(workspace.BasePath, viewModel.PathText);
            Assert.Equal(string.Empty, viewModel.SelectorText);
            Assert.Equal("package", Assert.Single(viewModel.RootNodes).Kind);
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
    public async Task LoadImageAsync_LoadsManualSelectorWithEmbeddedPackagePath()
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
            Assert.True(viewModel.LoadImageCommand.CanExecute(null));
            await viewModel.LoadImageAsync();

            var inspectedImage = Assert.Single(viewModel.ImageContentNodes);
            Assert.Equal(workspace.BasePath, viewModel.PathText);
            Assert.Equal("Canvas.img", viewModel.SelectorText);
            Assert.Equal("image", inspectedImage.Kind);
            Assert.Equal("package", Assert.Single(viewModel.RootNodes).Kind);
        }
        finally
        {
            workspace.Dispose();
        }
    }

    [Fact]
    public async Task SelectingMergedShardImage_LoadsImageContentFromOriginalShard()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-app-grouped-package-");
        var mapDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Map1"));
        var entryPath = Path.Combine(mapDirectory.FullName, "Map1.wz");
        var shardPath = Path.Combine(mapDirectory.FullName, "Map1_000.wz");
        var shardFixturePath = AppTestFixtures.MaterializeHexFixture("canvas-zlib.pkg1.hex", ".wz");
        var viewModel = new MainWindowViewModel
        {
            KeyText = "none"
        };

        try
        {
            File.WriteAllBytes(entryPath, AppTestFixtures.CreatePkg1());
            File.Copy(shardFixturePath, shardPath);
            await File.WriteAllTextAsync(
                Path.Combine(mapDirectory.FullName, "Map1.ini"),
                "LastWzIndex|0",
                TestContext.Current.CancellationToken);

            await viewModel.OpenPathAsync(entryPath);
            var image = Assert.Single(Assert.Single(viewModel.RootNodes).Children, child => child.Name == "Canvas.img");
            Assert.Equal($"{shardPath}/Canvas.img", image.Path);

            viewModel.SelectedNode = image;
            await WaitForImageContentAsync(viewModel);

            Assert.Equal(entryPath, viewModel.PathText);
            Assert.Equal("Canvas.img", Assert.Single(viewModel.ImageContentNodes).Name);
            Assert.Contains(Assert.Single(viewModel.ImageContentNodes).Children, child => child.Name == "icon" && child.Kind == "canvas");
        }
        finally
        {
            File.Delete(shardFixturePath);
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task SelectingOutlinkStringNode_LoadsLinkedCanvasPreview()
    {
        byte[] linkedPixels = [0x10, 0x20, 0x30, 0xff];
        var directory = Directory.CreateTempSubdirectory("wcx-app-outlink-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Data", "Map", "CanvasSource"));
        var proxyDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Data", "Map", "Proxy"));
        var sourcePath = Path.Combine(sourceDirectory.FullName, "CanvasSource.wz");
        var proxyPath = Path.Combine(proxyDirectory.FullName, "Proxy.wz");
        var viewModel = new MainWindowViewModel(
            document => new ResourceCanvasPreviewViewModel(document, bitmap: null))
        {
            KeyText = "none"
        };

        try
        {
            File.WriteAllBytes(
                sourcePath,
                AppTestFixtures.CreatePkg1ImagePackage(
                    "Linked.img",
                    AppTestFixtures.CreateCanvasPropertyImage("icon", linkedPixels)));
            File.WriteAllBytes(
                proxyPath,
                AppTestFixtures.CreatePkg1ImagePackage(
                    "Proxy.img",
                    AppTestFixtures.CreateLinkedCanvasPropertyImage(
                        "proxy",
                        "_outlink",
                        "Map/CanvasSource/Linked.img/icon")));

            await viewModel.OpenPathAsync(proxyPath);
            viewModel.SelectedNode = Assert.Single(Assert.Single(viewModel.RootNodes).Children);
            await WaitForImageContentAsync(viewModel);

            var proxy = Assert.Single(Assert.Single(viewModel.ImageContentNodes).Children, child => child.Name == "proxy");
            var outlink = Assert.Single(proxy.Children, child => child.Name == "_outlink");
            viewModel.SelectedImageContentNode = outlink;
            await WaitForCanvasPreviewAsync(viewModel);

            Assert.Equal("Linked.img", viewModel.CanvasPreview?.Selector);
            Assert.Equal("icon", viewModel.CanvasPreview?.ValuePath);
            Assert.Equal(1, viewModel.CanvasPreview?.Width);
            Assert.Equal(1, viewModel.CanvasPreview?.Height);
        }
        finally
        {
            viewModel.CanvasPreview?.Dispose();
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task SelectingMsOutlinkStringNode_LoadsLinkedCanvasPreview()
    {
        byte[] linkedPixels = [0x10, 0x20, 0x30, 0xff];
        var path = Path.Combine(Path.GetTempPath(), $"Mob_00000-{Guid.NewGuid():N}.ms");
        var proxyBytes = MsContainerFixture.CreateLinkedCanvasPropertyImage(
            "proxy",
            "_outlink",
            "Mob/_Canvas/1150000.img/icon");
        var linkedBytes = MsContainerFixture.CreateCanvasPropertyImage("icon", linkedPixels);
        await File.WriteAllBytesAsync(
            path,
            MsContainerFixture.CreateV4(
                Path.GetFileName(path),
                new MsContainerFixture.Entry(
                    "Mob/1150000.img",
                    0,
                    proxyBytes.Length,
                    1024,
                    Payload: proxyBytes),
                new MsContainerFixture.Entry(
                    "Mob/_Canvas/1150000.img",
                    1,
                    linkedBytes.Length,
                    1024,
                    Payload: linkedBytes)),
            TestContext.Current.CancellationToken);
        var viewModel = new MainWindowViewModel(
            document => new ResourceCanvasPreviewViewModel(document, bitmap: null))
        {
            KeyText = "none"
        };

        try
        {
            await viewModel.OpenPathAsync(path);
            var package = Assert.Single(viewModel.RootNodes);
            var mob = Assert.Single(package.Children, child => child.Name == "Mob");
            var image = Assert.Single(mob.Children, child => child.Name == "1150000.img");

            viewModel.SelectedNode = image;
            await WaitForImageContentAsync(viewModel);

            var proxy = Assert.Single(Assert.Single(viewModel.ImageContentNodes).Children, child => child.Name == "proxy");
            var outlink = Assert.Single(proxy.Children, child => child.Name == "_outlink");
            viewModel.SelectedImageContentNode = outlink;
            await WaitForCanvasPreviewAsync(viewModel);

            Assert.Equal("Mob/_Canvas/1150000.img", viewModel.CanvasPreview?.Selector);
            Assert.Equal("icon", viewModel.CanvasPreview?.ValuePath);
            Assert.Equal(1, viewModel.CanvasPreview?.Width);
            Assert.Equal(1, viewModel.CanvasPreview?.Height);
            Assert.DoesNotContain(viewModel.ActivityLog, item => item.Message.Contains("Invalid WZ package", StringComparison.Ordinal));
        }
        finally
        {
            viewModel.CanvasPreview?.Dispose();
            File.Delete(path);
        }
    }

    [Fact]
    public async Task SelectingImageNode_LoadsImageContentWithoutFirstCanvasPreview()
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
            await WaitForImageContentAsync(viewModel);

            Assert.Equal(path, viewModel.PathText);
            Assert.Equal(string.Empty, viewModel.SelectorText);
            Assert.True(viewModel.HasImageContent);
            Assert.False(viewModel.HasCanvasPreview);
            Assert.Equal("Canvas.img", Assert.Single(viewModel.ImageContentNodes).Name);
            Assert.Equal("Loaded IMG: Canvas.img", viewModel.ImageContentStatus);
        }
        finally
        {
            viewModel.CanvasPreview?.Dispose();
            File.Delete(path);
        }
    }

    [Fact]
    public async Task SelectingImageNodeFromLinkedPackage_LoadsImageContentWithoutChangingCurrentTree()
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
            await WaitForImageContentAsync(viewModel);

            Assert.Equal(workspace.BasePath, viewModel.PathText);
            Assert.Equal(string.Empty, viewModel.SelectorText);
            Assert.Equal("Canvas.img", Assert.Single(viewModel.ImageContentNodes).Name);
            Assert.Equal("Property", Assert.Single(viewModel.ImageContentNodes).DisplayValue);
            Assert.False(viewModel.HasCanvasPreview);
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
            await WaitForImageContentAsync(viewModel);

            var canvas = Assert.Single(Assert.Single(viewModel.ImageContentNodes).Children);
            Assert.Equal("canvas", canvas.Kind);
            viewModel.SelectedImageContentNode = canvas;
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
    public void CanvasPreviewViewModel_DefaultsToOneToOneScale()
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

        Assert.Equal(1d, preview.Scale);
        Assert.Equal("1x", preview.ScaleLabel);
        Assert.Equal(56d, preview.DisplayWidth);
        Assert.Equal(70d, preview.DisplayHeight);
    }

    [Fact]
    public void CanvasPreviewViewModel_CalculatesViewportFitScaleWithoutApplyingIt()
    {
        var document = new ResourceCanvasImageDocument(
            SourcePath: "Canvas.wz",
            Selector: "Canvas.img",
            ValuePath: "stand/0",
            Width: 56,
            Height: 70,
            Format: 1,
            PixelFormat: "bgra8888",
            Pixels: []);
        var preview = new ResourceCanvasPreviewViewModel(document, bitmap: null);

        var fitScale = preview.CalculateViewportFitScale(viewportWidth: 300, viewportHeight: 220);

        Assert.Equal(3d, fitScale);
        Assert.Equal(1d, preview.Scale);
        Assert.Equal("1x", preview.ScaleLabel);
        Assert.Equal(56d, preview.DisplayWidth);
        Assert.Equal(70d, preview.DisplayHeight);
    }

    [Fact]
    public void CanvasPreviewViewModel_AllowsManualFractionalDisplayScale()
    {
        var document = new ResourceCanvasImageDocument(
            SourcePath: "Canvas.wz",
            Selector: "Canvas.img",
            ValuePath: "miniMap/canvas",
            Width: 96,
            Height: 60,
            Format: 1,
            PixelFormat: "bgra8888",
            Pixels: []);
        var preview = new ResourceCanvasPreviewViewModel(document, bitmap: null);

        preview.SetScale(0.25);

        Assert.Equal(0.25d, preview.Scale);
        Assert.Equal("0.25x", preview.ScaleLabel);
        Assert.Equal(24d, preview.DisplayWidth);
        Assert.Equal(15d, preview.DisplayHeight);

        preview.SetScale(0.5);

        Assert.Equal(0.5d, preview.Scale);
        Assert.Equal("0.5x", preview.ScaleLabel);
        Assert.Equal(48d, preview.DisplayWidth);
        Assert.Equal(30d, preview.DisplayHeight);

        preview.SetScale(8);

        Assert.Equal(8d, preview.Scale);
        Assert.Equal("8x", preview.ScaleLabel);
        Assert.Equal(768d, preview.DisplayWidth);
        Assert.Equal(480d, preview.DisplayHeight);

        preview.SetScale(0.001);

        Assert.Equal(0.01d, preview.Scale);
        Assert.Equal("1%", preview.ScaleLabel);
    }

    [Fact]
    public void CanvasPreviewViewModel_CalculatesLargeImageViewportFitScale()
    {
        var document = new ResourceCanvasImageDocument(
            SourcePath: "Map.wz",
            Selector: "LargeMap.img",
            ValuePath: "miniMap/canvas",
            Width: 2048,
            Height: 1024,
            Format: 1,
            PixelFormat: "bgra8888",
            Pixels: []);
        var preview = new ResourceCanvasPreviewViewModel(document, bitmap: null);

        var fitScale = preview.CalculateViewportFitScale(viewportWidth: 800, viewportHeight: 400);
        preview.SetScale(fitScale);

        Assert.Equal(0.390625d, preview.Scale, precision: 6);
        Assert.Equal("39%", preview.ScaleLabel);
        Assert.Equal(800d, preview.DisplayWidth);
        Assert.Equal(400d, preview.DisplayHeight);
    }

    [Fact]
    public async Task SetCanvasPreviewScaleCommand_UpdatesCurrentPreviewScale()
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
            await WaitForImageContentAsync(viewModel);
            viewModel.SelectedImageContentNode = Assert.Single(Assert.Single(viewModel.ImageContentNodes).Children);
            await WaitForCanvasPreviewAsync(viewModel);

            viewModel.SetCanvasPreviewScaleCommand.Execute("0.5");

            Assert.Equal(0.5d, viewModel.CanvasPreview?.Scale);
            Assert.Equal("0.5x", viewModel.CanvasPreview?.ScaleLabel);
            Assert.Equal(1d, viewModel.CanvasPreview?.DisplayWidth);

            await viewModel.LoadCanvasPreviewAsync(viewModel.SelectedImageContentNode);
            await WaitForCanvasPreviewAsync(viewModel);

            Assert.Equal(0.5d, viewModel.CanvasPreview?.Scale);
            Assert.Equal("0.5x", viewModel.CanvasPreview?.ScaleLabel);

            viewModel.SetCanvasPreviewViewport(width: 300, height: 220);

            viewModel.SetCanvasPreviewScaleCommand.Execute("auto");

            Assert.Equal(16d, viewModel.CanvasPreview?.Scale);
            Assert.Equal("16x", viewModel.CanvasPreview?.ScaleLabel);

            await viewModel.LoadCanvasPreviewAsync(viewModel.SelectedImageContentNode);
            await WaitForCanvasPreviewAsync(viewModel);

            Assert.Equal(16d, viewModel.CanvasPreview?.Scale);
            Assert.Equal("16x", viewModel.CanvasPreview?.ScaleLabel);
        }
        finally
        {
            viewModel.CanvasPreview?.Dispose();
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

    private static async Task WaitForImageContentAsync(MainWindowViewModel viewModel)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (viewModel.HasImageContent)
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.Fail($"Image content was not loaded. Status: {viewModel.ImageContentStatus}");
    }
}
