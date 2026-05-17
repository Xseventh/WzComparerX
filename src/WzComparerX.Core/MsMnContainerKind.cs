namespace WzComparerX.Core;

internal static class MsMnContainerKind
{
    public const string Ms = "ms";
    public const string Mn = "mn";

    public static bool IsPath(string path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".ms", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".mn", StringComparison.OrdinalIgnoreCase);
    }

    public static string FromPath(string path)
    {
        return string.Equals(Path.GetExtension(path), ".mn", StringComparison.OrdinalIgnoreCase)
            ? Mn
            : Ms;
    }
}
