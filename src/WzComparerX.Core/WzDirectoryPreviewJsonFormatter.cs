using System.Text.Json;
using System.Text.Json.Serialization;
using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class WzDirectoryPreviewJsonFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public string Format(WzDirectoryPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return JsonSerializer.Serialize(preview, SerializerOptions) + Environment.NewLine;
    }
}

