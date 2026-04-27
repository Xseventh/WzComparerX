using System.Text.Json;
using System.Text.Json.Serialization;

namespace WzComparerX.Core;

public sealed class WzPackageHeaderJsonFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public string Format(WzComparerX.WzLib.WzPackageHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);
        return JsonSerializer.Serialize(header, SerializerOptions) + Environment.NewLine;
    }
}
