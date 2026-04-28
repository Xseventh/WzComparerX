using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WzComparerX.Core;

namespace WzComparerX.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ResourceInspectionService inspectionService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    private string pathText = "fixtures/synthetic/basic-tree.json";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private ResourceInspectionNodeViewModel? selectedNode;

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
            var document = await inspectionService.InspectAsync(
                path,
                selector: null,
                new ResourceInspectionOptions(
                    StringKey: null,
                    MaxPropertyDepth: 2,
                    IncludeDebugMetadata: true));

            RootNodes.Clear();
            RootNodes.Add(ResourceInspectionNodeViewModel.FromNode(document.Root));
            SetDocumentMetadata(document);
            SelectedNode = RootNodes[0];
            StatusMessage = $"Loaded {document.Format}: {Path.GetFileName(document.SourcePath)}";
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            RootNodes.Clear();
            DocumentMetadata.Clear();
            SelectedNode = null;
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanLoad()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(PathText);
    }

    partial void OnSelectedNodeChanged(ResourceInspectionNodeViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        SetSelectedMetadata(value);
        SetSelectedDiagnostics(value);
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
