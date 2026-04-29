namespace WzComparerX.Core;

public sealed record ResourceCanvasImageDocument(
    string SourcePath,
    string Selector,
    string? ValuePath,
    int Width,
    int Height,
    int Format,
    string PixelFormat,
    byte[] Pixels,
    IReadOnlyList<ResourceInspectionDiagnostic>? Diagnostics = null)
{
    public int Stride => Width * 4;
}
