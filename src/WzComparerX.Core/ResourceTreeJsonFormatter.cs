using System.Text.Json;
using System.Text.Json.Serialization;

namespace WzComparerX.Core;

public sealed class ResourceTreeJsonFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public string Format(ResourceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var projection = new
        {
            document.SourcePath,
            document.Root
        };

        return JsonSerializer.Serialize(projection, SerializerOptions) + Environment.NewLine;
    }
}

