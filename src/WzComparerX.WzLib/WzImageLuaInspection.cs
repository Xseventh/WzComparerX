namespace WzComparerX.WzLib;

public sealed record WzImageLuaInspection(
    int ScriptLength,
    string Snippet)
{
    public override string ToString()
    {
        return $"length={ScriptLength}, snippet=\"{Snippet}\"";
    }
}
