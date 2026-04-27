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

        return builder.ToString();
    }
}
