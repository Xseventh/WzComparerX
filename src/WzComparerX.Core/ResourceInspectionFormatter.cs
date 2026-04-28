using System.Text;

namespace WzComparerX.Core;

public sealed class ResourceInspectionFormatter
{
    public string Format(ResourceInspectionDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var builder = new StringBuilder();
        builder.AppendLine($"source: {document.SourcePath}");
        builder.AppendLine($"format: {document.Format}");
        AppendMetadata(builder, document.DebugMetadata, depth: 0);
        AppendDiagnostics(builder, document.Diagnostics, depth: 0);
        AppendNode(builder, document.Root, depth: 0);
        return builder.ToString();
    }

    private static void AppendNode(StringBuilder builder, ResourceInspectionNode node, int depth)
    {
        builder.Append(' ', depth * 2);
        builder.Append(node.Name);
        builder.Append(" [");
        builder.Append(node.Kind);
        builder.Append(']');
        if (!string.IsNullOrWhiteSpace(node.DisplayValue))
        {
            builder.Append(" : ");
            builder.Append(node.DisplayValue);
        }

        builder.AppendLine();
        AppendMetadata(builder, node.DebugMetadata, depth + 1);
        AppendDiagnostics(builder, node.Diagnostics, depth + 1);

        foreach (var child in node.Children)
        {
            AppendNode(builder, child, depth + 1);
        }
    }

    private static void AppendMetadata(
        StringBuilder builder,
        IReadOnlyList<ResourceInspectionMetadata>? metadata,
        int depth)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return;
        }

        builder.Append(' ', depth * 2);
        builder.AppendLine("debug:");
        foreach (var item in metadata)
        {
            builder.Append(' ', (depth + 1) * 2);
            builder.Append(item.Name);
            builder.Append(": ");
            builder.AppendLine(FormatMetadataValue(item.Value));
        }
    }

    private static void AppendDiagnostics(
        StringBuilder builder,
        IReadOnlyList<ResourceInspectionDiagnostic>? diagnostics,
        int depth)
    {
        if (diagnostics is null || diagnostics.Count == 0)
        {
            return;
        }

        builder.Append(' ', depth * 2);
        builder.AppendLine("diagnostics:");
        foreach (var diagnostic in diagnostics)
        {
            builder.Append(' ', (depth + 1) * 2);
            builder.AppendLine(ResourceInspectionDiagnosticFormatter.Format(diagnostic));
        }
    }

    private static string FormatMetadataValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
