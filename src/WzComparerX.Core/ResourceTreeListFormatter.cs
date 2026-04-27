using System.Text;
using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class ResourceTreeListFormatter
{
    public string Format(ResourceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var builder = new StringBuilder();
        AppendNode(builder, document.Root, depth: 0);
        return builder.ToString();
    }

    private static void AppendNode(StringBuilder builder, RawResourceNode node, int depth)
    {
        builder.Append(' ', depth * 2);
        builder.Append(node.Name);
        builder.Append(" [");
        builder.Append(node.Kind.ToString().ToLowerInvariant());
        builder.Append(']');

        if (node.Kind == RawResourceNodeKind.Value)
        {
            if (!string.IsNullOrWhiteSpace(node.ValueKind))
            {
                builder.Append(" : ");
                builder.Append(node.ValueKind);
            }

            if (!string.IsNullOrWhiteSpace(node.Value))
            {
                builder.Append(" = ");
                builder.Append(node.Value);
            }
        }

        builder.AppendLine();

        foreach (var child in node.Children)
        {
            AppendNode(builder, child, depth + 1);
        }
    }
}

