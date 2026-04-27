using System.Text.Json;
using System.Text.Json.Serialization;
using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class WzPackageHeaderScanJsonFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public string Format(IReadOnlyList<WzPackageHeader> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        var projection = new
        {
            Files = headers.Count,
            Headers = headers
        };

        return JsonSerializer.Serialize(projection, SerializerOptions) + Environment.NewLine;
    }
}

