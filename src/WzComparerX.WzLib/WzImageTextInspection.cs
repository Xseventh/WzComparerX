namespace WzComparerX.WzLib;

public sealed record WzImageTextInspection(
    string Format,
    int TextLength,
    string Text)
{
    public override string ToString()
    {
        return $"format={Format}, length={TextLength}";
    }
}
