using WzComparerX.WzLib;

namespace WzComparerX.Core;

public static class WzStringKeyAutoDetector
{
    private static readonly WzStringEncryptionKind[] CandidateKeys =
    [
        WzStringEncryptionKind.None,
        WzStringEncryptionKind.Kms,
        WzStringEncryptionKind.Gms
    ];

    public static async Task<WzDirectoryInspection> ReadDirectoryAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        WzDirectoryInspection? bestInspection = null;
        var bestScore = int.MinValue;
        foreach (var candidate in CandidateKeys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WzDirectoryInspection inspection;
            try
            {
                var reader = new WzDirectoryInspectionReader(stringDecryptor: new WzStringDecryptor(candidate));
                inspection = await reader.ReadAsync(path, cancellationToken);
            }
            catch (InvalidDataException)
            {
                continue;
            }
            catch (EndOfStreamException)
            {
                continue;
            }
            catch (NotSupportedException)
            {
                continue;
            }

            var score = Score(inspection);
            if (score > bestScore)
            {
                bestScore = score;
                bestInspection = inspection;
            }
        }

        if (bestInspection is null)
        {
            throw new InvalidDataException("Unable to inspect directory with any known PKG1 string key.");
        }

        return bestInspection;
    }

    private static int Score(WzDirectoryInspection inspection)
    {
        if (!inspection.Header.IsValid)
        {
            return int.MinValue / 2;
        }

        var score = 0;
        if (inspection.WzVersion is not null && inspection.HashVersion is not null)
        {
            score += 1000;
        }

        foreach (var entry in inspection.Entries)
        {
            if (entry.Offset is not null)
            {
                score += 5;
            }

            score += ScoreName(entry.Name, entry.Kind);
            score += ScoreName(entry.Path, entry.Kind) / 2;
        }

        return score;
    }

    private static int ScoreName(string? name, WzDirectoryEntryKind kind)
    {
        if (string.IsNullOrEmpty(name))
        {
            return -50;
        }

        var score = 0;
        foreach (var character in name)
        {
            if (character < 0x20 || character == 0x7f)
            {
                score -= 100;
                continue;
            }

            if (character > 0x7e)
            {
                score -= 10;
                continue;
            }

            if (char.IsAsciiLetterOrDigit(character))
            {
                score += 3;
                continue;
            }

            score += character is '.' or '_' or '-' or '/' or '#' or ' ' ? 2 : -5;
        }

        if (kind == WzDirectoryEntryKind.Image && name.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
        {
            score += 40;
        }

        if (kind == WzDirectoryEntryKind.Directory && !name.Contains('.'))
        {
            score += 10;
        }

        return score;
    }
}
