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
