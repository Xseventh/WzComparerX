namespace WzComparerX.WzLib;

public sealed record WzImageLuaInspection(
    int ScriptLength,
    string Snippet,
    string Script)
{
    public override string ToString()
    {
        return $"length={ScriptLength}, snippet=\"{Snippet}\"";
    }
}
