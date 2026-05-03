using System.Buffers.Binary;

namespace WzComparerX.WzLib;

public sealed class WzListFileReader
{
    private static readonly WzStringEncryptionKind[] AutoDetectCandidates =
    [
        WzStringEncryptionKind.Gms,
        WzStringEncryptionKind.Kms,
        WzStringEncryptionKind.None
    ];

    public async Task<WzListFileInspection> ReadAsync(
        string path,
        WzStringEncryptionKind? stringKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = File.OpenRead(path);
        return await ReadAsync(stream, Path.GetFullPath(path), stringKey, cancellationToken);
    }

    public async Task<WzListFileInspection> ReadAsync(
        Stream stream,
        string sourcePath,
        WzStringEncryptionKind? stringKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        return Read(memory.ToArray(), sourcePath, stringKey);
    }

    public WzListFileInspection Read(
        Stream stream,
        string sourcePath,
        WzStringEncryptionKind? stringKey = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return Read(memory.ToArray(), sourcePath, stringKey);
    }

    public WzListFileInspection Read(
        ReadOnlySpan<byte> bytes,
        string sourcePath,
        WzStringEncryptionKind? stringKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        var selectedStringKey = stringKey ?? DetectStringKey(bytes);
        var result = DecodeEntries(bytes, selectedStringKey);
        return new WzListFileInspection(
            Path.GetFullPath(sourcePath),
            selectedStringKey,
            result.RawEntryCount,
            result.Entries);
    }

    private static WzStringEncryptionKind DetectStringKey(ReadOnlySpan<byte> bytes)
    {
        foreach (var candidate in AutoDetectCandidates)
        {
            if (TryDecodeFirstCharacter(bytes, candidate, out var character) && character == 'd')
            {
                return candidate;
            }
        }

        WzStringEncryptionKind? bestCandidate = null;
        var bestScore = int.MinValue;
        foreach (var candidate in AutoDetectCandidates)
        {
            try
            {
                var result = DecodeEntries(bytes, candidate);
                var score = Score(result.Entries);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                }
            }
            catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException or OverflowException)
            {
            }
        }

        return bestCandidate ?? throw new InvalidDataException("Unable to decode List.wz with any known string key.");
    }

    private static bool TryDecodeFirstCharacter(
        ReadOnlySpan<byte> bytes,
        WzStringEncryptionKind stringKey,
        out char character)
    {
        character = '\0';
        if (bytes.Length < sizeof(int))
        {
            return false;
        }

        var characterCount = BinaryPrimitives.ReadInt32LittleEndian(bytes[..sizeof(int)]);
        if (characterCount <= 0 || bytes.Length < sizeof(int) + 2)
        {
            return false;
        }

        Span<byte> firstCharacter = stackalloc byte[2];
        bytes.Slice(sizeof(int), 2).CopyTo(firstCharacter);
        var decrypted = new WzStringDecryptor(stringKey).DecryptPayload(firstCharacter);
        character = (char)decrypted[0];
        return true;
    }

    private static (int RawEntryCount, IReadOnlyList<WzListFileEntryInspection> Entries) DecodeEntries(
        ReadOnlySpan<byte> bytes,
        WzStringEncryptionKind stringKey)
    {
        var entries = new List<WzListFileEntryInspection>();
        var offset = 0;
        var rawIndex = 0;
        while (offset < bytes.Length)
        {
            if (bytes.Length - offset < sizeof(int))
            {
                throw new EndOfStreamException("List.wz entry length is truncated.");
            }

            var lengthPosition = offset;
            var characterCount = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(offset, sizeof(int)));
            offset += sizeof(int);
            if (characterCount < 0)
            {
                throw new InvalidDataException($"List.wz entry character count cannot be negative: {characterCount}.");
            }

            int dataLength;
            try
            {
                dataLength = checked(characterCount * sizeof(char));
            }
            catch (OverflowException ex)
            {
                throw new InvalidDataException($"List.wz entry character count is too large: {characterCount}.", ex);
            }

            if (bytes.Length - offset < dataLength)
            {
                throw new EndOfStreamException("List.wz entry text is truncated.");
            }

            var dataPosition = offset;
            var path = DecodeEntryPath(bytes.Slice(offset, dataLength), characterCount, stringKey);
            offset += dataLength;

            if (bytes.Length - offset < sizeof(short))
            {
                throw new EndOfStreamException("List.wz entry terminator is truncated.");
            }

            var terminatorPosition = offset;
            offset += sizeof(short);

            if (!string.Equals(path, "dummy", StringComparison.Ordinal))
            {
                entries.Add(new WzListFileEntryInspection(
                    rawIndex,
                    path,
                    characterCount,
                    lengthPosition,
                    dataPosition,
                    terminatorPosition));
            }

            rawIndex++;
        }

        return (rawIndex, entries);
    }

    private static string DecodeEntryPath(
        ReadOnlySpan<byte> bytes,
        int characterCount,
        WzStringEncryptionKind stringKey)
    {
        var decrypted = new WzStringDecryptor(stringKey).DecryptPayload(bytes);
        var chars = new char[characterCount];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)decrypted[i * sizeof(char)];
        }

        return new string(chars);
    }

    private static int Score(IReadOnlyList<WzListFileEntryInspection> entries)
    {
        var score = 0;
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Path))
            {
                score -= 100;
                continue;
            }

            foreach (var character in entry.Path)
            {
                if (character < 0x20 || character == 0x7f)
                {
                    score -= 100;
                    continue;
                }

                if (character > 0x7e)
                {
                    score -= 20;
                    continue;
                }

                if (char.IsAsciiLetterOrDigit(character))
                {
                    score += 3;
                    continue;
                }

                score += character is '/' or '\\' or '.' or '_' or '-' or '#' or ' ' ? 2 : -5;
            }

            if (entry.Path.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
            {
                score += 40;
            }

            if (entry.Path.EndsWith(".wz", StringComparison.OrdinalIgnoreCase))
            {
                score += 20;
            }
        }

        return score;
    }
}
