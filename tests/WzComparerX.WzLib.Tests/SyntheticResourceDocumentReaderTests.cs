using WzComparerX.WzLib;

namespace WzComparerX.WzLib.Tests;

public class SyntheticResourceDocumentReaderTests
{
    [Fact]
    public async Task ReadAsync_LoadsSyntheticDirectoryTree()
    {
        var reader = new SyntheticResourceDocumentReader();

        var document = await reader.ReadAsync(FixturePath("basic-tree.json"));

        Assert.Equal("basic-tree", document.Root.Name);
        Assert.Equal(RawResourceNodeKind.Directory, document.Root.Kind);
        Assert.Equal(["Character.wz", "String.wz"], document.Root.Children.Select(child => child.Name));
        Assert.Equal("\"Beginner Cap\"", document.Root.Children[0].Children[0].Children[0].Children[0].Children[0].Value);
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
