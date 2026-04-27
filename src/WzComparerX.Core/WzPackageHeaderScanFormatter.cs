using System.Text;
using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class WzPackageHeaderScanFormatter
{
    public string Format(IReadOnlyList<WzPackageHeader> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        var builder = new StringBuilder();
        builder.AppendLine($"files: {headers.Count}");

        foreach (var header in headers)
        {
            builder.Append(header.IsValid ? "ok" : "invalid");
            builder.Append(" | ");
            builder.Append(header.Format.ToString().ToLowerInvariant());
            builder.Append(" | ");
            builder.Append(header.DataSize);
            builder.Append(" | ");
            builder.Append(header.SourcePath);
            builder.AppendLine();
        }

        return builder.ToString();
    }
}

