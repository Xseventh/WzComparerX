using System.Buffers.Binary;
using System.Text;

namespace WzComparerX.App.Tests;

internal static class AppTestFixtures
{
    public static string FixturePath(string name)
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "fixtures",
            "synthetic",
            name);
    }

    public static string MaterializeHexFixture(string name, string extension)
    {
        var hex = new StringBuilder();
        foreach (var ch in File.ReadAllText(FixturePath(name)))
        {
            if (Uri.IsHexDigit(ch))
            {
                hex.Append(ch);
            }
        }

        var path = Path.Combine(Path.GetTempPath(), $"wcx-app-fixture-{Guid.NewGuid():N}{extension}");
        File.WriteAllBytes(path, Convert.FromHexString(hex.ToString()));
        return path;
    }

    public static byte[] CreatePkg1()
    {
        byte[] encryptedVersion = [0x7b, 0x00];
        byte[] directoryData = [0x00];
        var header = CreateHeader("PKG1", "Copyright", dataSize: encryptedVersion.Length + directoryData.Length);
        return [.. header, .. encryptedVersion, .. directoryData];
    }

    private static byte[] CreateHeader(string signature, string copyright, long dataSize)
    {
        var copyrightBytes = Encoding.ASCII.GetBytes(copyright);
        var headerSize = 4 + sizeof(long) + sizeof(int) + copyrightBytes.Length;
        var bytes = new byte[headerSize];

        Encoding.ASCII.GetBytes(signature, bytes);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(4, sizeof(long)), dataSize);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12, sizeof(int)), headerSize);
        copyrightBytes.CopyTo(bytes.AsSpan(16));
        return bytes;
    }
}
