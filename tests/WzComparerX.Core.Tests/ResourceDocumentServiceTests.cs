using System.Text.Json;
using WzComparerX.Core;

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
}
