namespace WzComparerX.App.Services;

public static class ResourceImageSelector
{
    public static string? Normalize(string? selector, params string?[] rootPrefixes)
    {
        var trimmed = selector?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        foreach (var prefix in rootPrefixes)
        {
            var normalizedPrefix = prefix?.Trim().Trim('/');
            if (string.IsNullOrEmpty(normalizedPrefix))
            {
                continue;
            }

            var rootedPrefix = normalizedPrefix + "/";
            if (trimmed.StartsWith(rootedPrefix, StringComparison.OrdinalIgnoreCase) &&
                trimmed.Length > rootedPrefix.Length)
            {
                return trimmed[rootedPrefix.Length..];
            }
        }

        return trimmed;
    }
}
