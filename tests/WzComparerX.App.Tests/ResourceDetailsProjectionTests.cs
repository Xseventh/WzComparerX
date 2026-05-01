using WzComparerX.App.ViewModels;
using WzComparerX.Core;

namespace WzComparerX.App.Tests;

public class ResourceDetailsProjectionTests
{
    [Fact]
    public void CreateDocumentMetadata_IncludesSourceFormatAndDebugMetadata()
    {
        var document = new ResourceInspectionDocument(
            SourcePath: "Base.wz",
            Format: "pkg1",
            Root: new ResourceInspectionNode("Base.wz", "package"),
            DebugMetadata:
            [
                new ResourceInspectionMetadata("wzVersion", 264),
                new ResourceInspectionMetadata("stringKey", "none"),
            ]);

        var metadata = ResourceDetailsProjection.CreateDocumentMetadata(document);

        Assert.Collection(
            metadata,
            item => Assert.Equal(new ResourceMetadataItemViewModel("source", "Base.wz"), item),
            item => Assert.Equal(new ResourceMetadataItemViewModel("format", "pkg1"), item),
            item => Assert.Equal(new ResourceMetadataItemViewModel("wzVersion", "264"), item),
            item => Assert.Equal(new ResourceMetadataItemViewModel("stringKey", "none"), item));
    }

    [Fact]
    public void CreateSelectionMetadata_IncludesNodeIdentityAndOptionalValues()
    {
        var node = ResourceInspectionNodeViewModel.FromNode(
            new ResourceInspectionNode(
                Name: "Canvas.img",
                Kind: "image",
                Path: "Map1_000.wz/Canvas.img",
                DisplayValue: "Property",
                DebugMetadata:
                [
                    new ResourceInspectionMetadata("selector", "Canvas.img"),
                ]));

        var metadata = ResourceDetailsProjection.CreateSelectionMetadata(node);

        Assert.Collection(
            metadata,
            item => Assert.Equal(new ResourceMetadataItemViewModel("name", "Canvas.img"), item),
            item => Assert.Equal(new ResourceMetadataItemViewModel("kind", "image"), item),
            item => Assert.Equal(new ResourceMetadataItemViewModel("path", "Map1_000.wz/Canvas.img"), item),
            item => Assert.Equal(new ResourceMetadataItemViewModel("value", "Property"), item),
            item => Assert.Equal(new ResourceMetadataItemViewModel("selector", "Canvas.img"), item));
    }

    [Fact]
    public void CreateSelectionDiagnostics_ProjectsNodeDiagnostics()
    {
        var node = ResourceInspectionNodeViewModel.FromNode(
            new ResourceInspectionNode(
                Name: "canvas",
                Kind: "canvas",
                Diagnostics:
                [
                    new ResourceInspectionDiagnostic(
                        Severity: "warning",
                        Message: "Unsupported Canvas format.",
                        Path: "Canvas.img/canvas",
                        Code: "wz.canvas.unsupportedFormat"),
                ]));

        var diagnostic = Assert.Single(ResourceDetailsProjection.CreateSelectionDiagnostics(node));

        Assert.Equal("warning", diagnostic.Severity);
        Assert.Equal("Unsupported Canvas format.", diagnostic.Message);
        Assert.Equal("Canvas.img/canvas", diagnostic.Path);
        Assert.Equal("wz.canvas.unsupportedFormat", diagnostic.Code);
    }
}
