using System.Text.Json;
using System.Text.Json.Serialization;

namespace WzComparerX.WzLib;

public sealed class SyntheticResourceDocumentReader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public async Task<RawResourceDocument> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = File.OpenRead(path);
        var fixture = await JsonSerializer.DeserializeAsync<SyntheticRawNode>(
            stream,
            SerializerOptions,
            cancellationToken);

        if (fixture is null)
        {
            throw new InvalidDataException($"Synthetic resource fixture '{path}' is empty.");
        }

        return new RawResourceDocument(Path.GetFullPath(path), ToRawNode(fixture, path));
    }

    private static RawResourceNode ToRawNode(SyntheticRawNode fixture, string path)
    {
        if (string.IsNullOrWhiteSpace(fixture.Name))
        {
            throw new InvalidDataException($"Synthetic resource fixture '{path}' contains a node without a name.");
        }

        var children = fixture.Children
            .Select(child => ToRawNode(child, path))
            .ToArray();

        return new RawResourceNode(
            fixture.Name,
            fixture.Kind,
            children,
            fixture.ValueKind,
            fixture.Value is null ? null : fixture.Value.Value.GetRawText());
    }

    private sealed record SyntheticRawNode
    {
        public string Name { get; init; } = string.Empty;

        public RawResourceNodeKind Kind { get; init; }

        public IReadOnlyList<SyntheticRawNode> Children { get; init; } = Array.Empty<SyntheticRawNode>();

        public string? ValueKind { get; init; }

        public JsonElement? Value { get; init; }
    }
}
