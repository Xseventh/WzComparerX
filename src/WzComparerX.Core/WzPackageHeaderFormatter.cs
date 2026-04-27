using System.Text;
using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class WzPackageHeaderFormatter
{
    public string Format(WzPackageHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);

        var builder = new StringBuilder();
        builder.AppendLine($"source: {header.SourcePath}");
        builder.AppendLine($"format: {header.Format.ToString().ToLowerInvariant()}");
        builder.AppendLine($"signature: {header.Signature}");
        builder.AppendLine($"valid: {header.IsValid.ToString().ToLowerInvariant()}");

        if (!header.IsValid)
        {
            return builder.ToString();
        }

        builder.AppendLine($"headerSize: {header.HeaderSize}");
        builder.AppendLine($"dataSize: {header.DataSize}");
        builder.AppendLine($"fileSize: {header.FileSize}");
        builder.AppendLine($"directoryStartPosition: {header.DirectoryStartPosition}");
        builder.AppendLine($"copyright: {header.Copyright}");

        if (header.Format == WzPackageFormat.Pkg1)
        {
            builder.AppendLine($"encryptedVersion: {header.EncryptedVersion}");
            builder.AppendLine($"encryptedVersionMissing: {header.IsEncryptedVersionMissing.ToString().ToLowerInvariant()}");
        }
        else if (header.Format == WzPackageFormat.Pkg2)
        {
            builder.AppendLine($"hash1: {header.Hash1}");
            builder.AppendLine($"hash2: {header.Hash2}");
        }

        return builder.ToString();
    }
}

