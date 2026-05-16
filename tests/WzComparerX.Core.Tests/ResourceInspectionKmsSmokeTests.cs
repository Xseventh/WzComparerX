using WzComparerX.Core;

namespace WzComparerX.Core.Tests;

public class ResourceInspectionKmsSmokeTests
{
    [Fact]
    public async Task InspectOptionalKmsModernPkg2ItemPackage_ReadsDirectoryTable()
    {
        var dataDirectory = GetKmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var itemPath = FindPackage(dataDirectory, "Item_000.wz");
        if (itemPath is null)
        {
            return;
        }

        var service = new ResourceInspectionService();
        var inspection = await service.InspectAsync(
            itemPath,
            selector: null,
            new ResourceInspectionOptions(StringKey: null, IncludeDebugMetadata: true));

        Assert.Equal("pkg2", inspection.Format);
        Assert.Equal("package", inspection.Root.Kind);
        Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "formatProfile" && Equals(item.Value, "pkg2_modern_kms"));
        Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "pkg2HeaderVariant" && Equals(item.Value, "modern"));
        Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "hashVersion" && Equals(item.Value, 0xB0DA16F2u));
        Assert.DoesNotContain(inspection.DebugMetadata ?? [], item => item.Name == "wzVersion");
        Assert.Contains(inspection.Root.Children, child => child is { Name: "ItemOption.img", Kind: "image" });
        Assert.Contains(inspection.Root.Children, child => child is { Name: "ItemSellPriceStandard.img", Kind: "image" });
        AssertNoErrorDiagnostics(inspection);
    }

    [Fact]
    public async Task InspectOptionalKmsModernPkg2ItemImage_ExtractsImagePayload()
    {
        var dataDirectory = GetKmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var itemPath = FindPackage(dataDirectory, "Item_000.wz");
        if (itemPath is null)
        {
            return;
        }

        var service = new ResourceInspectionService();
        var inspection = await service.InspectAsync(
            itemPath,
            "ItemSellPriceStandard.img",
            new ResourceInspectionOptions(
                StringKey: null,
                MaxPropertyDepth: 1,
                IncludeDebugMetadata: true));

        Assert.Equal("pkg2", inspection.Format);
        Assert.Equal("image", inspection.Root.Kind);
        Assert.Equal("Property", inspection.Root.DisplayValue);
        Assert.Equal("ItemSellPriceStandard.img", inspection.Root.Identity?.ImageSelector);
        Assert.Contains(inspection.Root.Children, child => child.Kind == "object");
        AssertNoErrorDiagnostics(inspection);
    }

    [Fact]
    public async Task InspectOptionalKmsModernPkg2StringPackage_ReadsDirectoryTable()
    {
        var dataDirectory = GetKmsDataDirectory();
        if (dataDirectory is null)
        {
            return;
        }

        var stringPath = FindPackage(dataDirectory, "String_000.wz");
        if (stringPath is null)
        {
            return;
        }

        var service = new ResourceInspectionService();
        var inspection = await service.InspectAsync(
            stringPath,
            selector: null,
            new ResourceInspectionOptions(StringKey: null, IncludeDebugMetadata: true));

        Assert.Equal("pkg2", inspection.Format);
        Assert.Equal("package", inspection.Root.Kind);
        Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "formatProfile" && Equals(item.Value, "pkg2_modern_kms"));
        Assert.Contains(inspection.DebugMetadata ?? [], item => item.Name == "pkg2HeaderVariant" && Equals(item.Value, "modern"));
        Assert.Contains(inspection.Root.Children, child => child is { Name: "Eqp.img", Kind: "image" });
        Assert.Contains(inspection.Root.Children, child => child is { Name: "UI.img", Kind: "image" });
        AssertNoErrorDiagnostics(inspection);
    }

    private static string? GetKmsDataDirectory()
    {
        var path = Environment.GetEnvironmentVariable("WCX_KMS_DATA_DIR");
        return string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)
            ? null
            : path;
    }

    private static string? FindPackage(string dataDirectory, string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(dataDirectory, fileName),
            Path.Combine(dataDirectory, "Data", fileName),
            Path.Combine(dataDirectory, Path.GetFileNameWithoutExtension(fileName), fileName)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
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
