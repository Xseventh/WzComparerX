using WzComparerX.App.Services;
using WzComparerX.App.ViewModels;
using WzComparerX.Core;

namespace WzComparerX.App.Tests;

public class ResourceImageContentWorkflowTests
{
    [Fact]
    public void ResolveSelectedTarget_UsesMergedImagePackageTarget()
    {
        var packagePath = Path.Combine(Path.GetTempPath(), "Map1_000.wz");
        var node = ResourceInspectionNodeViewModel.FromNode(
            new ResourceInspectionNode(
                "100000000.img",
                "image",
                Path: $"{packagePath}/100000000.img"));
        var workflow = new ResourceImageContentWorkflow(new ResourceInspectionService());

        var target = workflow.ResolveSelectedTarget(
            Path.Combine(Path.GetTempPath(), "Map1.wz"),
            node);

        Assert.NotNull(target);
        Assert.Equal(packagePath, target.PackagePath);
        Assert.Equal("100000000.img", target.Selector);
    }

    [Fact]
    public void IsCurrentTarget_NormalizesEquivalentPackagePaths()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-image-workflow-");
        var packagePath = Path.Combine(directory.FullName, "Canvas.wz");
        var workflow = new ResourceImageContentWorkflow(new ResourceInspectionService());

        try
        {
            var current = new ResourceImageSelectorTarget(packagePath, "Canvas.img");
            var target = new ResourceImageSelectorTarget(
                Path.Combine(directory.FullName, ".", "Canvas.wz"),
                "Canvas.img");

            Assert.True(workflow.IsCurrentTarget(current, target));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void CanLoadManualImage_RejectsFoldersAndSyntheticDocuments()
    {
        var directory = Directory.CreateTempSubdirectory("wcx-image-workflow-");
        var workflow = new ResourceImageContentWorkflow(new ResourceInspectionService());

        try
        {
            Assert.False(workflow.CanLoadManualImage(directory.FullName, "pkg1", isBusy: false, "Canvas.img"));
            Assert.False(workflow.CanLoadManualImage("fixtures/synthetic/basic-tree.json", "synthetic", isBusy: false, "Canvas.img"));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
