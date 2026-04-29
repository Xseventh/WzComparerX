namespace WzComparerX.App.Services;

public static class ResourceImageSelector
{
    public static ResourceImageSelectorTarget? Resolve(string currentPackagePath, string? selector)
    {
        var trimmed = selector?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (TrySplitEmbeddedPackageSelector(trimmed, out var packagePath, out var imageSelector))
        {
            return new ResourceImageSelectorTarget(packagePath, imageSelector);
        }

        var normalized = Normalize(trimmed, Path.GetFileName(currentPackagePath));
        return normalized is null
            ? null
            : new ResourceImageSelectorTarget(currentPackagePath.Trim(), normalized);
    }

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

    private static bool TrySplitEmbeddedPackageSelector(
        string value,
        out string packagePath,
        out string imageSelector)
    {
        var markerIndex = -1;
        for (var i = 0; i <= value.Length - 4; i++)
        {
            if (!value.AsSpan(i, 3).Equals(".wz", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var separatorIndex = i + 3;
            if (separatorIndex < value.Length &&
                (value[separatorIndex] == '/' || value[separatorIndex] == '\\'))
            {
                markerIndex = i;
            }
        }

        if (markerIndex < 0)
        {
            packagePath = string.Empty;
            imageSelector = string.Empty;
            return false;
        }

        var packageEnd = markerIndex + 3;
        packagePath = value[..packageEnd];
        imageSelector = value[(packageEnd + 1)..].TrimStart('/', '\\');
        if (packagePath.IndexOfAny(['/', '\\']) < 0)
        {
            return false;
        }

        return imageSelector.Length > 0;
    }
}

public sealed record ResourceImageSelectorTarget(string PackagePath, string Selector);
