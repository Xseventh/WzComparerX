using System.Globalization;
using WzComparerX.Core;
using WzComparerX.WzLib;

namespace WzComparerX.App.Services;

public static class ResourceInspectionOptionParser
{
    public static bool TryParse(
        string? keyText,
        string? depthText,
        out ResourceInspectionOptions options,
        out string errorMessage)
    {
        options = new ResourceInspectionOptions(IncludeDebugMetadata: true);
        errorMessage = string.Empty;

        if (!TryParseStringKey(keyText, out var stringKey))
        {
            errorMessage = $"Unknown string key: {keyText}";
            return false;
        }

        if (!TryParseDepth(depthText, out var depth))
        {
            errorMessage = $"Depth must be between 0 and {WzImageInspectionReader.MaxPropertyInspectionDepth}.";
            return false;
        }

        options = new ResourceInspectionOptions(stringKey, depth, IncludeDebugMetadata: true);
        return true;
    }

    public static bool TryParseStringKey(string? value, out WzStringEncryptionKind? kind)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) ||
            string.Equals(trimmed, "auto", StringComparison.OrdinalIgnoreCase))
        {
            kind = null;
            return true;
        }

        if (string.Equals(trimmed, "none", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "noop", StringComparison.OrdinalIgnoreCase))
        {
            kind = WzStringEncryptionKind.None;
            return true;
        }

        if (string.Equals(trimmed, "kms", StringComparison.OrdinalIgnoreCase))
        {
            kind = WzStringEncryptionKind.Kms;
            return true;
        }

        if (string.Equals(trimmed, "gms", StringComparison.OrdinalIgnoreCase))
        {
            kind = WzStringEncryptionKind.Gms;
            return true;
        }

        kind = WzStringEncryptionKind.None;
        return false;
    }

    public static bool TryParseDepth(string? value, out int depth)
    {
        return int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out depth) &&
            depth >= 0 &&
            depth <= WzImageInspectionReader.MaxPropertyInspectionDepth;
    }
}
