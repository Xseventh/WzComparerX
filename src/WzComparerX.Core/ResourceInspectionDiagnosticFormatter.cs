namespace WzComparerX.Core;

public static class ResourceInspectionDiagnosticFormatter
{
    public static string Format(ResourceInspectionDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        var text = diagnostic.Severity;
        if (!string.IsNullOrWhiteSpace(diagnostic.Code))
        {
            text += $" [{diagnostic.Code}]";
        }

        text += $": {diagnostic.Message}";
        if (!string.IsNullOrWhiteSpace(diagnostic.Path))
        {
            text += $" ({diagnostic.Path})";
        }

        return text;
    }
}
