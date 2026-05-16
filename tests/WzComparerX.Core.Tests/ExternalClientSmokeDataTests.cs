using WzComparerX.Tests;

namespace WzComparerX.Core.Tests;

public class ExternalClientSmokeDataTests
{
    [Fact]
    public void ParseDataDirectory_ReturnsFullPathForExistingDirectory()
    {
        using var directory = TemporaryDirectory.Create();

        var parsed = ExternalClientSmokeData.ParseDataDirectory(directory.Path);

        Assert.Equal(Path.GetFullPath(directory.Path), parsed);
    }

    [Fact]
    public void ParseDataDirectory_IgnoresMissingDirectory()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"wcx-missing-{Guid.NewGuid():N}");

        var parsed = ExternalClientSmokeData.ParseDataDirectory(missing);

        Assert.Null(parsed);
    }

    [Fact]
    public void ParseDataDirectory_IgnoresEmptyValue()
    {
        var parsed = ExternalClientSmokeData.ParseDataDirectory("  ");

        Assert.Null(parsed);
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
