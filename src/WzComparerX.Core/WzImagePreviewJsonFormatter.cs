using System.Text.Json;
using System.Text.Json.Serialization;
using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class WzImagePreviewJsonFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public string Format(WzImagePreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return JsonSerializer.Serialize(preview, SerializerOptions) + Environment.NewLine;
    }
}
