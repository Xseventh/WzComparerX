namespace WzComparerX.WzLib;

public sealed record WzImagePropertyInspectionEntry(
    int Index,
    string? Name,
    byte Type,
    string Kind,
    object? Value = null,
    int Depth = 0,
    string? Path = null,
    int? ChildCount = null,
    IReadOnlyList<WzImagePropertyInspectionEntry>? Children = null);
