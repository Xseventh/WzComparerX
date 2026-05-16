using WzComparerX.Tests;

namespace WzComparerX.Core.Tests;

public class ExternalClientSmokeDataTests
{
    [Fact]
    public void ParseDataDirectories_ReturnsLabeledDirectories()
    {
        using var first = TemporaryDirectory.Create();
        using var second = TemporaryDirectory.Create();
        var value = string.Join(
            Path.PathSeparator,
            $"gms={first.Path}",
            $"kms={second.Path}");

        var directories = ExternalClientSmokeData.ParseDataDirectories(value);

        Assert.Collection(
            directories,
            directory =>
            {
                Assert.Equal("gms", directory.Label);
                Assert.Equal(Path.GetFullPath(first.Path), directory.Path);
            },
            directory =>
            {
                Assert.Equal("kms", directory.Label);
                Assert.Equal(Path.GetFullPath(second.Path), directory.Path);
            });
    }

    [Fact]
    public void ParseDataDirectories_UsesClientLabelForUnlabeledDirectory()
    {
        using var directory = TemporaryDirectory.Create();

        var directories = ExternalClientSmokeData.ParseDataDirectories(directory.Path);

        var parsed = Assert.Single(directories);
        Assert.Equal("client", parsed.Label);
        Assert.Equal(Path.GetFullPath(directory.Path), parsed.Path);
    }

    [Fact]
    public void ParseDataDirectories_IgnoresMissingAndDuplicateDirectories()
    {
        using var directory = TemporaryDirectory.Create();
        var missing = Path.Combine(Path.GetTempPath(), $"wcx-missing-{Guid.NewGuid():N}");
        var value = string.Join(
            Path.PathSeparator,
            $"first={directory.Path}",
            $"missing={missing}",
            $"duplicate={directory.Path}");

        var directories = ExternalClientSmokeData.ParseDataDirectories(value);

        var parsed = Assert.Single(directories);
        Assert.Equal("first", parsed.Label);
        Assert.Equal(Path.GetFullPath(directory.Path), parsed.Path);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string path)
        {
            Path = path;
            Directory.CreateDirectory(path);
        }

        public string Path { get; }

        public static TemporaryDirectory Create()
        {
            return new TemporaryDirectory(
                System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"wcx-external-client-smoke-{Guid.NewGuid():N}"));
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
