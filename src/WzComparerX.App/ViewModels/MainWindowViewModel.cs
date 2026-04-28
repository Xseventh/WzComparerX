using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WzComparerX.WzLib;
using WzComparerX.Core;

namespace WzComparerX.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ResourceInspectionService inspectionService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    private string pathText = "fixtures/synthetic/basic-tree.json";

    [ObservableProperty]
    private string selectorText = string.Empty;

    [ObservableProperty]
    private string keyText = "auto";

    [ObservableProperty]
    private string depthText = "2";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(InspectSelectedImageCommand))]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private ResourceInspectionNodeViewModel? selectedNode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InspectSelectedImageCommand))]
    private string currentFormat = string.Empty;

    public MainWindowViewModel()
        : this(new ResourceInspectionService())
    {
    }

    internal MainWindowViewModel(ResourceInspectionService inspectionService)
    {
        this.inspectionService = inspectionService;
    }

    public ObservableCollection<ResourceInspectionNodeViewModel> RootNodes { get; } = [];

    public ObservableCollection<ResourceMetadataItemViewModel> DocumentMetadata { get; } = [];

    public ObservableCollection<ResourceMetadataItemViewModel> SelectedMetadata { get; } = [];

    public ObservableCollection<ResourceDiagnosticViewModel> SelectedDiagnostics { get; } = [];

    public bool HasSelection => SelectedNode is not null;

    public bool HasDiagnostics => SelectedDiagnostics.Count > 0;

    public async Task OpenPathAsync(string path)
    {
        var trimmed = path.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        PathText = trimmed;
        SelectorText = string.Empty;
        await LoadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanLoad))]
    public async Task LoadAsync()
    {
        var path = PathText.Trim();
        if (path.Length == 0)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Loading";
            if (!TryCreateInspectionOptions(out var options))
            {
                return;
            }

            var selector = ResourceImageSelector.Normalize(SelectorText, Path.GetFileName(path));
            if (!string.Equals(selector, NormalizeOptional(SelectorText), StringComparison.Ordinal))
            {
                SelectorText = selector ?? string.Empty;
            }

            var document = await inspectionService.InspectAsync(
                path,
                selector,
                options);

            RootNodes.Clear();
            RootNodes.Add(ResourceInspectionNodeViewModel.FromNode(document.Root));
            SetDocumentMetadata(document);
            SelectedNode = RootNodes[0];
            CurrentFormat = document.Format;
            StatusMessage = $"Loaded {document.Format}: {Path.GetFileName(document.SourcePath)}";
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            RootNodes.Clear();
            DocumentMetadata.Clear();
            SelectedNode = null;
            CurrentFormat = string.Empty;
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanInspectSelectedImage))]
    public async Task InspectSelectedImageAsync()
    {
        if (SelectedNode is null)
        {
            return;
        }

        SelectorText = ResourceImageSelector.Normalize(
            SelectedNode.Path ?? SelectedNode.Name,
            Path.GetFileName(PathText.Trim())) ?? string.Empty;
        await LoadAsync();
    }

    private bool CanLoad()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(PathText);
    }

    private bool CanInspectSelectedImage()
    {
        return !IsBusy &&
            SelectedNode?.Kind == "image" &&
            !string.Equals(CurrentFormat, "synthetic", StringComparison.OrdinalIgnoreCase);
    }

    partial void OnSelectedNodeChanged(ResourceInspectionNodeViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        SetSelectedMetadata(value);
        SetSelectedDiagnostics(value);
        InspectSelectedImageCommand.NotifyCanExecuteChanged();
    }

    private void SetDocumentMetadata(ResourceInspectionDocument document)
    {
        DocumentMetadata.Clear();
        DocumentMetadata.Add(new ResourceMetadataItemViewModel("source", document.SourcePath));
        DocumentMetadata.Add(new ResourceMetadataItemViewModel("format", document.Format));
        foreach (var item in document.DebugMetadata ?? [])
        {
            DocumentMetadata.Add(ResourceMetadataItemViewModel.FromMetadata(item));
        }
    }

    private void SetSelectedMetadata(ResourceInspectionNodeViewModel? node)
    {
        SelectedMetadata.Clear();
        if (node is null)
        {
            return;
        }

        SelectedMetadata.Add(new ResourceMetadataItemViewModel("name", node.Name));
        SelectedMetadata.Add(new ResourceMetadataItemViewModel("kind", node.Kind));
        AddOptional("path", node.Path);
        AddOptional("value", node.DisplayValue);
        foreach (var item in node.DebugMetadata)
        {
            SelectedMetadata.Add(item);
        }
    }

    private void SetSelectedDiagnostics(ResourceInspectionNodeViewModel? node)
    {
        SelectedDiagnostics.Clear();
        if (node is not null)
        {
            foreach (var diagnostic in node.Diagnostics)
            {
                SelectedDiagnostics.Add(diagnostic);
            }
        }

        OnPropertyChanged(nameof(HasDiagnostics));
    }

    private void AddOptional(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            SelectedMetadata.Add(new ResourceMetadataItemViewModel(name, value));
        }
    }

    private bool TryCreateInspectionOptions(out ResourceInspectionOptions options)
    {
        options = new ResourceInspectionOptions(IncludeDebugMetadata: true);
        if (!TryParseStringKey(KeyText.Trim(), out var stringKey))
        {
            StatusMessage = $"Unknown string key: {KeyText}";
            return false;
        }

        if (!int.TryParse(DepthText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var depth) ||
            depth < 0 ||
            depth > WzImageInspectionReader.MaxPropertyInspectionDepth)
        {
            StatusMessage = $"Depth must be between 0 and {WzImageInspectionReader.MaxPropertyInspectionDepth}.";
            return false;
        }

        options = new ResourceInspectionOptions(stringKey, depth, IncludeDebugMetadata: true);
        return true;
    }

    private static bool TryParseStringKey(string value, out WzStringEncryptionKind? kind)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase))
        {
            kind = null;
            return true;
        }

        if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "noop", StringComparison.OrdinalIgnoreCase))
        {
            kind = WzStringEncryptionKind.None;
            return true;
        }

        if (string.Equals(value, "kms", StringComparison.OrdinalIgnoreCase))
        {
            kind = WzStringEncryptionKind.Kms;
            return true;
        }

        if (string.Equals(value, "gms", StringComparison.OrdinalIgnoreCase))
        {
            kind = WzStringEncryptionKind.Gms;
            return true;
        }

        kind = WzStringEncryptionKind.None;
        return false;
    }

    private static string? NormalizeOptional(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}

public static class ResourceImageSelector
{
    public static string? Normalize(string? selector, params string?[] rootPrefixes)
    {
        var trimmed = selector?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        foreach (var prefix in rootPrefixes)
        {
            var normalizedPrefix = prefix?.Trim().Trim('/');
            if (string.IsNullOrEmpty(normalizedPrefix))
            {
                continue;
            }

            var rootedPrefix = normalizedPrefix + "/";
            if (trimmed.StartsWith(rootedPrefix, StringComparison.OrdinalIgnoreCase) &&
                trimmed.Length > rootedPrefix.Length)
            {
                return trimmed[rootedPrefix.Length..];
            }
        }

        return trimmed;
    }
}

public sealed class ResourceInspectionNodeViewModel
{
    private ResourceInspectionNodeViewModel(
        string name,
        string kind,
        string? path,
        string? displayValue,
        IEnumerable<ResourceInspectionNodeViewModel> children,
        IEnumerable<ResourceMetadataItemViewModel> debugMetadata,
        IEnumerable<ResourceDiagnosticViewModel> diagnostics)
    {
        Name = name;
        Kind = kind;
        Path = path;
        DisplayValue = displayValue;
        Children = new ObservableCollection<ResourceInspectionNodeViewModel>(children);
        DebugMetadata = new ObservableCollection<ResourceMetadataItemViewModel>(debugMetadata);
        Diagnostics = new ObservableCollection<ResourceDiagnosticViewModel>(diagnostics);
    }

    public string Name { get; }

    public string Kind { get; }

    public string? Path { get; }

    public string? DisplayValue { get; }

    public string Title => DisplayValue is null
        ? $"{Name} [{Kind}]"
        : $"{Name} [{Kind}] : {DisplayValue}";

    public ObservableCollection<ResourceInspectionNodeViewModel> Children { get; }

    public ObservableCollection<ResourceMetadataItemViewModel> DebugMetadata { get; }

    public ObservableCollection<ResourceDiagnosticViewModel> Diagnostics { get; }

    public static ResourceInspectionNodeViewModel FromNode(ResourceInspectionNode node)
    {
        return new ResourceInspectionNodeViewModel(
            node.Name,
            node.Kind,
            node.Path,
            node.DisplayValue,
            node.Children.Select(FromNode),
            (node.DebugMetadata ?? []).Select(ResourceMetadataItemViewModel.FromMetadata),
            (node.Diagnostics ?? []).Select(ResourceDiagnosticViewModel.FromDiagnostic));
    }
}

public sealed record ResourceMetadataItemViewModel(string Name, string Value)
{
    public static ResourceMetadataItemViewModel FromMetadata(ResourceInspectionMetadata metadata)
    {
        return new ResourceMetadataItemViewModel(
            metadata.Name,
            Convert.ToString(metadata.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
    }
}

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
