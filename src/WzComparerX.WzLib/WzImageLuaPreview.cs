namespace WzComparerX.WzLib;

public sealed record WzImageLuaPreview(
    int ScriptLength,
    string Preview)
{
    public override string ToString()
    {
        return $"length={ScriptLength}, preview=\"{Preview}\"";
    }
}
