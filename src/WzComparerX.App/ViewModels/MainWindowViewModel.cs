using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WzComparerX.App.Services;
using WzComparerX.Core;

namespace WzComparerX.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ResourceInspectionService inspectionService;
    private readonly ResourceFolderInspectionService folderInspectionService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenPackageCommand))]
    [NotifyCanExecuteChangedFor(nameof(InspectImageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ActivateSelectedNodeCommand))]
    private string pathText = "fixtures/synthetic/basic-tree.json";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenPackageCommand))]
    [NotifyCanExecuteChangedFor(nameof(InspectImageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ActivateSelectedNodeCommand))]
    private string selectorText = string.Empty;

    [ObservableProperty]
    private string keyText = "auto";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(InspectImageCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenPackageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ActivateSelectedNodeCommand))]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private ResourceInspectionNodeViewModel? selectedNode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InspectImageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ActivateSelectedNodeCommand))]
    private string currentFormat = string.Empty;

    public MainWindowViewModel()
        : this(new ResourceInspectionService())
    {
    }

    internal MainWindowViewModel(
        ResourceInspectionService inspectionService,
        ResourceFolderInspectionService? folderInspectionService = null)
    {
        this.inspectionService = inspectionService;
        this.folderInspectionService = folderInspectionService ?? new ResourceFolderInspectionService();
    }

    public ObservableCollection<ResourceInspectionNodeViewModel> RootNodes { get; } = [];

    public ObservableCollection<ResourceMetadataItemViewModel> DocumentMetadata { get; } = [];

    public ObservableCollection<ResourceMetadataItemViewModel> SelectedMetadata { get; } = [];

    public ObservableCollection<ResourceDiagnosticViewModel> SelectedDiagnostics { get; } = [];

    public ObservableCollection<ResourceActivityLogItemViewModel> ActivityLog { get; } = [];

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

    [RelayCommand(CanExecute = nameof(CanOpenPackage))]
    public async Task OpenPackageAsync()
    {
        if (CanOpenSelectedPackageNode() && SelectedNode?.Path is not null)
        {
            await OpenPathAsync(SelectedNode.Path);
            return;
        }

        if (CanOpenCurrentPackage())
        {
            SelectorText = string.Empty;
            await LoadAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(CanActivateSelectedNode))]
    public async Task ActivateSelectedNodeAsync()
    {
        if (CanOpenSelectedPackageNode())
        {
            await OpenPackageAsync();
            return;
        }

        if (CanInspectSelectedImageNode())
        {
            await InspectImageAsync();
        }
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
            AddActivity("info", $"Loading {GetDisplayPathName(path)}");

            if (Directory.Exists(path))
            {
                SelectorText = string.Empty;
                var folderDocument = await folderInspectionService.InspectAsync(path);
                ApplyDocument(folderDocument);
                StatusMessage = $"Loaded folder: {Path.GetFileName(Path.TrimEndingDirectorySeparator(folderDocument.SourcePath))}";
                AddActivity("success", StatusMessage);
                return;
            }

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

            ApplyDocument(document);
            StatusMessage = $"Loaded {document.Format}: {Path.GetFileName(document.SourcePath)}";
            AddActivity("success", StatusMessage);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            RootNodes.Clear();
            DocumentMetadata.Clear();
            SelectedNode = null;
            CurrentFormat = string.Empty;
            StatusMessage = ex.Message;
            AddActivity("error", StatusMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanInspectImage))]
    public async Task InspectImageAsync()
    {
        if (!CanInspectImage())
        {
            return;
        }

        if (CanInspectSelectedImageNode())
        {
            SelectorText = ResourceImageSelector.Normalize(
                SelectedNode?.Path ?? SelectedNode?.Name,
                Path.GetFileName(PathText.Trim())) ?? string.Empty;
        }

        await LoadAsync();
    }

    private bool CanLoad()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(PathText);
    }

    private bool CanInspectImage()
    {
        return CanInspectSelectedImageNode() || CanInspectManualSelector();
    }

    private bool CanInspectSelectedImageNode()
    {
        if (IsBusy ||
            SelectedNode?.Kind != "image" ||
            string.Equals(CurrentFormat, "synthetic", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var selectedSelector = ResourceImageSelector.Normalize(
            SelectedNode.Path ?? SelectedNode.Name,
            Path.GetFileName(PathText.Trim()));
        var currentSelector = ResourceImageSelector.Normalize(
            SelectorText,
            Path.GetFileName(PathText.Trim()));
        return selectedSelector is not null &&
            !string.Equals(selectedSelector, currentSelector, StringComparison.Ordinal);
    }

    private bool CanInspectManualSelector()
    {
        if (IsBusy ||
            string.IsNullOrWhiteSpace(PathText) ||
            Directory.Exists(PathText.Trim()) ||
            string.Equals(CurrentFormat, "synthetic", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var selector = ResourceImageSelector.Normalize(
            SelectorText,
            Path.GetFileName(PathText.Trim()));
        return !string.IsNullOrWhiteSpace(selector) &&
            !IsCurrentImageSelector(selector) &&
            !string.Equals(Path.GetExtension(PathText.Trim()), ".json", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCurrentImageSelector(string selector)
    {
        return RootNodes.Count == 1 &&
            RootNodes[0].Kind == "image" &&
            string.Equals(RootNodes[0].Name, selector, StringComparison.Ordinal);
    }

    private bool CanOpenPackage()
    {
        return CanOpenSelectedPackageNode() || CanOpenCurrentPackage();
    }

    private bool CanOpenSelectedPackageNode()
    {
        return !IsBusy &&
            SelectedNode?.Kind == "package" &&
            !string.IsNullOrWhiteSpace(SelectedNode.Path) &&
            File.Exists(SelectedNode.Path) &&
            !PathsEqual(SelectedNode.Path, PathText);
    }

    private bool CanOpenCurrentPackage()
    {
        return !IsBusy &&
            RootNodes.Count == 1 &&
            RootNodes[0].Kind == "image" &&
            !string.IsNullOrWhiteSpace(SelectorText) &&
            !string.IsNullOrWhiteSpace(PathText) &&
            !Directory.Exists(PathText.Trim()) &&
            !string.Equals(Path.GetExtension(PathText.Trim()), ".json", StringComparison.OrdinalIgnoreCase);
    }

    private bool CanActivateSelectedNode()
    {
        return CanOpenSelectedPackageNode() || CanInspectSelectedImageNode();
    }

    partial void OnSelectedNodeChanged(ResourceInspectionNodeViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        SetSelectedMetadata(value);
        SetSelectedDiagnostics(value);
        InspectImageCommand.NotifyCanExecuteChanged();
        OpenPackageCommand.NotifyCanExecuteChanged();
        ActivateSelectedNodeCommand.NotifyCanExecuteChanged();
    }

    private void ApplyDocument(ResourceInspectionDocument document)
    {
        RootNodes.Clear();
        RootNodes.Add(ResourceInspectionNodeViewModel.FromNode(document.Root));
        SetDocumentMetadata(document);
        SelectedNode = RootNodes[0];
        CurrentFormat = document.Format;
    }

    private void AddActivity(string kind, string message)
    {
        ActivityLog.Insert(0, new ResourceActivityLogItemViewModel(kind, message));
        const int maxActivityLogItems = 100;
        while (ActivityLog.Count > maxActivityLogItems)
        {
            ActivityLog.RemoveAt(ActivityLog.Count - 1);
        }
    }

    private static string GetDisplayPathName(string path)
    {
        var normalizedPath = Path.TrimEndingDirectorySeparator(path);
        var name = Path.GetFileName(normalizedPath);
        return string.IsNullOrWhiteSpace(name) ? normalizedPath : name;
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
        if (!ResourceInspectionOptionParser.TryParse(KeyText, out options, out var errorMessage))
        {
            StatusMessage = errorMessage;
            AddActivity("error", StatusMessage);
            return false;
        }

        return true;
    }

    private static string? NormalizeOptional(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(left),
                Path.GetFullPath(right),
                StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }
    }
}
