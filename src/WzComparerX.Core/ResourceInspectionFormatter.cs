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

        foreach (var child in node.Children)
        {
            AppendNode(builder, child, depth + 1);
        }
    }
}
