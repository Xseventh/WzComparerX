namespace WzComparerX.WzLib;

public sealed record WzImagePropertyPreviewEntry(
    int Index,
    string? Name,
    byte Type,
    string Kind,
    object? Value = null);
