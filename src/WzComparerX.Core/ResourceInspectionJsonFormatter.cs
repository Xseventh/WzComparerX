using System.Text.Json;
using System.Text.Json.Serialization;

namespace WzComparerX.Core;

public sealed class ResourceInspectionJsonFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public string Format(ResourceInspectionDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return JsonSerializer.Serialize(document, SerializerOptions) + Environment.NewLine;
    }
}
