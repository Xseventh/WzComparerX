namespace WzComparerX.WzLib;

internal sealed record WzImageNestedPropertyInspection(
    int? ChildCount,
    List<WzImagePropertyInspectionEntry>? Children);
