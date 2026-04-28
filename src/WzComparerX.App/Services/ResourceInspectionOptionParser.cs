using WzComparerX.Core;
using WzComparerX.WzLib;

namespace WzComparerX.App.Services;

public static class ResourceInspectionOptionParser
{
    public static bool TryParse(
        string? keyText,
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

        options = new ResourceInspectionOptions(
            stringKey,
            WzImageInspectionReader.FullPropertyInspectionDepth,
            IncludeDebugMetadata: true);
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

}
