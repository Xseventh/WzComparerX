namespace WzComparerX.App.ViewModels;

public sealed record ResourceActivityLogItemViewModel(string Kind, string Message)
{
    public string Title => $"{Kind}: {Message}";
}
