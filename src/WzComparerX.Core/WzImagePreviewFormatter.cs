using System.Text;
using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class WzImagePreviewFormatter
{
    public string Format(WzImagePreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);

        var builder = new StringBuilder();
        builder.AppendLine($"source: {preview.Header.SourcePath}");
        builder.AppendLine($"format: {preview.Header.Format.ToString().ToLowerInvariant()}");
        builder.AppendLine($"selector: {preview.Selector}");

        if (preview.Entry is not null)
        {
            builder.AppendLine($"entry: {preview.Entry.Path ?? preview.Entry.Name ?? preview.Entry.Index.ToString()}");
            builder.AppendLine($"offset: {preview.Entry.Offset}");
            builder.AppendLine($"size: {preview.Entry.DataSize}");
            builder.AppendLine($"checksum: {preview.Entry.Checksum}");
        }

        if (preview.ObjectType is not null)
        {
            builder.AppendLine($"objectType: {preview.ObjectType}");
        }

        if (preview.ObjectValue is not null)
        {
            builder.AppendLine($"objectValue: {preview.ObjectValue}");
        }

        if (preview.PropertyCount is not null)
        {
            builder.AppendLine($"properties: {preview.PropertyCount}");
        }

        if (preview.Properties is not null)
        {
            foreach (var property in preview.Properties)
            {
                AppendProperty(builder, property);
            }
        }

        return builder.ToString();
    }

    private static void AppendProperty(StringBuilder builder, WzImagePropertyPreviewEntry property)
    {
        if (property.Depth > 0)
        {
            builder.Append(' ', property.Depth * 2);
        }

        builder.Append(property.Index);
        builder.Append(" | ");
        builder.Append(property.Kind);
        if (!string.IsNullOrEmpty(property.Name))
        {
            builder.Append(" | name=");
            builder.Append(property.Name);
        }

        builder.Append(" | type=0x");
        builder.Append(property.Type.ToString("X2"));
        if (property.Value is not null)
        {
            builder.Append(" | value=");
            builder.Append(property.Value);
        }

        if (property.ChildCount is not null)
        {
            builder.Append(" | children=");
            builder.Append(property.ChildCount);
        }

        builder.AppendLine();

        if (property.Children is null)
        {
            return;
        }

        foreach (var child in property.Children)
        {
            AppendProperty(builder, child);
        }
    }
}
