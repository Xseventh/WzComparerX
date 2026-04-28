using WzComparerX.Core;

namespace WzComparerX.App.ViewModels;

public sealed record ResourceMetadataItemViewModel(string Name, string Value)
{
    public static ResourceMetadataItemViewModel FromMetadata(ResourceInspectionMetadata metadata)
    {
        return new ResourceMetadataItemViewModel(
            metadata.Name,
            Convert.ToString(metadata.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
    }
}
