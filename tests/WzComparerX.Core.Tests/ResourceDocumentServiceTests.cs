using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using WzComparerX.Core;
using WzComparerX.Tests;
using WzComparerX.WzLib;

namespace WzComparerX.Core.Tests;

public class ResourceDocumentServiceTests
{
    [Fact]
    public async Task OpenAsync_AddsSyntheticDocumentToWorkspace()
    {
        var service = new ResourceDocumentService();
        var workspace = new ResourceWorkspace();

        var document = await service.OpenAsync(FixturePath("basic-tree.json"));
        workspace.Add(document);

        Assert.Single(workspace.Documents);
        Assert.Equal("basic-tree", workspace.Documents[0].Root.Name);
        Assert.True(Path.IsPathFullyQualified(workspace.Documents[0].SourcePath));
    }

    [Fact]
    public async Task Format_ReturnsDeterministicTreeListing()
    {
        var service = new ResourceDocumentService();
        var formatter = new ResourceTreeListFormatter();

        var document = await service.OpenAsync(FixturePath("basic-tree.json"));

        var output = formatter.Format(document);

        Assert.Equal(
            """
            basic-tree [directory]
              Character.wz [directory]
                Cap [directory]
                  00002000.img [image]
                    info [property]
                      name [value] : string = "Beginner Cap"
                      reqLevel [value] : int32 = 0
              String.wz [directory]
                Eqp.img [image]
                  Eqp [property]

            """.ReplaceLineEndings(),
            output);
    }

    [Fact]
    public async Task FormatJson_ReturnsDeterministicTreeDocument()
    {
        var service = new ResourceDocumentService();
        var formatter = new ResourceTreeJsonFormatter();

        var document = await service.OpenAsync(FixturePath("basic-tree.json"));

        var output = formatter.Format(document);
        using var json = JsonDocument.Parse(output);
        var root = json.RootElement.GetProperty("Root");

        Assert.True(json.RootElement.TryGetProperty("SourcePath", out _));
        Assert.Equal("basic-tree", root.GetProperty("Name").GetString());
        Assert.Equal("Directory", root.GetProperty("Kind").GetString());
        Assert.Contains("Beginner Cap", output);
    }

    [Fact]
    public async Task InspectSynthetic_ReturnsStableInspectionTree()
    {
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionFormatter();

        var inspection = await service.InspectAsync(FixturePath("basic-tree.json"));
        var output = formatter.Format(inspection);

        Assert.Equal("synthetic", inspection.Format);
        Assert.Equal("basic-tree", inspection.Root.Name);
        Assert.Contains("source: ", output);
        Assert.Contains("basic-tree [directory]", output);
        Assert.Contains("name [value] : string = \"Beginner Cap\"", output);
    }

    [Fact]
    public async Task InspectSyntheticJson_ReturnsInspectionDocument()
    {
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionJsonFormatter();

        var inspection = await service.InspectAsync(FixturePath("basic-tree.json"));
        var output = formatter.Format(inspection);
        using var json = JsonDocument.Parse(output);

        Assert.Equal("synthetic", json.RootElement.GetProperty("Format").GetString());
        Assert.Equal("basic-tree", json.RootElement.GetProperty("Root").GetProperty("Name").GetString());
    }

    [Fact]
    public async Task InspectInvalidWz_ThrowsInvalidDataException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wcx-invalid-{Guid.NewGuid():N}.wz");
        await File.WriteAllTextAsync(path, "NOPE");

        try
        {
            var service = new ResourceInspectionService();

            await Assert.ThrowsAsync<InvalidDataException>(
                () => service.InspectAsync(path, stringKey: WzComparerX.WzLib.WzStringEncryptionKind.None));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectPkg2Directory_ReturnsUnsupportedDiagnosticDocument()
    {
        var path = WriteTemporaryPkg2File();
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                null,
                new ResourceInspectionOptions(WzStringEncryptionKind.None, IncludeDebugMetadata: true));

            Assert.Equal("pkg2", inspection.Format);
            Assert.Equal(Path.GetFileName(path), inspection.Root.Name);
            Assert.Equal("package", inspection.Root.Kind);
            Assert.Equal("pkg2", inspection.Root.DisplayValue);
            Assert.NotNull(inspection.Root.Identity);
            Assert.Equal(path, inspection.Root.Identity.PackagePath);
            Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "hash1" && Equals(item.Value, 0x11223344u));
            Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "hash2" && Equals(item.Value, 0xaabbccddu));

            var diagnostic = Assert.Single(inspection.Diagnostics!);
            Assert.Equal(ResourceDiagnosticSeverities.Error, diagnostic.Severity);
            Assert.Equal(ResourceDiagnosticCodes.Pkg2DirectoryInspectionUnsupported, diagnostic.Code);
            Assert.Equal(ResourceDiagnosticSources.Parser, diagnostic.Source);
            Assert.Equal(path, diagnostic.Path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectPkg2Kmst1200Directory_ReturnsEntryTree()
    {
        var path = WriteTemporaryPkg2File(
            new Pkg2PackageFixture.Entry(
                "ItemOption.img",
                Pkg2PackageFixture.CreateTextImage(("name", "item"))),
            new Pkg2PackageFixture.Entry(
                "SkillOption.img",
                Pkg2PackageFixture.CreateTextImage(("reqLevel", "12"))));
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                null,
                new ResourceInspectionOptions(WzStringEncryptionKind.None, IncludeDebugMetadata: true));

            Assert.Equal("pkg2", inspection.Format);
            Assert.Null(inspection.Diagnostics);
            Assert.Equal(Path.GetFileName(path), inspection.Root.Name);
            Assert.Equal("package", inspection.Root.Kind);
            Assert.Equal("pkg2", inspection.Root.DisplayValue);
            Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "formatProfile" && Equals(item.Value, "pkg2_kmst1200"));
            Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "wzVersion" && Equals(item.Value, Pkg2PackageFixture.WzVersion));
            Assert.Collection(
                inspection.Root.Children,
                first =>
                {
                    Assert.Equal("ItemOption.img", first.Name);
                    Assert.Equal("image", first.Kind);
                    Assert.Equal(path, first.Identity?.PackagePath);
                    Assert.Equal("ItemOption.img", first.Identity?.ImageSelector);
                },
                second =>
                {
                    Assert.Equal("SkillOption.img", second.Name);
                    Assert.Equal("image", second.Kind);
                    Assert.Equal(path, second.Identity?.PackagePath);
                    Assert.Equal("SkillOption.img", second.Identity?.ImageSelector);
                });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectModernKmsPkg2Directory_ReturnsEntryTree()
    {
        var path = WriteTemporaryModernPkg2File(
            new Pkg2PackageFixture.Entry(
                "ItemOption.img",
                Pkg2PackageFixture.CreateTextImage(("name", "item"))),
            new Pkg2PackageFixture.Entry(
                "SkillOption.img",
                Pkg2PackageFixture.CreateTextImage(("reqLevel", "12"))));
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                null,
                new ResourceInspectionOptions(WzStringEncryptionKind.None, IncludeDebugMetadata: true));

            Assert.Equal("pkg2", inspection.Format);
            Assert.Null(inspection.Diagnostics);
            Assert.Equal("package", inspection.Root.Kind);
            Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "pkg2HeaderVariant" && Equals(item.Value, "modern"));
            Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "formatProfile" && Equals(item.Value, "pkg2_modern_kms"));
            Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "hashVersion" && Equals(item.Value, Pkg2PackageFixture.HashVersion));
            Assert.DoesNotContain(inspection.DebugMetadata ?? [], item => item.Name == "wzVersion");
            Assert.Collection(
                inspection.Root.Children,
                first => Assert.Equal("ItemOption.img", first.Name),
                second => Assert.Equal("SkillOption.img", second.Name));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectPkg2Kmst1200Image_ReadsImagePayload()
    {
        var path = WriteTemporaryPkg2File(
            new Pkg2PackageFixture.Entry(
                "ItemOption.img",
                Pkg2PackageFixture.CreateTextImage(("name", "item"))),
            new Pkg2PackageFixture.Entry(
                "SkillOption.img",
                Pkg2PackageFixture.CreateTextImage(("reqLevel", "12"))));
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                "SkillOption.img",
                new ResourceInspectionOptions(WzStringEncryptionKind.None, IncludeDebugMetadata: true));

            Assert.Equal("pkg2", inspection.Format);
            Assert.Null(inspection.Diagnostics);
            Assert.Equal("SkillOption.img", inspection.Root.Name);
            Assert.Equal("image", inspection.Root.Kind);
            Assert.Equal("Property", inspection.Root.DisplayValue);
            var reqLevel = Assert.Single(inspection.Root.Children, child => child.Name == "reqLevel");
            Assert.Equal("int32", reqLevel.Kind);
            Assert.Equal("12", reqLevel.DisplayValue);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectMsContainer_ReturnsUnsupportedDiagnosticDocument()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wcx-ms-{Guid.NewGuid():N}.ms");
        await File.WriteAllBytesAsync(path, [0x4d, 0x53, 0x00]);
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                null,
                new ResourceInspectionOptions(WzStringEncryptionKind.None, IncludeDebugMetadata: true));

            Assert.Equal("ms", inspection.Format);
            Assert.Equal(Path.GetFileName(path), inspection.Root.Name);
            Assert.Equal("package", inspection.Root.Kind);
            Assert.Equal("ms", inspection.Root.DisplayValue);
            Assert.NotNull(inspection.Root.Identity);
            Assert.Equal(path, inspection.Root.Identity.PackagePath);
            Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "containerKind" && Equals(item.Value, "ms"));

            var diagnostic = Assert.Single(inspection.Diagnostics!);
            Assert.Equal(ResourceDiagnosticSeverities.Error, diagnostic.Severity);
            Assert.Equal(ResourceDiagnosticCodes.MsContainerInspectionUnsupported, diagnostic.Code);
            Assert.Equal(ResourceDiagnosticSources.Parser, diagnostic.Source);
            Assert.Equal(path, diagnostic.Path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectMsVersion4Container_ReturnsEntryTree()
    {
        var path = Path.Combine(Path.GetTempPath(), $"Skill_00002-{Guid.NewGuid():N}.ms");
        await File.WriteAllBytesAsync(
            path,
            MsContainerFixture.CreateV4(
                Path.GetFileName(path),
                new MsContainerFixture.Entry("Skill/1000.img", 0, 12, 1024, Flags: 7),
                new MsContainerFixture.Entry("Skill/2000.img", 1, 34, 1024, Flags: 8)));
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                null,
                new ResourceInspectionOptions(WzStringEncryptionKind.None, IncludeDebugMetadata: true));

            Assert.Equal("ms", inspection.Format);
            Assert.Null(inspection.Diagnostics);
            Assert.Equal(Path.GetFileName(path), inspection.Root.Name);
            Assert.Equal("package", inspection.Root.Kind);
            Assert.Equal("ms", inspection.Root.DisplayValue);
            Assert.Equal(path, inspection.Root.Identity?.PackagePath);
            Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "version" && Equals(item.Value, 4));
            Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "entryCount" && Equals(item.Value, 2));

            var skill = Assert.Single(inspection.Root.Children);
            Assert.Equal("Skill", skill.Name);
            Assert.Equal("directory", skill.Kind);
            Assert.Collection(
                skill.Children,
                image =>
                {
                    Assert.Equal("1000.img", image.Name);
                    Assert.Equal("image", image.Kind);
                    Assert.Equal("Skill/1000.img", image.Identity?.ImageSelector);
                    Assert.Contains(image.DebugMetadata!, item => item.Name == "flags" && Equals(item.Value, 7));
                    Assert.Contains(image.DebugMetadata!, item => item.Name == "size" && Equals(item.Value, 12));
                },
                image =>
                {
                    Assert.Equal("2000.img", image.Name);
                    Assert.Equal("image", image.Kind);
                    Assert.Equal("Skill/2000.img", image.Identity?.ImageSelector);
                    Assert.Contains(image.DebugMetadata!, item => item.Name == "flags" && Equals(item.Value, 8));
                    Assert.Contains(image.DebugMetadata!, item => item.Name == "size" && Equals(item.Value, 34));
                });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectMnVersion4Container_ReturnsEntryTree()
    {
        var path = Path.Combine(Path.GetTempPath(), $"Quest_00001-{Guid.NewGuid():N}.mn");
        await File.WriteAllBytesAsync(
            path,
            MsContainerFixture.CreateV4(
                Path.GetFileName(path),
                new MsContainerFixture.Entry("Quest/1000.img", 0, 21, 1024, Flags: 4)));
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                null,
                new ResourceInspectionOptions(WzStringEncryptionKind.None, IncludeDebugMetadata: true));

            Assert.Equal("ms", inspection.Format);
            Assert.Null(inspection.Diagnostics);
            Assert.Equal(Path.GetFileName(path), inspection.Root.Name);
            Assert.Equal("package", inspection.Root.Kind);
            Assert.Equal("ms", inspection.Root.DisplayValue);
            Assert.Equal(path, inspection.Root.Identity?.PackagePath);
            Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "version" && Equals(item.Value, 4));
            Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "entryCount" && Equals(item.Value, 1));

            var quest = Assert.Single(inspection.Root.Children);
            Assert.Equal("Quest", quest.Name);
            var image = Assert.Single(quest.Children);
            Assert.Equal("1000.img", image.Name);
            Assert.Equal("image", image.Kind);
            Assert.Equal("Quest/1000.img", image.Identity?.ImageSelector);
            Assert.Contains(image.DebugMetadata!, item => item.Name == "flags" && Equals(item.Value, 4));
            Assert.Contains(image.DebugMetadata!, item => item.Name == "size" && Equals(item.Value, 21));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectMsVersion4Image_ReturnsUnsupportedImageDiagnostic()
    {
        var path = Path.Combine(Path.GetTempPath(), $"Skill_00002-{Guid.NewGuid():N}.ms");
        await File.WriteAllBytesAsync(
            path,
            MsContainerFixture.CreateV4(
                Path.GetFileName(path),
                new MsContainerFixture.Entry("Skill/1000.img", 0, 12, 1024)));
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                "Skill/1000.img",
                new ResourceInspectionOptions(WzStringEncryptionKind.None, IncludeDebugMetadata: true));

            Assert.Equal("ms", inspection.Format);
            Assert.Equal("1000.img", inspection.Root.Name);
            Assert.Equal("image", inspection.Root.Kind);
            Assert.Equal("ms", inspection.Root.DisplayValue);
            Assert.Equal("Skill/1000.img", inspection.Root.Identity?.ImageSelector);

            var diagnostic = Assert.Single(inspection.Diagnostics!);
            Assert.Equal(ResourceDiagnosticSeverities.Error, diagnostic.Severity);
            Assert.Equal(ResourceDiagnosticCodes.MsImageInspectionUnsupported, diagnostic.Code);
            Assert.Equal(ResourceDiagnosticSources.Parser, diagnostic.Source);
            Assert.Equal("Skill/1000.img", diagnostic.Path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectMsVersion4Image_ReturnsImageTreeWhenPayloadIsSupported()
    {
        var path = Path.Combine(Path.GetTempPath(), $"Skill_00002-{Guid.NewGuid():N}.ms");
        var imageBytes = MsContainerFixture.CreatePropertyImage(
            MsContainerFixture.CreateScalarProperty("level", 7));
        await File.WriteAllBytesAsync(
            path,
            MsContainerFixture.CreateV4(
                Path.GetFileName(path),
                new MsContainerFixture.Entry(
                    "Skill/1000.img",
                    0,
                    imageBytes.Length,
                    1024,
                    Flags: 7,
                    Payload: imageBytes)));
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                "Skill/1000.img",
                new ResourceInspectionOptions(WzStringEncryptionKind.None, IncludeDebugMetadata: true));

            Assert.Equal("ms", inspection.Format);
            Assert.Null(inspection.Diagnostics);
            Assert.Equal("Skill/1000.img", inspection.Root.Name);
            Assert.Equal("image", inspection.Root.Kind);
            Assert.Equal("Property", inspection.Root.DisplayValue);
            Assert.Equal(path, inspection.Root.Identity?.PackagePath);
            Assert.Equal("Skill/1000.img", inspection.Root.Identity?.ImageSelector);
            Assert.Contains(inspection.Root.DebugMetadata!, item => item.Name == "flags" && Equals(item.Value, 7));
            Assert.Contains(inspection.Root.DebugMetadata!, item => item.Name == "size" && Equals(item.Value, imageBytes.Length));

            var level = Assert.Single(inspection.Root.Children);
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
    public async Task InspectMsVersion2Image_ReturnsImageTreeWhenPayloadIsSupported()
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
                    Payload: imageBytes)));
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                "Skill/1000.img",
                new ResourceInspectionOptions(WzStringEncryptionKind.None, IncludeDebugMetadata: true));

            Assert.Equal("ms", inspection.Format);
            Assert.Null(inspection.Diagnostics);
            Assert.Equal("Skill/1000.img", inspection.Root.Name);
            Assert.Equal("image", inspection.Root.Kind);
            Assert.Equal("Property", inspection.Root.DisplayValue);
            Assert.Equal("Skill/1000.img", inspection.Root.Identity?.ImageSelector);

            var level = Assert.Single(inspection.Root.Children);
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
    public async Task InspectDirectory_LinksSiblingSplitPackagesForEmptyTopLevelDirectories()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-split-package-");
        var baseDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Base"));
        var effectDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Effect"));
        var canvasDirectory = Directory.CreateDirectory(Path.Combine(effectDirectory.FullName, "_Canvas"));
        var basePath = Path.Combine(baseDirectory.FullName, "Base.wz");
        var effectPath = Path.Combine(effectDirectory.FullName, "Effect.wz");
        var effectShardPath = Path.Combine(effectDirectory.FullName, "Effect_000.wz");
        var canvasPackagePath = Path.Combine(canvasDirectory.FullName, "_Canvas.wz");
        await File.WriteAllBytesAsync(basePath, CreatePkg1DirectoryPackage(CreateDirectoryStub("Effect")));
        await File.WriteAllBytesAsync(effectPath, CreatePkg1DirectoryPackage(CreateDirectoryStub("_Canvas")));
        await File.WriteAllBytesAsync(effectShardPath, CreatePkg1DirectoryPackage(CreateImageDirectory("BasicEff.img")));
        await File.WriteAllBytesAsync(canvasPackagePath, CreatePkg1DirectoryPackage(CreateImageDirectory("Canvas.img")));
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                basePath,
                selector: null,
                new ResourceInspectionOptions(
                    WzStringEncryptionKind.None,
                    MaxPropertyDepth: 2,
                    IncludeDebugMetadata: true));

            var effect = Assert.Single(inspection.Root.Children, child => child.Name == "Effect");
            Assert.Equal("directory", effect.Kind);
            Assert.Single(effect.Children);

            var primaryPackage = Assert.Single(effect.Children, child => child.Name == "Effect.wz");
            Assert.Equal("package", primaryPackage.Kind);
            Assert.Equal(effectPath, primaryPackage.Path);
            var canvas = Assert.Single(primaryPackage.Children, child => child.Name == "_Canvas");
            var canvasPackage = Assert.Single(canvas.Children, child => child.Name == "_Canvas.wz");
            Assert.Equal("package", canvasPackage.Kind);
            Assert.Equal(canvasPackagePath, canvasPackage.Path);
            Assert.Contains(canvasPackage.Children, child => child.Name == "Canvas.img" && child.Kind == "image");
            var shardImage = Assert.Single(primaryPackage.Children, child => child.Name == "BasicEff.img");
            Assert.Equal("image", shardImage.Kind);
            Assert.Equal($"{effectShardPath}/BasicEff.img", shardImage.Path);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectDirectory_LinksNestedSplitPackagesRelativeToCurrentPackage()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-nested-split-package-");
        var packageDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "UI"));
        var canvasDirectory = Directory.CreateDirectory(Path.Combine(packageDirectory.FullName, "Resources", "_Canvas"));
        var packagePath = Path.Combine(packageDirectory.FullName, "UI.wz");
        var canvasPackagePath = Path.Combine(canvasDirectory.FullName, "_Canvas.wz");
        await File.WriteAllBytesAsync(packagePath, CreatePkg1DirectoryPackage(CreateNestedDirectoryStub("Resources", "_Canvas")));
        await File.WriteAllBytesAsync(canvasPackagePath, CreatePkg1DirectoryPackage(CreateImageDirectory("Texture.img")));
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                packagePath,
                selector: null,
                new ResourceInspectionOptions(
                    WzStringEncryptionKind.None,
                    MaxPropertyDepth: 2,
                    IncludeDebugMetadata: true));

            var resources = Assert.Single(inspection.Root.Children, child => child.Name == "Resources");
            var canvas = Assert.Single(resources.Children, child => child.Name == "_Canvas");
            var linkedPackage = Assert.Single(canvas.Children, child => child.Name == "_Canvas.wz");

            Assert.Equal("package", linkedPackage.Kind);
            Assert.Equal(canvasPackagePath, linkedPackage.Path);
            Assert.Contains(linkedPackage.Children, child => child.Name == "Texture.img" && child.Kind == "image");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectDirectory_ReportsUnresolvedSplitPackageCandidate()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-unresolved-split-package-");
        var baseDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Base"));
        var effectDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Effect"));
        var basePath = Path.Combine(baseDirectory.FullName, "Base.wz");
        var effectPath = Path.Combine(effectDirectory.FullName, "Effect.wz");
        await File.WriteAllBytesAsync(basePath, CreatePkg1DirectoryPackage(CreateDirectoryStub("Effect")));
        await File.WriteAllTextAsync(effectPath, "NOPE");
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionFormatter();

        try
        {
            var inspection = await service.InspectAsync(
                basePath,
                selector: null,
                new ResourceInspectionOptions(
                    WzStringEncryptionKind.None,
                    MaxPropertyDepth: 1,
                    IncludeDebugMetadata: true));
            var output = formatter.Format(inspection);

            var effect = Assert.Single(inspection.Root.Children, child => child.Name == "Effect");
            var diagnostic = Assert.Single(effect.Diagnostics!);
            Assert.Equal(ResourceDiagnosticSeverities.Warning, diagnostic.Severity);
            Assert.Equal(ResourceDiagnosticCodes.SplitPackageLinkUnresolved, diagnostic.Code);
            Assert.Equal(ResourceDiagnosticSources.Inspection, diagnostic.Source);
            Assert.Equal("Effect", diagnostic.Path);
            Assert.Contains("warning [wcx.package.link.unresolved]", output);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectDirectory_LinksSplitPackagesWhenPropertyDepthIsZero()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-split-package-depth-zero-");
        var baseDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Base"));
        var effectDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Effect"));
        var basePath = Path.Combine(baseDirectory.FullName, "Base.wz");
        var effectPath = Path.Combine(effectDirectory.FullName, "Effect.wz");
        await File.WriteAllBytesAsync(basePath, CreatePkg1DirectoryPackage(CreateDirectoryStub("Effect")));
        await File.WriteAllBytesAsync(effectPath, CreatePkg1DirectoryPackage(CreateImageDirectory("BasicEff.img")));
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                basePath,
                selector: null,
                new ResourceInspectionOptions(
                    WzStringEncryptionKind.None,
                    MaxPropertyDepth: 0,
                    IncludeDebugMetadata: true));

            var effect = Assert.Single(inspection.Root.Children, child => child.Name == "Effect");
            var linkedPackage = Assert.Single(effect.Children, child => child.Name == "Effect.wz");
            Assert.Contains(linkedPackage.Children, child => child.Name == "BasicEff.img" && child.Kind == "image");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectDirectory_DoesNotLinkCurrentPackageShardsThroughSameNameChild()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-same-name-split-package-");
        var packageDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Map"));
        var backDirectory = Directory.CreateDirectory(Path.Combine(packageDirectory.FullName, "Back"));
        var packagePath = Path.Combine(packageDirectory.FullName, "Map.wz");
        var shardPath = Path.Combine(packageDirectory.FullName, "Map_000.wz");
        var backPackagePath = Path.Combine(backDirectory.FullName, "Back.wz");
        await File.WriteAllBytesAsync(packagePath, CreatePkg1DirectoryPackage(CreateDirectoryStubs("Back", "Map")));
        await File.WriteAllBytesAsync(shardPath, CreatePkg1DirectoryPackage(CreateImageDirectory("Field.img")));
        await File.WriteAllBytesAsync(backPackagePath, CreatePkg1DirectoryPackage(CreateImageDirectory("Back.img")));
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                packagePath,
                selector: null,
                new ResourceInspectionOptions(
                    WzStringEncryptionKind.None,
                    MaxPropertyDepth: 1,
                    IncludeDebugMetadata: true));

            var back = Assert.Single(inspection.Root.Children, child => child.Name == "Back");
            var backPackage = Assert.Single(back.Children, child => child.Name == "Back.wz");
            Assert.Equal(backPackagePath, backPackage.Path);

            var map = Assert.Single(inspection.Root.Children, child => child.Name == "Map");
            Assert.Empty(map.Children);

            var field = Assert.Single(inspection.Root.Children, child => child.Name == "Field.img");
            Assert.Equal("image", field.Kind);
            Assert.Equal($"{shardPath}/Field.img", field.Path);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectDirectory_DoesNotUseWorkspaceFallbackForNonBasePackages()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-non-base-workspace-fallback-");
        var packageDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Map"));
        var workspaceSiblingDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Back"));
        var packagePath = Path.Combine(packageDirectory.FullName, "Map.wz");
        var workspaceSiblingPackagePath = Path.Combine(workspaceSiblingDirectory.FullName, "Back.wz");
        await File.WriteAllBytesAsync(packagePath, CreatePkg1DirectoryPackage(CreateDirectoryStub("Back")));
        await File.WriteAllBytesAsync(workspaceSiblingPackagePath, CreatePkg1DirectoryPackage(CreateImageDirectory("Back.img")));
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                packagePath,
                selector: null,
                new ResourceInspectionOptions(
                    WzStringEncryptionKind.None,
                    MaxPropertyDepth: 1,
                    IncludeDebugMetadata: true));

            var back = Assert.Single(inspection.Root.Children, child => child.Name == "Back");
            Assert.Empty(back.Children);
            Assert.Null(back.Diagnostics);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectDirectory_MergesNumberedShardsFromIniIntoEntryPackage()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-package-group-ini-");
        var mapDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Map1"));
        var entryPath = Path.Combine(mapDirectory.FullName, "Map1.wz");
        var shardPath = Path.Combine(mapDirectory.FullName, "Map1_000.wz");
        await File.WriteAllBytesAsync(entryPath, CreatePkg1DirectoryPackage([0x00]));
        await File.WriteAllBytesAsync(shardPath, CreatePkg1DirectoryPackage(CreateImageDirectory("100000000.img")));
        await File.WriteAllTextAsync(Path.Combine(mapDirectory.FullName, "Map1.ini"), "LastWzIndex|0");
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                entryPath,
                selector: null,
                new ResourceInspectionOptions(
                    WzStringEncryptionKind.None,
                    MaxPropertyDepth: 1,
                    IncludeDebugMetadata: true));

            var image = Assert.Single(inspection.Root.Children, child => child.Name == "100000000.img");
            Assert.Equal("image", image.Kind);
            Assert.Equal($"{shardPath}/100000000.img", image.Path);
            Assert.NotNull(image.Identity);
            Assert.Equal(shardPath, image.Identity.PackagePath);
            Assert.Equal("100000000.img", image.Identity.ImageSelector);
            Assert.Null(image.Identity.ValuePath);
            Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "packageGroupCount" && Equals(item.Value, 2));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectDirectory_ReportsMissingNumberedShardDeclaredByIni()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-package-group-missing-shard-");
        var mapDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Map1"));
        var entryPath = Path.Combine(mapDirectory.FullName, "Map1.wz");
        var shardPath = Path.Combine(mapDirectory.FullName, "Map1_000.wz");
        var missingShardPath = Path.Combine(mapDirectory.FullName, "Map1_001.wz");
        await File.WriteAllBytesAsync(entryPath, CreatePkg1DirectoryPackage([0x00]));
        await File.WriteAllBytesAsync(shardPath, CreatePkg1DirectoryPackage(CreateImageDirectory("100000000.img")));
        await File.WriteAllTextAsync(Path.Combine(mapDirectory.FullName, "Map1.ini"), "LastWzIndex|1");
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionFormatter();

        try
        {
            var inspection = await service.InspectAsync(
                entryPath,
                selector: null,
                new ResourceInspectionOptions(
                    WzStringEncryptionKind.None,
                    MaxPropertyDepth: 1,
                    IncludeDebugMetadata: true));
            var output = formatter.Format(inspection);

            Assert.Contains(inspection.Root.Children, child => child.Name == "100000000.img" && child.Kind == "image");
            var diagnostic = Assert.Single(inspection.Root.Diagnostics!);
            Assert.Equal(ResourceDiagnosticSeverities.Warning, diagnostic.Severity);
            Assert.Equal(ResourceDiagnosticCodes.PackageGroupShardMissing, diagnostic.Code);
            Assert.Equal(ResourceDiagnosticSources.Inspection, diagnostic.Source);
            Assert.Equal(missingShardPath, diagnostic.Path);
            Assert.Contains("warning [wcx.package.group.shardMissing]", output);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectDirectory_ReportsInvalidNumberedShardDeclaredByIni()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-package-group-invalid-shard-");
        var mapDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Map1"));
        var entryPath = Path.Combine(mapDirectory.FullName, "Map1.wz");
        var shardPath = Path.Combine(mapDirectory.FullName, "Map1_000.wz");
        await File.WriteAllBytesAsync(entryPath, CreatePkg1DirectoryPackage([0x00]));
        await File.WriteAllTextAsync(shardPath, "NOPE");
        await File.WriteAllTextAsync(Path.Combine(mapDirectory.FullName, "Map1.ini"), "LastWzIndex=0");
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                entryPath,
                selector: null,
                new ResourceInspectionOptions(
                    WzStringEncryptionKind.None,
                    MaxPropertyDepth: 1,
                    IncludeDebugMetadata: true));

            var diagnostic = Assert.Single(inspection.Root.Diagnostics!);
            Assert.Equal(ResourceDiagnosticSeverities.Warning, diagnostic.Severity);
            Assert.Equal(ResourceDiagnosticCodes.PackageGroupShardInvalid, diagnostic.Code);
            Assert.Equal(ResourceDiagnosticSources.Inspection, diagnostic.Source);
            Assert.Equal(shardPath, diagnostic.Path);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectDirectory_MergedImageNodeTargetCanInspectShardImage()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-package-group-target-");
        var mapDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Map1"));
        var entryPath = Path.Combine(mapDirectory.FullName, "Map1.wz");
        var shardPath = Path.Combine(mapDirectory.FullName, "Map1_000.wz");
        var imageBytes = CreatePropertyImage(CreateObjectProperty(
            "icon",
            CreateObjectValue(
                "Canvas",
                0x00,
                0x00,
                1,
                1,
                2,
                0x00,
                1,
                0,
                (byte)0x00,
                (byte)0x00,
                BitConverter.GetBytes(3),
                0x00,
                0x78,
                0x9c)));
        await File.WriteAllBytesAsync(entryPath, CreatePkg1DirectoryPackage([0x00]));
        await File.WriteAllBytesAsync(shardPath, CreatePkg1ImagePackage("Canvas.img", imageBytes));
        var service = new ResourceInspectionService();

        try
        {
            var directoryInspection = await service.InspectAsync(
                entryPath,
                selector: null,
                new ResourceInspectionOptions(WzStringEncryptionKind.None, IncludeDebugMetadata: true));
            var image = Assert.Single(directoryInspection.Root.Children, child => child.Name == "Canvas.img");
            Assert.Equal($"{shardPath}/Canvas.img", image.Path);

            var imageInspection = await service.InspectAsync(
                shardPath,
                selector: "Canvas.img",
                new ResourceInspectionOptions(WzStringEncryptionKind.None, MaxPropertyDepth: 2, IncludeDebugMetadata: true));

            Assert.Equal("Canvas.img", imageInspection.Root.Name);
            Assert.NotNull(imageInspection.Root.Identity);
            Assert.Equal(shardPath, imageInspection.Root.Identity.PackagePath);
            Assert.Equal("Canvas.img", imageInspection.Root.Identity.ImageSelector);
            Assert.Contains(imageInspection.Root.Children, child => child.Name == "icon" && child.Kind == "canvas");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectDebugImage_IncludesEntryPropertyAndPayloadMetadata()
    {
        var imageBytes = CreatePropertyImage(CreateObjectProperty(
            "icon",
            CreateObjectValue(
                "Canvas",
                0x00,
                0x00,
                16,
                8,
                2,
                0x00,
                1,
                0,
                (byte)0x00,
                (byte)0x00,
                BitConverter.GetBytes(3),
                0x00,
                0x78,
                0x9c)));
        var path = WriteTemporaryPkg1ImageFile(imageBytes);
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionFormatter();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                selector: "Canvas.img",
                new ResourceInspectionOptions(WzStringEncryptionKind.None, MaxPropertyDepth: 2, IncludeDebugMetadata: true));
            var output = formatter.Format(inspection);

            Assert.Contains("entryDataSize:", output);
            Assert.Contains("objectType: Property", output);
            Assert.Contains("icon [canvas]", output);
            Assert.Contains("type: 0x09", output);
            Assert.Contains("dataOffset:", output);
            Assert.Contains("dataLength: 3", output);
            Assert.Contains("compressionKind: Zlib", output);
            Assert.Contains("Canvas pixel decoding is lazy and currently supports a narrow direct-zlib format slice.", output);

            var icon = Assert.Single(inspection.Root.Children, child => child.Name == "icon");
            Assert.NotNull(icon.Identity);
            Assert.Equal(path, icon.Identity.PackagePath);
            Assert.Equal("Canvas.img", icon.Identity.ImageSelector);
            Assert.Equal("icon", icon.Identity.ValuePath);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectDebugImage_IncludesMediaPayloadDiagnostics()
    {
        var imageBytes = CreatePropertyImage(
            CreateRawDataProperty(),
            CreateVideoProperty(),
            CreateSoundProperty());
        var path = WriteTemporaryPkg1ImageFile(imageBytes);
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                selector: "Canvas.img",
                new ResourceInspectionOptions(WzStringEncryptionKind.None, MaxPropertyDepth: 2, IncludeDebugMetadata: true));

            var raw = Assert.Single(inspection.Root.Children, child => child.Name == "raw");
            Assert.Equal("rawData", raw.Kind);
            Assert.Contains(raw.DebugMetadata ?? [], item => item.Name == "valueType" && Equals(item.Value, "rawData"));
            Assert.Contains(raw.DebugMetadata ?? [], item => item.Name == "version" && Equals(item.Value, 0));
            Assert.Contains(raw.DebugMetadata ?? [], item => item.Name == "dataLength" && Equals(item.Value, 3));
            Assert.Contains(raw.Diagnostics ?? [], diagnostic =>
                diagnostic.Code == ResourceDiagnosticCodes.RawDataPayloadDecodingUnsupported &&
                diagnostic.Severity == ResourceDiagnosticSeverities.Info &&
                diagnostic.Source == ResourceDiagnosticSources.Parser &&
                diagnostic.Path == "raw");

            var clip = Assert.Single(inspection.Root.Children, child => child.Name == "clip");
            Assert.Equal("video", clip.Kind);
            Assert.Contains(clip.DebugMetadata ?? [], item => item.Name == "valueType" && Equals(item.Value, "video"));
            Assert.Contains(clip.DebugMetadata ?? [], item => item.Name == "unknown" && Equals(item.Value, 5));
            Assert.Contains(clip.DebugMetadata ?? [], item => item.Name == "dataLength" && Equals(item.Value, 4));
            Assert.Contains(clip.Diagnostics ?? [], diagnostic =>
                diagnostic.Code == ResourceDiagnosticCodes.VideoPayloadDecodingUnsupported &&
                diagnostic.Severity == ResourceDiagnosticSeverities.Info &&
                diagnostic.Source == ResourceDiagnosticSources.Parser &&
                diagnostic.Path == "clip");

            var sound = Assert.Single(inspection.Root.Children, child => child.Name == "sound");
            Assert.Equal("sound", sound.Kind);
            Assert.Contains(sound.DebugMetadata ?? [], item => item.Name == "valueType" && Equals(item.Value, "sound"));
            Assert.Contains(sound.DebugMetadata ?? [], item => item.Name == "duration" && Equals(item.Value, 60));
            Assert.Contains(sound.DebugMetadata ?? [], item => item.Name == "soundDeclaration" && Equals(item.Value, 2));
            Assert.Contains(sound.DebugMetadata ?? [], item => item.Name == "dataLength" && Equals(item.Value, 3));
            Assert.Contains(sound.Diagnostics ?? [], diagnostic =>
                diagnostic.Code == ResourceDiagnosticCodes.AudioPayloadDecodingUnsupported &&
                diagnostic.Severity == ResourceDiagnosticSeverities.Info &&
                diagnostic.Source == ResourceDiagnosticSources.Parser &&
                diagnostic.Path == "sound");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectDebugImage_IncludesLinkIdentityAndMetadata()
    {
        var imageBytes = CreatePropertyImage(
            CreateLinkedCanvasProperty("proxy", "_outlink", "Map\\CanvasSource\\Linked.img\\icon"),
            CreateObjectProperty("ref", CreateObjectValue("UOL", 0x00, CreateImageString("..\\info\\source"))));
        var path = WriteTemporaryPkg1ImageFile(imageBytes);
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionFormatter();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                selector: "Canvas.img",
                new ResourceInspectionOptions(WzStringEncryptionKind.None, MaxPropertyDepth: 2, IncludeDebugMetadata: true));
            var output = formatter.Format(inspection);

            var proxy = Assert.Single(inspection.Root.Children, child => child.Name == "proxy");
            var outlink = Assert.Single(proxy.Children, child => child.Name == "_outlink");
            Assert.Equal("string", outlink.Kind);
            Assert.NotNull(outlink.Identity);
            Assert.Equal(path, outlink.Identity.PackagePath);
            Assert.Equal("Canvas.img", outlink.Identity.ImageSelector);
            Assert.Equal("proxy/_outlink", outlink.Identity.ValuePath);
            Assert.Equal("Map/CanvasSource/Linked.img/icon", outlink.Identity.LinkedTarget);
            Assert.Contains("linkKind: _outlink", output);
            Assert.Contains("linkedTarget: Map/CanvasSource/Linked.img/icon", output);

            var uol = Assert.Single(inspection.Root.Children, child => child.Name == "ref");
            Assert.Equal("uol", uol.Kind);
            Assert.NotNull(uol.Identity);
            Assert.Equal("../info/source", uol.Identity.LinkedTarget);
            Assert.NotNull(uol.Identity.ResolvedLinkedTarget);
            Assert.Equal(path, uol.Identity.ResolvedLinkedTarget.PackagePath);
            Assert.Equal("Canvas.img", uol.Identity.ResolvedLinkedTarget.ImageSelector);
            Assert.Equal("info/source", uol.Identity.ResolvedLinkedTarget.ValuePath);
            Assert.Contains("linkKind: uol", output);
            Assert.Contains("linkedTarget: ../info/source", output);
            Assert.Contains("resolvedLinkedValuePath: info/source", output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectDebugImage_ResolvesOutlinkIdentityAcrossDataPackages()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-inspect-resolved-outlink-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Data", "Map", "CanvasSource"));
        var proxyDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Data", "Map", "Proxy"));
        var sourcePath = Path.Combine(sourceDirectory.FullName, "CanvasSource.wz");
        var proxyPath = Path.Combine(proxyDirectory.FullName, "Proxy.wz");
        await File.WriteAllBytesAsync(sourcePath, CreatePkg1ImagePackage("Linked.img", CreatePropertyImage()));
        await File.WriteAllBytesAsync(
            proxyPath,
            CreatePkg1ImagePackage(
                "Proxy.img",
                CreatePropertyImage(CreateLinkedCanvasProperty(
                    "proxy",
                    "_outlink",
                    "Map\\CanvasSource\\Linked.img\\icon"))));
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionFormatter();

        try
        {
            var inspection = await service.InspectAsync(
                proxyPath,
                selector: "Proxy.img",
                new ResourceInspectionOptions(WzStringEncryptionKind.None, MaxPropertyDepth: 2, IncludeDebugMetadata: true));
            var output = formatter.Format(inspection);

            var proxy = Assert.Single(inspection.Root.Children, child => child.Name == "proxy");
            var outlink = Assert.Single(proxy.Children, child => child.Name == "_outlink");
            Assert.NotNull(outlink.Identity);
            Assert.Equal("Map/CanvasSource/Linked.img/icon", outlink.Identity.LinkedTarget);
            Assert.NotNull(outlink.Identity.ResolvedLinkedTarget);
            Assert.Equal(sourcePath, outlink.Identity.ResolvedLinkedTarget.PackagePath);
            Assert.Equal("Linked.img", outlink.Identity.ResolvedLinkedTarget.ImageSelector);
            Assert.Equal("icon", outlink.Identity.ResolvedLinkedTarget.ValuePath);
            Assert.Contains("resolvedLinkedPackagePath:", output);
            Assert.Contains("resolvedLinkedImageSelector: Linked.img", output);
            Assert.Contains("resolvedLinkedValuePath: icon", output);

            var jsonOutput = new ResourceInspectionJsonFormatter().Format(inspection);
            using var json = JsonDocument.Parse(jsonOutput);
            var jsonProxy = Assert.Single(json.RootElement.GetProperty("Root").GetProperty("Children").EnumerateArray());
            var jsonOutlink = Assert.Single(jsonProxy.GetProperty("Children").EnumerateArray());
            var resolved = jsonOutlink.GetProperty("Identity").GetProperty("ResolvedLinkedTarget");
            Assert.Equal(sourcePath, resolved.GetProperty("PackagePath").GetString());
            Assert.Equal("Linked.img", resolved.GetProperty("ImageSelector").GetString());
            Assert.Equal("icon", resolved.GetProperty("ValuePath").GetString());
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InspectNormalImage_DoesNotIncludeDebugMetadata()
    {
        var path = WriteTemporaryPkg1ImageFile(CreatePropertyImage());
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionFormatter();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                selector: "Canvas.img",
                new ResourceInspectionOptions(WzStringEncryptionKind.None));
            var output = formatter.Format(inspection);

            Assert.DoesNotContain("debug:", output);
            Assert.DoesNotContain("entryDataSize", output);
            Assert.DoesNotContain("nodeType", output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectImageConvenienceOverload_UsesFullPropertyDepthByDefault()
    {
        var imageBytes = CreatePropertyImage(
            CreateObjectProperty(
                "child",
                CreateObjectValue(
                    "Property",
                    0x00,
                    0x00,
                    1,
                    CreateScalarProperty("foo", 42))));
        var path = WriteTemporaryPkg1ImageFile(imageBytes);
        var service = new ResourceInspectionService();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                selector: "Canvas.img",
                stringKey: WzStringEncryptionKind.None);

            var nested = Assert.Single(inspection.Root.Children, child => child.Name == "child");
            var child = Assert.Single(nested.Children, child => child.Name == "foo");
            Assert.Equal("int32", child.Kind);
            Assert.Equal("42", child.DisplayValue);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InspectDebugJson_IncludesStructuredMetadata()
    {
        var path = WriteTemporaryPkg1ImageFile(CreatePropertyImage());
        var service = new ResourceInspectionService();
        var formatter = new ResourceInspectionJsonFormatter();

        try
        {
            var inspection = await service.InspectAsync(
                path,
                selector: "Canvas.img",
                new ResourceInspectionOptions(WzStringEncryptionKind.None, IncludeDebugMetadata: true));
            var output = formatter.Format(inspection);
            using var json = JsonDocument.Parse(output);

            Assert.True(json.RootElement.TryGetProperty("DebugMetadata", out var metadata));
            Assert.Contains(metadata.EnumerateArray(), item =>
                item.GetProperty("Name").GetString() == "stringKey" &&
                item.GetProperty("Value").GetString() == "none");
            var root = json.RootElement.GetProperty("Root");
            Assert.True(root.TryGetProperty("DebugMetadata", out _));
            Assert.Equal(path, root.GetProperty("Identity").GetProperty("PackagePath").GetString());
            Assert.Equal("Canvas.img", root.GetProperty("Identity").GetProperty("ImageSelector").GetString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DiagnosticFormatter_ReturnsStableCliText()
    {
        var diagnostic = ResourceInspectionDiagnostics.ExportUnsupported(ResourceExportKind.Lua, "Text.img");

        var output = ResourceInspectionDiagnosticFormatter.Format(diagnostic);

        Assert.Equal(
            "error [wcx.export.unsupported]: Selected image is not a supported Lua IMG: Text.img. (Text.img)",
            output);
    }

    [Fact]
    public void DiagnosticFormatter_OmitsEmptyCodeAndPath()
    {
        var diagnostic = new ResourceInspectionDiagnostic(
            ResourceDiagnosticSeverities.Info,
            "Nothing to report.",
            Code: " ",
            Path: "");

        var output = ResourceInspectionDiagnosticFormatter.Format(diagnostic);

        Assert.Equal("info: Nothing to report.", output);
    }

    [Fact]
    public void DiagnosticsFactory_ReturnsStablePayloadDiagnostic()
    {
        var diagnostic = ResourceInspectionDiagnostics.CanvasPixelDecodingPartial("icon");

        Assert.Equal(ResourceDiagnosticSeverities.Info, diagnostic.Severity);
        Assert.Equal("Canvas pixel decoding is lazy and currently supports a narrow direct-zlib format slice.", diagnostic.Message);
        Assert.Equal("icon", diagnostic.Path);
        Assert.Equal(ResourceDiagnosticCodes.CanvasPixelDecodingPartial, diagnostic.Code);
        Assert.Equal(ResourceDiagnosticSources.Parser, diagnostic.Source);
    }

    [Fact]
    public void DiagnosticsFactory_ReturnsStableMediaPayloadDiagnostics()
    {
        var rawData = ResourceInspectionDiagnostics.RawDataPayloadDecodingUnsupported("raw");
        var video = ResourceInspectionDiagnostics.VideoPayloadDecodingUnsupported("clip");
        var audio = ResourceInspectionDiagnostics.AudioPayloadDecodingUnsupported("sound");

        Assert.Equal(ResourceDiagnosticSeverities.Info, rawData.Severity);
        Assert.Equal("RawData payload decoding is not implemented.", rawData.Message);
        Assert.Equal("raw", rawData.Path);
        Assert.Equal(ResourceDiagnosticCodes.RawDataPayloadDecodingUnsupported, rawData.Code);
        Assert.Equal(ResourceDiagnosticSources.Parser, rawData.Source);

        Assert.Equal(ResourceDiagnosticSeverities.Info, video.Severity);
        Assert.Equal("Video payload decoding is not implemented.", video.Message);
        Assert.Equal("clip", video.Path);
        Assert.Equal(ResourceDiagnosticCodes.VideoPayloadDecodingUnsupported, video.Code);
        Assert.Equal(ResourceDiagnosticSources.Parser, video.Source);

        Assert.Equal(ResourceDiagnosticSeverities.Info, audio.Severity);
        Assert.Equal("Audio payload decoding is not implemented.", audio.Message);
        Assert.Equal("sound", audio.Path);
        Assert.Equal(ResourceDiagnosticCodes.AudioPayloadDecodingUnsupported, audio.Code);
        Assert.Equal(ResourceDiagnosticSources.Parser, audio.Source);
    }

    [Fact]
    public void DiagnosticsFactory_ReturnsStableCanvasLinkDiagnostic()
    {
        var diagnostic = ResourceInspectionDiagnostics.CanvasPreviewLinkUnresolved(
            "proxy/_outlink",
            "Proxy.img",
            "_outlink",
            "Map/Missing/Missing.img/icon");

        Assert.Equal(ResourceDiagnosticSeverities.Error, diagnostic.Severity);
        Assert.Equal("Canvas preview _outlink target could not be resolved: Map/Missing/Missing.img/icon.", diagnostic.Message);
        Assert.Equal("Proxy.img/proxy/_outlink", diagnostic.Path);
        Assert.Equal(ResourceDiagnosticCodes.CanvasPreviewLinkUnresolved, diagnostic.Code);
        Assert.Equal(ResourceDiagnosticSources.Viewer, diagnostic.Source);
    }

    [Fact]
    public void DiagnosticsFactory_ReturnsStablePackageGroupDiagnostics()
    {
        var missing = ResourceInspectionDiagnostics.PackageGroupShardMissing("Map1_001.wz");
        var invalid = ResourceInspectionDiagnostics.PackageGroupShardInvalid("Map1_000.wz");

        Assert.Equal(ResourceDiagnosticSeverities.Warning, missing.Severity);
        Assert.Equal("Package group shard is declared but missing: Map1_001.wz.", missing.Message);
        Assert.Equal("Map1_001.wz", missing.Path);
        Assert.Equal(ResourceDiagnosticCodes.PackageGroupShardMissing, missing.Code);
        Assert.Equal(ResourceDiagnosticSources.Inspection, missing.Source);

        Assert.Equal(ResourceDiagnosticSeverities.Warning, invalid.Severity);
        Assert.Equal("Package group shard could not be loaded: Map1_000.wz.", invalid.Message);
        Assert.Equal("Map1_000.wz", invalid.Path);
        Assert.Equal(ResourceDiagnosticCodes.PackageGroupShardInvalid, invalid.Code);
        Assert.Equal(ResourceDiagnosticSources.Inspection, invalid.Source);
    }

    [Fact]
    public async Task CanvasImageService_LoadsSelectedCanvasPixels()
    {
        byte[] pixels = [0x10, 0x20, 0x30, 0xff, 0x40, 0x50, 0x60, 0xff];
        var path = MaterializeHexFixture("canvas-zlib.pkg1.hex", ".wz");
        var service = new ResourceCanvasImageService();

        try
        {
            var document = await service.LoadAsync(
                path,
                "Canvas.img",
                "icon",
                new ResourceInspectionOptions(WzStringEncryptionKind.None));

            Assert.Equal(path, document.SourcePath);
            Assert.Equal("Canvas.img", document.Selector);
            Assert.Equal("icon", document.ValuePath);
            Assert.Equal(2, document.Width);
            Assert.Equal(1, document.Height);
            Assert.Equal(2, document.Format);
            Assert.Equal("bgra8888", document.PixelFormat);
            Assert.Equal(8, document.Stride);
            Assert.Equal(pixels, document.Pixels);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CanvasImageService_ResolvesOutlinkCanvasPixels()
    {
        byte[] pixels = [0x10, 0x20, 0x30, 0xff];
        var directory = Directory.CreateTempSubdirectory("wcx-canvas-outlink-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Data", "Map", "CanvasSource"));
        var proxyDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Data", "Map", "Proxy"));
        var sourcePath = Path.Combine(sourceDirectory.FullName, "CanvasSource.wz");
        var proxyPath = Path.Combine(proxyDirectory.FullName, "Proxy.wz");
        await File.WriteAllBytesAsync(
            sourcePath,
            CreatePkg1ImagePackage(
                "Linked.img",
                CreatePropertyImage(CreateObjectProperty("icon", CreateObjectValue(
                    "Canvas",
                    0x00,
                    0x00,
                    1,
                    1,
                    2,
                    0x00,
                    1,
                    0,
                    (byte)0x00,
                    (byte)0x00,
                    BitConverter.GetBytes(CreateDirectZlibPayload(pixels).Length),
                    CreateDirectZlibPayload(pixels))))));
        await File.WriteAllBytesAsync(
            proxyPath,
            CreatePkg1ImagePackage(
                "Proxy.img",
                CreatePropertyImage(CreateLinkedCanvasProperty(
                    "proxy",
                    "_outlink",
                    "Map/CanvasSource/Linked.img/icon"))));
        var service = new ResourceCanvasImageService();

        try
        {
            var document = await service.LoadAsync(
                proxyPath,
                "Proxy.img",
                "proxy/_outlink",
                new ResourceInspectionOptions(WzStringEncryptionKind.None));

            Assert.Equal(sourcePath, document.SourcePath);
            Assert.Equal("Linked.img", document.Selector);
            Assert.Equal("icon", document.ValuePath);
            Assert.Equal(pixels, document.Pixels);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task CanvasImageService_LoadsMsCanvasPixels()
    {
        byte[] pixels = [0x10, 0x20, 0x30, 0xff];
        var path = Path.Combine(Path.GetTempPath(), $"Mob_00000-{Guid.NewGuid():N}.ms");
        var imageBytes = MsContainerFixture.CreateCanvasPropertyImage("icon", pixels);
        await File.WriteAllBytesAsync(
            path,
            MsContainerFixture.CreateV4(
                Path.GetFileName(path),
                new MsContainerFixture.Entry(
                    "Mob/1150000.img",
                    0,
                    imageBytes.Length,
                    1024,
                    Payload: imageBytes)),
            CancellationToken.None);
        var service = new ResourceCanvasImageService();

        try
        {
            var document = await service.LoadAsync(
                path,
                "Mob/1150000.img",
                "icon",
                new ResourceInspectionOptions(WzStringEncryptionKind.None));

            Assert.Equal(path, document.SourcePath);
            Assert.Equal("Mob/1150000.img", document.Selector);
            Assert.Equal("icon", document.ValuePath);
            Assert.Equal(pixels, document.Pixels);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CanvasImageService_ResolvesMsOutlinkCanvasPixels()
    {
        byte[] pixels = [0x10, 0x20, 0x30, 0xff];
        var path = Path.Combine(Path.GetTempPath(), $"Mob_00000-{Guid.NewGuid():N}.ms");
        var proxyBytes = MsContainerFixture.CreateLinkedCanvasPropertyImage(
            "proxy",
            "_outlink",
            "Mob/_Canvas/1150000.img/icon");
        var linkedBytes = MsContainerFixture.CreateCanvasPropertyImage("icon", pixels);
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
            CancellationToken.None);
        var service = new ResourceCanvasImageService();

        try
        {
            var inspection = await new WzMsContainerInspectionReader().ReadAsync(path, CancellationToken.None);
            Assert.Contains(inspection.Entries, entry => entry.Path == "Mob/_Canvas/1150000.img");

            var document = await service.LoadAsync(
                path,
                "Mob/1150000.img",
                "proxy/_outlink",
                new ResourceInspectionOptions(WzStringEncryptionKind.None));

            Assert.Equal(path, document.SourcePath);
            Assert.Equal("Mob/_Canvas/1150000.img", document.Selector);
            Assert.Equal("icon", document.ValuePath);
            Assert.Equal(pixels, document.Pixels);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CanvasImageService_ReportsUnresolvedOutlinkCanvasTarget()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-canvas-unresolved-outlink-");
        var proxyDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "Data", "Map", "Proxy"));
        var proxyPath = Path.Combine(proxyDirectory.FullName, "Proxy.wz");
        await File.WriteAllBytesAsync(
            proxyPath,
            CreatePkg1ImagePackage(
                "Proxy.img",
                CreatePropertyImage(CreateLinkedCanvasProperty(
                    "proxy",
                    "_outlink",
                    "Map\\Missing\\Missing.img\\icon"))));
        var service = new ResourceCanvasImageService();

        try
        {
            var exception = await Assert.ThrowsAsync<ResourceCanvasImageException>(() =>
                service.LoadAsync(
                    proxyPath,
                    "Proxy.img",
                    "proxy/_outlink",
                    new ResourceInspectionOptions(WzStringEncryptionKind.None)));

            Assert.Equal(ResourceDiagnosticCodes.CanvasPreviewLinkUnresolved, exception.Diagnostic.Code);
            Assert.Equal(ResourceDiagnosticSources.Viewer, exception.Diagnostic.Source);
            Assert.Equal("Proxy.img/proxy/_outlink", exception.Diagnostic.Path);
            Assert.Contains("Map/Missing/Missing.img/icon", exception.Diagnostic.Message);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task CanvasImageService_LoadsRootCanvasPixelsWithoutValueSelector()
    {
        byte[] pixels = [0x10, 0x20, 0x30, 0xff];
        var path = WriteTemporaryPkg1ImageFile(CreateCanvasImage(pixels, width: 1));
        var service = new ResourceCanvasImageService();

        try
        {
            var document = await service.LoadAsync(
                path,
                "Canvas.img",
                valueSelector: null,
                new ResourceInspectionOptions(WzStringEncryptionKind.None));

            Assert.Equal("Canvas.img", document.Selector);
            Assert.Null(document.ValuePath);
            Assert.Equal(1, document.Width);
            Assert.Equal(1, document.Height);
            Assert.Equal(pixels, document.Pixels);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CanvasImageService_ConvertsFormat1CanvasToBgra8888()
    {
        byte[] rawPixels = [0x21, 0xf3];
        byte[] bgraPixels = [0x11, 0x22, 0x33, 0xff];
        var path = WriteTemporaryPkg1ImageFile(CreateCanvasImage(rawPixels, width: 1, format: 1));
        var service = new ResourceCanvasImageService();

        try
        {
            var document = await service.LoadAsync(
                path,
                "Canvas.img",
                valueSelector: null,
                new ResourceInspectionOptions(WzStringEncryptionKind.None));

            Assert.Equal(1, document.Format);
            Assert.Equal("bgra8888", document.PixelFormat);
            Assert.Equal(bgraPixels, document.Pixels);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CanvasImageService_ConvertsFormat257CanvasToBgra8888()
    {
        byte[] rawPixels = [0x00, 0xfc, 0xff, 0x7f];
        byte[] bgraPixels =
        [
            0x00, 0x00, 0xff, 0xff,
            0xff, 0xff, 0xff, 0x00
        ];
        var path = WriteTemporaryPkg1ImageFile(CreateCanvasImage(rawPixels, width: 2, format: 257));
        var service = new ResourceCanvasImageService();

        try
        {
            var document = await service.LoadAsync(
                path,
                "Canvas.img",
                valueSelector: null,
                new ResourceInspectionOptions(WzStringEncryptionKind.None));

            Assert.Equal(257, document.Format);
            Assert.Equal("bgra8888", document.PixelFormat);
            Assert.Equal(bgraPixels, document.Pixels);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CanvasImageService_ConvertsFormat513CanvasToBgra8888()
    {
        byte[] rawPixels = [0x00, 0xf8, 0xe0, 0x07, 0x1f, 0x00];
        byte[] bgraPixels =
        [
            0x00, 0x00, 0xff, 0xff,
            0x00, 0xff, 0x00, 0xff,
            0xff, 0x00, 0x00, 0xff
        ];
        var path = WriteTemporaryPkg1ImageFile(CreateCanvasImage(rawPixels, width: 3, format: 513));
        var service = new ResourceCanvasImageService();

        try
        {
            var document = await service.LoadAsync(
                path,
                "Canvas.img",
                valueSelector: null,
                new ResourceInspectionOptions(WzStringEncryptionKind.None));

            Assert.Equal(513, document.Format);
            Assert.Equal("bgra8888", document.PixelFormat);
            Assert.Equal(bgraPixels, document.Pixels);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CanvasImageService_ConvertsFormat2050CanvasToBgra8888()
    {
        const ulong alphaBits = 0 | (1UL << 3) | (6UL << 6) | (7UL << 9);
        const uint colorBits = 0 | (1u << 2) | (2u << 4) | (3u << 6);
        var rawPixels = CreateDxt5Block(
            alpha0: 10,
            alpha1: 250,
            color0: 0xf800,
            color1: 0x07e0,
            alphaBits: alphaBits,
            colorBits: colorBits);
        byte[] bgraPixels =
        [
            0x00, 0x00, 0xff, 0x0a,
            0x00, 0xff, 0x00, 0xfa,
            0x00, 0x55, 0xaa, 0x00,
            0x00, 0xaa, 0x55, 0xff
        ];
        var path = WriteTemporaryPkg1ImageFile(CreateCanvasImage(rawPixels, width: 4, height: 1, format: 2050));
        var service = new ResourceCanvasImageService();

        try
        {
            var document = await service.LoadAsync(
                path,
                "Canvas.img",
                valueSelector: null,
                new ResourceInspectionOptions(WzStringEncryptionKind.None));

            Assert.Equal(2050, document.Format);
            Assert.Equal("bgra8888", document.PixelFormat);
            Assert.Equal(bgraPixels, document.Pixels);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DiagnosticsFactory_ReturnsStableExportDiagnostic()
    {
        var diagnostic = ResourceInspectionDiagnostics.ExportLuaMultipleBlocks(2, "Script.lua");

        Assert.Equal(ResourceDiagnosticSeverities.Info, diagnostic.Severity);
        Assert.Equal("Exported 2 Lua blocks in stream order.", diagnostic.Message);
        Assert.Equal("Script.lua", diagnostic.Path);
        Assert.Equal(ResourceDiagnosticCodes.ExportLuaMultipleBlocks, diagnostic.Code);
        Assert.Equal(ResourceDiagnosticSources.Export, diagnostic.Source);
    }

    private static string FixturePath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "fixtures", "synthetic", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate fixture '{fileName}'.");
    }

    private static string MaterializeHexFixture(string fileName, string extension)
    {
        var hex = new StringBuilder();
        foreach (var ch in File.ReadAllText(FixturePath(fileName)))
        {
            if (Uri.IsHexDigit(ch))
            {
                hex.Append(ch);
            }
        }

        var path = Path.Combine(Path.GetTempPath(), $"wcx-resource-fixture-{Guid.NewGuid():N}{extension}");
        File.WriteAllBytes(path, Convert.FromHexString(hex.ToString()));
        return path;
    }

    private static string WriteTemporaryPkg1ImageFile(byte[] imageBytes)
    {
        var directoryData = CreateDirectoryDataForImage("Canvas.img", imageBytes.Length - 4);
        var header = CreateHeader("PKG1", string.Empty, dataSize: directoryData.Length + imageBytes.Length);
        var path = Path.Combine(Path.GetTempPath(), $"wcx-inspect-image-{Guid.NewGuid():N}.wz");
        File.WriteAllBytes(path, [.. header, .. directoryData, .. imageBytes]);
        return path;
    }

    private static string WriteTemporaryPkg2File()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wcx-inspect-pkg2-{Guid.NewGuid():N}.wz");
        File.WriteAllBytes(path, CreatePkg2("Copyright", hash1: 0x11223344, hash2: 0xaabbccdd));
        return path;
    }

    private static string WriteTemporaryPkg2File(params Pkg2PackageFixture.Entry[] entries)
    {
        var path = Path.Combine(Path.GetTempPath(), $"wcx-inspect-pkg2-{Guid.NewGuid():N}.wz");
        File.WriteAllBytes(path, Pkg2PackageFixture.CreateKmst1200(entries));
        return path;
    }

    private static string WriteTemporaryModernPkg2File(params Pkg2PackageFixture.Entry[] entries)
    {
        var path = Path.Combine(Path.GetTempPath(), $"wcx-inspect-modern-pkg2-{Guid.NewGuid():N}.wz");
        File.WriteAllBytes(path, Pkg2PackageFixture.CreateModernKms(entries));
        return path;
    }

    private static byte[] CreatePkg2(string copyright, uint hash1, uint hash2)
    {
        var header = CreateHeader("PKG2", copyright, dataSize: 1);
        var hashBytes = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(hashBytes.AsSpan(0, sizeof(uint)), hash1);
        BinaryPrimitives.WriteUInt32LittleEndian(hashBytes.AsSpan(sizeof(uint), sizeof(uint)), hash2);
        return [.. header, .. hashBytes, 0x00];
    }

    private static byte[] CreateDirectoryDataForImage(string name, int imageSize)
    {
        var bytes = new List<byte> { 0x01, 0x04 };
        AddWzString(bytes, name);
        AddCompressedInt32(bytes, imageSize);
        bytes.Add(0x00);
        var hashOffsetPosition = bytes.Count + 16;
        var imageOffset = 16 + bytes.Count + sizeof(uint) + 4;
        var hashOffset = CreateHashOffset(
            hashOffsetPosition: checked((uint)hashOffsetPosition),
            desiredOffset: checked((uint)imageOffset));
        bytes.AddRange(BitConverter.GetBytes(hashOffset));
        return bytes.ToArray();
    }

    private static byte[] CreateDirectoryStub(string name)
    {
        var bytes = new List<byte> { 0x01, 0x03 };
        AddWzString(bytes, name);
        bytes.Add(0x00);
        bytes.Add(0x00);
        bytes.AddRange(BitConverter.GetBytes(0u));
        bytes.Add(0x00);
        return bytes.ToArray();
    }

    private static byte[] CreateDirectoryStubs(params string[] names)
    {
        var bytes = new List<byte> { checked((byte)names.Length) };
        foreach (var name in names)
        {
            bytes.Add(0x03);
            AddWzString(bytes, name);
            bytes.Add(0x00);
            bytes.Add(0x00);
            bytes.AddRange(BitConverter.GetBytes(0u));
        }

        foreach (var _ in names)
        {
            bytes.Add(0x00);
        }

        return bytes.ToArray();
    }

    private static byte[] CreateNestedDirectoryStub(string parentName, string childName)
    {
        var bytes = new List<byte> { 0x01, 0x03 };
        AddWzString(bytes, parentName);
        bytes.Add(0x00);
        bytes.Add(0x00);
        bytes.AddRange(BitConverter.GetBytes(0u));
        bytes.Add(0x01);
        bytes.Add(0x03);
        AddWzString(bytes, childName);
        bytes.Add(0x00);
        bytes.Add(0x00);
        bytes.AddRange(BitConverter.GetBytes(0u));
        bytes.Add(0x00);
        return bytes.ToArray();
    }

    private static byte[] CreateImageDirectory(string name)
    {
        var bytes = new List<byte> { 0x01, 0x04 };
        AddWzString(bytes, name);
        bytes.Add(0x01);
        bytes.Add(0x00);
        bytes.AddRange(BitConverter.GetBytes(0u));
        return bytes.ToArray();
    }

    private static byte[] CreatePkg1DirectoryPackage(byte[] directoryData)
    {
        byte[] encryptedVersion = [0x7b, 0x00];
        var header = CreateHeader("PKG1", string.Empty, dataSize: encryptedVersion.Length + directoryData.Length);
        return [.. header, .. encryptedVersion, .. directoryData];
    }

    private static byte[] CreatePkg1ImagePackage(string imageName, byte[] imageBytes)
    {
        var directoryData = CreateDirectoryDataForImage(imageName, imageBytes.Length - 4);
        var header = CreateHeader("PKG1", string.Empty, dataSize: directoryData.Length + imageBytes.Length);
        return [.. header, .. directoryData, .. imageBytes];
    }

    private static uint CreateHashOffset(uint hashOffsetPosition, uint desiredOffset)
    {
        const uint headerSize = 16;
        var hashVersion = WzPkg1VersionHash.CalculateHashVersion(777);
        unchecked
        {
            var offset = hashOffsetPosition - headerSize;
            offset = ~offset;
            offset *= hashVersion;
            offset -= 0x581C3F6D;
            var distance = (int)offset & 0x1F;
            offset = (offset << distance) | (offset >> (32 - distance));
            return offset ^ (desiredOffset - headerSize * 2);
        }
    }

    private static byte[] CreatePropertyImage(params byte[][] entries)
    {
        var bytes = new List<byte>(CreateImage("Property"));
        bytes.Add(0x00);
        bytes.Add(0x00);
        bytes.Add((byte)entries.Length);
        foreach (var entry in entries)
        {
            bytes.AddRange(entry);
        }

        return bytes.ToArray();
    }

    private static byte[] CreateCanvasImage(byte[] pixels, int width, int height = 1, int format = 2)
    {
        var payload = CreateDirectZlibPayload(pixels);
        return CreateImage(
            "Canvas",
            0x00,
            0x00,
            CreateCompressedInt32(width),
            CreateCompressedInt32(height),
            CreateCompressedInt32(format),
            0x00,
            CreateCompressedInt32(1),
            CreateCompressedInt32(0),
            (byte)0x00,
            (byte)0x00,
            BitConverter.GetBytes(payload.Length),
            payload);
    }

    private static byte[] CreateLinkedCanvasProperty(string name, string linkName, string linkValue)
    {
        byte[] pixels = [0x00, 0x00, 0x00, 0x00];
        var payload = CreateDirectZlibPayload(pixels);
        return CreateObjectProperty(
            name,
            CreateObjectValue(
                "Canvas",
                0x00,
                0x01,
                0x00,
                0x00,
                1,
                CreateImageString(linkName),
                0x08,
                CreateImageString(linkValue),
                1,
                1,
                2,
                0x00,
                1,
                0,
                (byte)0x00,
                (byte)0x00,
                BitConverter.GetBytes(payload.Length),
                payload));
    }

    private static byte[] CreateRawDataProperty()
    {
        return CreateObjectProperty(
            "raw",
            CreateObjectValue(
                "RawData",
                0,
                3,
                0x01,
                0x02,
                0x03));
    }

    private static byte[] CreateVideoProperty()
    {
        return CreateObjectProperty(
            "clip",
            CreateObjectValue(
                "Canvas#Video",
                0x00,
                0x00,
                5,
                4,
                0x01,
                0x02,
                0x03,
                0x04));
    }

    private static byte[] CreateSoundProperty()
    {
        return CreateObjectProperty(
            "sound",
            CreateObjectValue(
                "Sound_DX8",
                0,
                3,
                60,
                2,
                CreateBytes(0x00, 16),
                CreateBytes(0x01, 16),
                0x01,
                0x00,
                CreateBytes(0x02, 16),
                4,
                CreateBytes(0x03, 4),
                0x10,
                0x11,
                0x12));
    }

    private static byte[] CreateDirectZlibPayload(byte[] pixels)
    {
        using var output = new MemoryStream();
        output.WriteByte(0x00);
        using (var zlib = new System.IO.Compression.ZLibStream(output, System.IO.Compression.CompressionMode.Compress, leaveOpen: true))
        {
            zlib.Write(pixels);
        }

        return output.ToArray();
    }

    private static byte[] CreateObjectProperty(string name, byte[] objectValue)
    {
        var bytes = new List<byte>();
        bytes.AddRange(CreateImageString(name));
        bytes.Add(0x09);
        bytes.AddRange(BitConverter.GetBytes(objectValue.Length));
        bytes.AddRange(objectValue);
        return bytes.ToArray();
    }

    private static byte[] CreateScalarProperty(string name, int value)
    {
        var bytes = new List<byte>();
        bytes.AddRange(CreateImageString(name));
        bytes.Add(0x03);
        bytes.Add((byte)value);
        return bytes.ToArray();
    }

    private static byte[] CreateObjectValue(string objectType, params object[] payloadParts)
    {
        var bytes = new List<byte>();
        AddImageObjectName(bytes, objectType);
        AddPayloadParts(bytes, payloadParts);
        return bytes.ToArray();
    }

    private static byte[] CreateImage(string objectType, params object[] payloadParts)
    {
        var bytes = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
        AddImageObjectName(bytes, objectType);
        AddPayloadParts(bytes, payloadParts);
        return bytes.ToArray();
    }

    private static byte[] CreateImageString(string value)
    {
        var bytes = new List<byte> { 0x00 };
        AddWzString(bytes, value);
        return bytes.ToArray();
    }

    private static void AddPayloadParts(List<byte> bytes, params object[] payloadParts)
    {
        foreach (var part in payloadParts)
        {
            switch (part)
            {
                case byte value:
                    bytes.Add(value);
                    break;
                case int value:
                    bytes.Add((byte)value);
                    break;
                case byte[] value:
                    bytes.AddRange(value);
                    break;
                default:
                    throw new ArgumentException($"Unsupported payload part type: {part.GetType()}.");
            }
        }
    }

    private static byte[] CreateBytes(byte value, int count)
    {
        return Enumerable.Repeat(value, count).ToArray();
    }

    private static byte[] CreateDxt5Block(
        byte alpha0 = 255,
        byte alpha1 = 0,
        ushort color0 = 0xf800,
        ushort color1 = 0,
        ulong alphaBits = 0,
        uint colorBits = 0)
    {
        return
        [
            alpha0,
            alpha1,
            (byte)(alphaBits & 0xff),
            (byte)((alphaBits >> 8) & 0xff),
            (byte)((alphaBits >> 16) & 0xff),
            (byte)((alphaBits >> 24) & 0xff),
            (byte)((alphaBits >> 32) & 0xff),
            (byte)((alphaBits >> 40) & 0xff),
            (byte)(color0 & 0xff),
            (byte)(color0 >> 8),
            (byte)(color1 & 0xff),
            (byte)(color1 >> 8),
            (byte)(colorBits & 0xff),
            (byte)((colorBits >> 8) & 0xff),
            (byte)((colorBits >> 16) & 0xff),
            (byte)((colorBits >> 24) & 0xff)
        ];
    }

    private static byte[] CreateCompressedInt32(int value)
    {
        var bytes = new List<byte>();
        AddCompressedInt32(bytes, value);
        return bytes.ToArray();
    }

    private static void AddCompressedInt32(List<byte> bytes, int value)
    {
        if (value > sbyte.MinValue && value <= sbyte.MaxValue)
        {
            bytes.Add(unchecked((byte)(sbyte)value));
            return;
        }

        bytes.Add(0x80);
        bytes.AddRange(BitConverter.GetBytes(value));
    }

    private static void AddImageObjectName(List<byte> bytes, string value)
    {
        bytes.Add(0x73);
        AddWzString(bytes, value);
    }

    private static void AddWzString(List<byte> bytes, string value)
    {
        bytes.Add(unchecked((byte)(sbyte)-value.Length));
        for (var i = 0; i < value.Length; i++)
        {
            bytes.Add((byte)(value[i] ^ (byte)(0xAA + i)));
        }
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
