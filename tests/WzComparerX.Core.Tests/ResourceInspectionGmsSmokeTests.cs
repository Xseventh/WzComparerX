using WzComparerX.Core;

namespace WzComparerX.Core.Tests;

public class ResourceInspectionGmsSmokeTests
{
    [Fact]
    public async Task InspectOptionalGmsPackageRoots_ReadsRepresentativePackageFamilies()
    {
        var dataDirectory = GetGmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var samples = new[]
        {
            "Character/Character.wz",
            "Effect/Effect.wz",
            "Item/Item.wz",
            "Mob/Mob.wz",
            "Npc/Npc.wz",
            "Skill/Skill.wz",
            "Sound/Sound.wz",
            "String/String.wz",
            "UI/UI.wz"
        };
        var service = new ResourceInspectionService();
        var inspectedCount = 0;

        foreach (var sample in samples)
        {
            var path = Path.Combine(dataDirectory, sample.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                continue;
            }

            var inspection = await service.InspectAsync(
                path,
                selector: null,
                new ResourceInspectionOptions(StringKey: null, IncludeDebugMetadata: true));

            Assert.Equal("pkg1", inspection.Format);
            Assert.Equal("package", inspection.Root.Kind);
            Assert.NotEmpty(inspection.Root.Children);
            AssertNoErrorDiagnostics(inspection);
            inspectedCount++;
        }

        Assert.True(inspectedCount > 0, "No representative GMS package roots were found.");
    }

    [Fact]
    public async Task InspectOptionalGmsMapPackageGroup_ExposesMergedShardImageIdentity()
    {
        var dataDirectory = GetGmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var map1Path = Path.Combine(dataDirectory, "Map", "Map", "Map1", "Map1.wz");
        if (!File.Exists(map1Path))
        {
            return;
        }

        var service = new ResourceInspectionService();

        var inspection = await service.InspectAsync(
            map1Path,
            selector: null,
            new ResourceInspectionOptions(StringKey: null, IncludeDebugMetadata: true));

        var image = Assert.Single(inspection.Root.Children, child => child.Name == "100000000.img");
        Assert.Equal("image", image.Kind);
        Assert.NotNull(image.Identity);
        Assert.EndsWith("Map1_000.wz", image.Identity.PackagePath, StringComparison.Ordinal);
        Assert.Equal("100000000.img", image.Identity.ImageSelector);
        Assert.EndsWith("Map1_000.wz/100000000.img", image.Path, StringComparison.Ordinal);
        AssertNoErrorDiagnostics(inspection);
    }

    [Fact]
    public async Task InspectOptionalGmsMapImage_ResolvesMiniMapOutlinkIdentity()
    {
        var dataDirectory = GetGmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var map1ShardPath = Path.Combine(dataDirectory, "Map", "Map", "Map1", "Map1_000.wz");
        if (!File.Exists(map1ShardPath))
        {
            return;
        }

        var service = new ResourceInspectionService();

        var inspection = await service.InspectAsync(
            map1ShardPath,
            "100000000.img",
            new ResourceInspectionOptions(StringKey: null, IncludeDebugMetadata: true));

        var outlink = Flatten(inspection.Root)
            .FirstOrDefault(node => node is { Name: "_outlink", Kind: "string" });
        Assert.NotNull(outlink);
        Assert.NotNull(outlink.Identity);
        Assert.NotNull(outlink.Identity.LinkedTarget);
        Assert.NotNull(outlink.Identity.ResolvedLinkedTarget);
        Assert.Equal("100000000.img", outlink.Identity.ResolvedLinkedTarget.ImageSelector);
        Assert.Equal("miniMap/canvas", outlink.Identity.ResolvedLinkedTarget.ValuePath);
        AssertNoErrorDiagnostics(inspection);
    }

    [Fact]
    public async Task InspectOptionalGmsMsPackContainers_ReadsDirectoryTables()
    {
        var dataDirectory = GetGmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var packsDirectory = Path.Combine(dataDirectory, "Packs");
        if (!Directory.Exists(packsDirectory))
        {
            return;
        }

        var msPaths = Directory.GetFiles(packsDirectory, "*.ms");
        if (msPaths.Length == 0)
        {
            return;
        }

        var service = new ResourceInspectionService();
        foreach (var msPath in msPaths)
        {
            var inspection = await service.InspectAsync(
                msPath,
                selector: null,
                new ResourceInspectionOptions(StringKey: null, IncludeDebugMetadata: true));

            Assert.Equal("ms", inspection.Format);
            Assert.Equal("package", inspection.Root.Kind);
            Assert.NotEmpty(inspection.Root.Children);
            Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "version" && Equals(item.Value, 2));
            Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "entryCount");
            AssertNoErrorDiagnostics(inspection);
        }
    }

    [Fact]
    public async Task InspectOptionalGmsMsPackImage_ExtractsImagePayload()
    {
        var dataDirectory = GetGmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var msPath = Path.Combine(dataDirectory, "Packs", "Skill_00002.ms");
        if (!File.Exists(msPath))
        {
            return;
        }

        var service = new ResourceInspectionService();
        var inspection = await service.InspectAsync(
            msPath,
            "Skill/15500.img",
            new ResourceInspectionOptions(StringKey: null, IncludeDebugMetadata: true));

        Assert.Equal("ms", inspection.Format);
        Assert.Equal("image", inspection.Root.Kind);
        Assert.Equal("Property", inspection.Root.DisplayValue);
        Assert.Equal("Skill/15500.img", inspection.Root.Identity?.ImageSelector);
        Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "version" && Equals(item.Value, 2));
        Assert.Contains(inspection.Root.Children, child => child.Name == "info" && child.Kind == "object");
        Assert.Contains(inspection.Root.Children, child => child.Name == "skill" && child.Kind == "object");
        AssertNoErrorDiagnostics(inspection);
    }

    [Fact]
    public async Task InspectOptionalGmsStringEqpImage_ReadsStringLinkerShape()
    {
        var dataDirectory = GetGmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var stringPath = Path.Combine(dataDirectory, "String", "String_000.wz");
        if (!File.Exists(stringPath))
        {
            return;
        }

        var service = new ResourceInspectionService();
        var inspection = await service.InspectAsync(
            stringPath,
            "Eqp.img",
            new ResourceInspectionOptions(
                StringKey: null,
                MaxPropertyDepth: 2,
                IncludeDebugMetadata: true));

        var eqp = Assert.Single(inspection.Root.Children, child => child.Name == "Eqp");
        Assert.Equal("object", eqp.Kind);
        Assert.Contains(eqp.Children, child => child is { Name: "Cap", Kind: "object" });
        Assert.Contains(eqp.Children, child => child is { Name: "Weapon", Kind: "object" });
        Assert.Contains(eqp.Children, child => child is { Name: "Accessory", Kind: "object" });
        AssertNoErrorDiagnostics(inspection);
    }

    [Fact]
    public async Task InspectOptionalGmsItemSkillOptionImage_ReadsSkillOptionScalars()
    {
        var dataDirectory = GetGmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var itemPath = Path.Combine(dataDirectory, "Item", "Item_000.wz");
        if (!File.Exists(itemPath))
        {
            return;
        }

        var service = new ResourceInspectionService();
        var inspection = await service.InspectAsync(
            itemPath,
            "SkillOption.img",
            new ResourceInspectionOptions(
                StringKey: null,
                MaxPropertyDepth: 3,
                IncludeDebugMetadata: true));

        Assert.Contains(inspection.Root.Children, child => child is { Name: "skill", Kind: "object" });
        Assert.Contains(inspection.Root.Children, child => child is { Name: "socket", Kind: "object" });
        Assert.Contains(inspection.Root.Children, child => child is { Name: "inc", Kind: "object" });
        Assert.Contains(Flatten(inspection.Root), node => node is { Name: "skillId", Kind: "int32" });
        Assert.Contains(Flatten(inspection.Root), node => node is { Name: "reqLevel", Kind: "int32" });
        AssertNoErrorDiagnostics(inspection);
    }

    [Fact]
    public async Task CanvasOptionalGmsMsPackImage_ResolvesOutlinkPreview()
    {
        var dataDirectory = GetGmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var msPath = Path.Combine(dataDirectory, "Packs", "Mob_00000.ms");
        if (!File.Exists(msPath))
        {
            return;
        }

        var service = new ResourceCanvasImageService();
        var document = await service.LoadAsync(
            msPath,
            "Mob/1150000.img",
            "move/0/_outlink",
            new ResourceInspectionOptions(StringKey: null));

        Assert.True(
            document.SourcePath.EndsWith(".wz", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(msPath, document.SourcePath, StringComparison.Ordinal),
            $"Unexpected linked source path: {document.SourcePath}");
        Assert.EndsWith("1150000.img", document.Selector, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("move/0", document.ValuePath);
        Assert.True(document.Width > 0);
        Assert.True(document.Height > 0);
        Assert.NotEmpty(document.Pixels);
    }

    [Fact]
    public async Task InspectOptionalGmsEffectImage_ResolvesCanvasOutlinkIdentity()
    {
        var dataDirectory = GetGmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var effectPath = Path.Combine(dataDirectory, "Effect", "Effect_000.wz");
        if (!File.Exists(effectPath))
        {
            return;
        }

        var service = new ResourceInspectionService();
        var inspection = await service.InspectAsync(
            effectPath,
            "BasicEff.img",
            new ResourceInspectionOptions(
                StringKey: null,
                MaxPropertyDepth: 4,
                IncludeDebugMetadata: true));

        var outlink = Flatten(inspection.Root)
            .FirstOrDefault(node =>
                node is { Name: "_outlink", Kind: "string" } &&
                string.Equals(node.Identity?.LinkedTarget, "Effect/_Canvas/BasicEff.img/scout/back/0", StringComparison.Ordinal));

        Assert.NotNull(outlink);
        Assert.NotNull(outlink.Identity?.ResolvedLinkedTarget);
        Assert.EndsWith(
            Path.Combine("Effect", "_Canvas", "_Canvas_002.wz"),
            outlink.Identity.ResolvedLinkedTarget.PackagePath,
            StringComparison.Ordinal);
        Assert.Equal("BasicEff.img", outlink.Identity.ResolvedLinkedTarget.ImageSelector);
        Assert.Equal("scout/back/0", outlink.Identity.ResolvedLinkedTarget.ValuePath);
        AssertNoErrorDiagnostics(inspection);
    }

    [Fact]
    public async Task CanvasOptionalGmsEffectImage_ResolvesOutlinkPreview()
    {
        var dataDirectory = GetGmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var effectPath = Path.Combine(dataDirectory, "Effect", "Effect_000.wz");
        if (!File.Exists(effectPath))
        {
            return;
        }

        var service = new ResourceCanvasImageService();
        var document = await service.LoadAsync(
            effectPath,
            "BasicEff.img",
            "scout/back/0/_outlink",
            new ResourceInspectionOptions(StringKey: null));

        Assert.EndsWith(
            Path.Combine("Effect", "_Canvas", "_Canvas_002.wz"),
            document.SourcePath,
            StringComparison.Ordinal);
        Assert.Equal("BasicEff.img", document.Selector);
        Assert.Equal("scout/back/0", document.ValuePath);
        Assert.True(document.Width > 0);
        Assert.True(document.Height > 0);
        Assert.NotEmpty(document.Pixels);
    }

    [Fact]
    public async Task InspectOptionalGmsCharacterImage_ResolvesCanvasOutlinkIdentity()
    {
        var dataDirectory = GetGmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var characterPath = Path.Combine(dataDirectory, "Character", "Character_000.wz");
        if (!File.Exists(characterPath))
        {
            return;
        }

        var service = new ResourceInspectionService();
        var inspection = await service.InspectAsync(
            characterPath,
            "00002000.img",
            new ResourceInspectionOptions(StringKey: null, IncludeDebugMetadata: true));

        var outlink = Flatten(inspection.Root)
            .FirstOrDefault(node =>
                node is { Name: "_outlink", Kind: "string" } &&
                string.Equals(node.Identity?.ResolvedLinkedTarget?.ValuePath, "walk1/0/body", StringComparison.Ordinal));

        Assert.NotNull(outlink);
        Assert.NotNull(outlink.Identity);
        Assert.Equal("Character/_Canvas/00002000.img/walk1/0/body", outlink.Identity.LinkedTarget);
        Assert.NotNull(outlink.Identity.ResolvedLinkedTarget);
        Assert.EndsWith(
            Path.Combine("Character", "_Canvas", "_Canvas_000.wz"),
            outlink.Identity.ResolvedLinkedTarget.PackagePath,
            StringComparison.Ordinal);
        Assert.Equal("00002000.img", outlink.Identity.ResolvedLinkedTarget.ImageSelector);
        Assert.Equal("walk1/0/body", outlink.Identity.ResolvedLinkedTarget.ValuePath);
        AssertNoErrorDiagnostics(inspection);
    }

    [Fact]
    public async Task CanvasOptionalGmsCharacterImage_ResolvesOutlinkPreview()
    {
        var dataDirectory = GetGmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var characterPath = Path.Combine(dataDirectory, "Character", "Character_000.wz");
        if (!File.Exists(characterPath))
        {
            return;
        }

        var service = new ResourceCanvasImageService();
        var document = await service.LoadAsync(
            characterPath,
            "00002000.img",
            "walk1/0/body/_outlink",
            new ResourceInspectionOptions(StringKey: null));

        Assert.EndsWith(
            Path.Combine("Character", "_Canvas", "_Canvas_000.wz"),
            document.SourcePath,
            StringComparison.Ordinal);
        Assert.Equal("00002000.img", document.Selector);
        Assert.Equal("walk1/0/body", document.ValuePath);
        Assert.True(document.Width > 0);
        Assert.True(document.Height > 0);
        Assert.NotEmpty(document.Pixels);
    }

    [Fact]
    public async Task InspectOptionalGmsSoundImage_ReadsSoundPayloadMetadata()
    {
        var dataDirectory = GetGmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var soundPath = Path.Combine(dataDirectory, "Sound", "Sound_000.wz");
        if (!File.Exists(soundPath))
        {
            return;
        }

        var service = new ResourceInspectionService();
        var inspection = await service.InspectAsync(
            soundPath,
            "AchievementEff.img",
            new ResourceInspectionOptions(StringKey: null, IncludeDebugMetadata: true));

        var sound = Flatten(inspection.Root).FirstOrDefault(node => node is { Name: "GradeUp", Kind: "sound" });

        Assert.NotNull(sound);
        Assert.Contains("duration=", sound.DisplayValue, StringComparison.Ordinal);
        Assert.Contains("dataLength=", sound.DisplayValue, StringComparison.Ordinal);
        Assert.Contains(sound.Diagnostics ?? [], diagnostic =>
            diagnostic.Code == ResourceDiagnosticCodes.AudioPayloadDecodingUnsupported &&
            diagnostic.Severity == ResourceDiagnosticSeverities.Info);
        Assert.Contains(sound.DebugMetadata ?? [], item => item.Name == "duration");
        Assert.Contains(sound.DebugMetadata ?? [], item => item.Name == "dataOffset");
        Assert.Contains(sound.DebugMetadata ?? [], item => item.Name == "dataLength");
        AssertNoErrorDiagnostics(inspection);
    }

    private static string? GetGmsDataDirectory()
    {
        var path = Environment.GetEnvironmentVariable("WCX_GMS_DATA_DIR");
        return string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)
            ? null
            : path;
    }

    private static IEnumerable<ResourceInspectionNode> Flatten(ResourceInspectionNode node)
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

    private static void AssertNoErrorDiagnostics(ResourceInspectionDocument document)
    {
        Assert.DoesNotContain(EnumerateDiagnostics(document), diagnostic =>
            string.Equals(diagnostic.Severity, ResourceDiagnosticSeverities.Error, StringComparison.Ordinal));
    }

    private static IEnumerable<ResourceInspectionDiagnostic> EnumerateDiagnostics(ResourceInspectionDocument document)
    {
        foreach (var diagnostic in document.Diagnostics ?? [])
        {
            yield return diagnostic;
        }

        foreach (var diagnostic in EnumerateDiagnostics(document.Root))
        {
            yield return diagnostic;
        }
    }

    private static IEnumerable<ResourceInspectionDiagnostic> EnumerateDiagnostics(ResourceInspectionNode node)
    {
        foreach (var diagnostic in node.Diagnostics ?? [])
        {
            yield return diagnostic;
        }

        foreach (var child in node.Children)
        {
            foreach (var diagnostic in EnumerateDiagnostics(child))
            {
                yield return diagnostic;
            }
        }
    }
}
