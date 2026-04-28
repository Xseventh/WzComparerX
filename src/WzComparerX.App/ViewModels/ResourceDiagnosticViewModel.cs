using WzComparerX.Core;

namespace WzComparerX.App.ViewModels;

public sealed record ResourceDiagnosticViewModel(
    string Severity,
    string Message,
    string? Code,
    string? Path)
{
    public string Title => Code is null ? Severity : $"{Severity} [{Code}]";

    public static ResourceDiagnosticViewModel FromDiagnostic(ResourceInspectionDiagnostic diagnostic)
    {
        return new ResourceDiagnosticViewModel(
            diagnostic.Severity,
            diagnostic.Message,
            diagnostic.Code,
            diagnostic.Path);
    }
}
