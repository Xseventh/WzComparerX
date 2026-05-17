using WzComparerX.Core;
using WzComparerX.Tests;

namespace WzComparerX.Core.Tests;

public class ResourceInspectionExternalClientSmokeTests
{
    [Fact]
    public async Task InspectOptionalExternalClientPackageRoots_ReadsRepresentativePackageFamilies()
    {
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

        foreach (var sample in samples)
        {
            foreach (var path in ExternalClientSmokeData.FindRelativeFiles(sample.Split('/')))
            {
                var inspection = await service.InspectAsync(
                    path,
                    selector: null,
                    new ResourceInspectionOptions(StringKey: null, IncludeDebugMetadata: true));

                Assert.Equal("pkg1", inspection.Format);
                Assert.Equal("package", inspection.Root.Kind);
                Assert.NotEmpty(inspection.Root.Children);
                AssertNoErrorDiagnostics(inspection);
            }
        }
    }

    [Fact]
    public async Task InspectOptionalExternalClientMapPackageGroup_ExposesMergedShardImageIdentity()
    {
        var map1Path = ExternalClientSmokeData.FindFirstFile("Map", "Map", "Map1", "Map1.wz");
        if (map1Path is null)
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
    public async Task InspectOptionalExternalClientMapImage_ResolvesMiniMapOutlinkIdentity()
    {
        var map1ShardPath = ExternalClientSmokeData.FindFirstFile("Map", "Map", "Map1", "Map1_000.wz");
        if (map1ShardPath is null)
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
    public async Task InspectOptionalExternalClientMsPackContainers_ReadsDirectoryTables()
    {
        var msPaths = ExternalClientSmokeData.FindFiles("*.ms", "Packs");
        if (msPaths.Count == 0)
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
    public async Task InspectOptionalExternalClientMsPackImage_ExtractsImagePayload()
    {
        var msPath = ExternalClientSmokeData.FindFirstFile("Packs", "Skill_00002.ms");
        if (msPath is null)
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
    public async Task InspectOptionalExternalClientStringEqpImage_ReadsStringLinkerShape()
    {
        var stringPath = ExternalClientSmokeData.FindFirstFile("String", "String_000.wz");
        if (stringPath is null)
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
    public async Task InspectOptionalExternalClientItemSkillOptionImage_ReadsSkillOptionScalars()
    {
        var itemPath = ExternalClientSmokeData.FindFirstFile("Item", "Item_000.wz");
        if (itemPath is null)
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
    public async Task CanvasOptionalExternalClientMsPackImage_ResolvesOutlinkPreview()
    {
        var msPath = ExternalClientSmokeData.FindFirstFile("Packs", "Mob_00000.ms");
        if (msPath is null)
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
    public async Task InspectOptionalExternalClientEffectImage_ResolvesCanvasOutlinkIdentity()
    {
        var effectPath = ExternalClientSmokeData.FindFirstFile("Effect", "Effect_000.wz");
        if (effectPath is null)
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
    public async Task CanvasOptionalExternalClientEffectImage_ResolvesOutlinkPreview()
    {
        var effectPath = ExternalClientSmokeData.FindFirstFile("Effect", "Effect_000.wz");
        if (effectPath is null)
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
    public async Task InspectOptionalExternalClientCharacterImage_ResolvesCanvasOutlinkIdentity()
    {
        var characterPath = ExternalClientSmokeData.FindFirstFile("Character", "Character_000.wz");
        if (characterPath is null)
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
    public async Task CanvasOptionalExternalClientCharacterImage_ResolvesOutlinkPreview()
    {
        var characterPath = ExternalClientSmokeData.FindFirstFile("Character", "Character_000.wz");
        if (characterPath is null)
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
    public async Task InspectOptionalExternalClientUiImage_ResolvesCanvasOutlinkIdentity()
    {
        var uiPath = ExternalClientSmokeData.FindFirstFile("UI", "UI_000.wz");
        if (uiPath is null)
        {
            return;
        }

        var service = new ResourceInspectionService();
        var inspection = await service.InspectAsync(
            uiPath,
            "Basic.img",
            new ResourceInspectionOptions(
                StringKey: null,
                MaxPropertyDepth: 4,
                IncludeDebugMetadata: true));

        var outlink = Flatten(inspection.Root)
            .FirstOrDefault(node =>
                node is { Name: "_outlink", Kind: "string" } &&
                string.Equals(node.Identity?.LinkedTarget, "UI/_Canvas/Basic.img/Cursor/0/0", StringComparison.Ordinal));

        Assert.NotNull(outlink);
        Assert.NotNull(outlink.Identity?.ResolvedLinkedTarget);
        Assert.EndsWith(
            Path.Combine("UI", "_Canvas", "_Canvas_000.wz"),
            outlink.Identity.ResolvedLinkedTarget.PackagePath,
            StringComparison.Ordinal);
        Assert.Equal("Basic.img", outlink.Identity.ResolvedLinkedTarget.ImageSelector);
        Assert.Equal("Cursor/0/0", outlink.Identity.ResolvedLinkedTarget.ValuePath);
        AssertNoErrorDiagnostics(inspection);
    }

    [Fact]
    public async Task CanvasOptionalExternalClientUiImage_ResolvesOutlinkPreview()
    {
        var uiPath = ExternalClientSmokeData.FindFirstFile("UI", "UI_000.wz");
        if (uiPath is null)
        {
            return;
        }

        var service = new ResourceCanvasImageService();
        var document = await service.LoadAsync(
            uiPath,
            "Basic.img",
            "Cursor/0/0/_outlink",
            new ResourceInspectionOptions(StringKey: null));

        Assert.EndsWith(
            Path.Combine("UI", "_Canvas", "_Canvas_000.wz"),
            document.SourcePath,
            StringComparison.Ordinal);
        Assert.Equal("Basic.img", document.Selector);
        Assert.Equal("Cursor/0/0", document.ValuePath);
        Assert.Equal(24, document.Width);
        Assert.Equal(28, document.Height);
        Assert.Equal(1, document.Format);
        Assert.NotEmpty(document.Pixels);
    }

    [Fact]
    public async Task CanvasOptionalExternalClientSkillImage_PreviewsBc7Canvas()
    {
        var skillCanvasPath = ExternalClientSmokeData.FindFirstFile("Skill", "_Canvas", "_Canvas_097.wz");
        if (skillCanvasPath is null)
        {
            return;
        }

        var service = new ResourceCanvasImageService();
        var document = await service.LoadAsync(
            skillCanvasPath,
            "6414.img",
            "skill/64141504/effect/1",
            new ResourceInspectionOptions(StringKey: null));

        Assert.Equal(skillCanvasPath, document.SourcePath);
        Assert.Equal("6414.img", document.Selector);
        Assert.Equal("skill/64141504/effect/1", document.ValuePath);
        Assert.Equal(4098, document.Format);
        Assert.Equal(224, document.Width);
        Assert.Equal(224, document.Height);
        Assert.NotEmpty(document.Pixels);
    }

    [Fact]
    public async Task InspectOptionalExternalClientSoundImage_ReadsSoundPayloadMetadata()
    {
        var soundPath = ExternalClientSmokeData.FindFirstFile("Sound", "Sound_000.wz");
        if (soundPath is null)
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
