using System.Text;
using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class WzDirectoryPreviewFormatter
{
    public string Format(WzDirectoryPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);

        var builder = new StringBuilder();
        builder.AppendLine($"source: {preview.Header.SourcePath}");
        builder.AppendLine($"format: {preview.Header.Format.ToString().ToLowerInvariant()}");
        if (preview.WzVersion is not null && preview.HashVersion is not null)
        {
            builder.AppendLine($"wzVersion: {preview.WzVersion}");
            builder.AppendLine($"hashVersion: {preview.HashVersion}");
        }

        builder.AppendLine($"entries: {preview.EntryCount}");

        foreach (var entry in preview.Entries)
        {
            builder.Append(entry.Index);
            builder.Append(" | ");
            builder.Append(entry.Kind.ToString().ToLowerInvariant());
            if (!string.IsNullOrEmpty(entry.Name))
            {
                builder.Append(" | name=");
                builder.Append(entry.Name);
            }

            builder.Append(" | type=0x");
            builder.Append(entry.NodeType.ToString("X2"));
            builder.Append(" | size=");
            builder.Append(entry.DataSize);
            builder.Append(" | checksum=");
            builder.Append(entry.Checksum);
            builder.Append(" | hashOffset=");
            builder.Append(entry.HashOffset);
            if (entry.Offset is not null)
            {
                builder.Append(" | offset=");
                builder.Append(entry.Offset);
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }
}
