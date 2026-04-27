namespace WzComparerX.Core;

public sealed record ResourceExportDocument(
    string SourcePath,
    ResourceExportKind Kind,
    string ContentType,
    byte[] Content,
    IReadOnlyList<ResourceInspectionDiagnostic>? Diagnostics = null)
{
    public bool IsText =>
        ContentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(ContentType, "application/json", StringComparison.OrdinalIgnoreCase);

    public string GetTextContent()
    {
        if (!IsText)
        {
            throw new InvalidOperationException("Binary export content cannot be written to a text output.");
        }

        return System.Text.Encoding.UTF8.GetString(Content);
    }
}
