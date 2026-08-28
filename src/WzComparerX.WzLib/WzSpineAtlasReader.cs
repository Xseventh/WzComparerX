namespace WzComparerX.WzLib;

public static class WzSpineAtlasReader
{
    private static readonly HashSet<string> PagePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "size",
        "format",
        "filter",
        "repeat",
        "pma",
        "scale"
    };

    public static IReadOnlyList<WzSpineAtlasPage> ReadPages(string atlasText)
    {
        ArgumentNullException.ThrowIfNull(atlasText);

        var pages = new List<WzSpineAtlasPage>();
        var block = new List<string>();
        using var reader = new StringReader(atlasText);
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                AddPage(block, pages);
                block.Clear();
            }
            else
            {
                block.Add(line.Trim());
            }
        }

        AddPage(block, pages);
        return pages;
    }

    private static void AddPage(List<string> block, List<WzSpineAtlasPage> pages)
    {
        if (block.Count == 0)
        {
            return;
        }

        var path = block[0];
        int? width = null;
        int? height = null;
        for (var i = 1; i < block.Count; i++)
        {
            var separator = block[i].IndexOf(':');
            if (separator <= 0)
            {
                break;
            }

            var name = block[i][..separator].Trim();
            if (!PagePropertyNames.Contains(name))
            {
                break;
            }

            if (string.Equals(name, "size", StringComparison.OrdinalIgnoreCase))
            {
                var dimensions = block[i][(separator + 1)..].Split(',', StringSplitOptions.TrimEntries);
                if (dimensions.Length == 2 &&
                    int.TryParse(dimensions[0], out var parsedWidth) &&
                    int.TryParse(dimensions[1], out var parsedHeight) &&
                    parsedWidth > 0 &&
                    parsedHeight > 0)
                {
                    width = parsedWidth;
                    height = parsedHeight;
                }
            }
        }

        pages.Add(new WzSpineAtlasPage(path, width, height));
    }
}
