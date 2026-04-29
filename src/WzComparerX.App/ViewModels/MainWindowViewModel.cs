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
    private readonly ResourceCanvasImageService canvasImageService;
    private readonly Func<ResourceCanvasImageDocument, ResourceCanvasPreviewViewModel> canvasPreviewFactory;
    private int canvasPreviewRequestId;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCanvasPreview))]
    private ResourceCanvasPreviewViewModel? canvasPreview;

    [ObservableProperty]
    private string canvasPreviewStatus = "Select a Canvas node to preview.";

    public MainWindowViewModel()
        : this(new ResourceInspectionService())
    {
    }

    public MainWindowViewModel(Func<ResourceCanvasImageDocument, ResourceCanvasPreviewViewModel> canvasPreviewFactory)
        : this(new ResourceInspectionService(), canvasPreviewFactory: canvasPreviewFactory)
    {
    }

    public MainWindowViewModel(
        ResourceInspectionService inspectionService,
        ResourceFolderInspectionService? folderInspectionService = null,
        ResourceCanvasImageService? canvasImageService = null,
        Func<ResourceCanvasImageDocument, ResourceCanvasPreviewViewModel>? canvasPreviewFactory = null)
    {
        this.inspectionService = inspectionService;
        this.folderInspectionService = folderInspectionService ?? new ResourceFolderInspectionService();
        this.canvasImageService = canvasImageService ?? new ResourceCanvasImageService();
        this.canvasPreviewFactory = canvasPreviewFactory ?? CreateCanvasPreview;
    }

    public ObservableCollection<ResourceInspectionNodeViewModel> RootNodes { get; } = [];

    public ObservableCollection<ResourceMetadataItemViewModel> DocumentMetadata { get; } = [];

    public ObservableCollection<ResourceMetadataItemViewModel> SelectedMetadata { get; } = [];

    public ObservableCollection<ResourceDiagnosticViewModel> SelectedDiagnostics { get; } = [];

    public ObservableCollection<ResourceActivityLogItemViewModel> ActivityLog { get; } = [];

    public bool HasSelection => SelectedNode is not null;

    public bool HasDiagnostics => SelectedDiagnostics.Count > 0;

    public bool HasCanvasPreview => CanvasPreview is not null;

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

            var imageTarget = ResourceImageSelector.Resolve(path, SelectorText);
            if (imageTarget is not null && !PathsEqual(imageTarget.PackagePath, path))
            {
                path = imageTarget.PackagePath;
                PathText = path;
            }

            var selector = imageTarget?.Selector;
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

        var target = CanInspectSelectedImageNode()
            ? ResolveSelectedImageTarget()
            : ResolveManualImageTarget();
        if (target is null)
        {
            return;
        }

        PathText = target.PackagePath;
        SelectorText = target.Selector;
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

        var target = ResolveSelectedImageTarget();
        return target is not null && !IsCurrentImageTarget(target);
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

        var target = ResolveManualImageTarget();
        return target is not null &&
            !IsCurrentImageTarget(target) &&
            !string.Equals(Path.GetExtension(PathText.Trim()), ".json", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCurrentImageTarget(ResourceImageSelectorTarget target)
    {
        return RootNodes.Count == 1 &&
            RootNodes[0].Kind == "image" &&
            string.Equals(RootNodes[0].Name, target.Selector, StringComparison.Ordinal) &&
            PathsEqual(PathText.Trim(), target.PackagePath);
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
        QueueCanvasPreview(value);
        InspectImageCommand.NotifyCanExecuteChanged();
        OpenPackageCommand.NotifyCanExecuteChanged();
        ActivateSelectedNodeCommand.NotifyCanExecuteChanged();
    }

    private void ApplyDocument(ResourceInspectionDocument document)
    {
        ClearCanvasPreview("Select a Canvas node to preview.");
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

    private void QueueCanvasPreview(ResourceInspectionNodeViewModel? node)
    {
        var requestId = Interlocked.Increment(ref canvasPreviewRequestId);
        ClearCanvasPreview(GetCanvasPreviewIdleStatus(node));
        if (!CanLoadCanvasPreview(node))
        {
            return;
        }

        CanvasPreviewStatus = "Loading Canvas preview...";
        _ = LoadCanvasPreviewAsync(node!, requestId);
    }

    public Task LoadCanvasPreviewAsync(ResourceInspectionNodeViewModel node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return LoadCanvasPreviewAsync(node, Interlocked.Increment(ref canvasPreviewRequestId));
    }

    [RelayCommand]
    private void SetCanvasPreviewScale(string? scale)
    {
        if (CanvasPreview is null)
        {
            return;
        }

        if (string.Equals(scale, "auto", StringComparison.OrdinalIgnoreCase))
        {
            CanvasPreview.SetScale(null);
            return;
        }

        var normalizedScale = scale?.Trim().TrimEnd('x', 'X');
        if (int.TryParse(normalizedScale, out var parsedScale))
        {
            CanvasPreview.SetScale(parsedScale);
        }
    }

    private async Task LoadCanvasPreviewAsync(ResourceInspectionNodeViewModel node, int requestId)
    {
        if (!CanLoadCanvasPreview(node))
        {
            return;
        }

        if (!TryCreateInspectionOptions(out var options))
        {
            return;
        }

        var target = ResolveCanvasPreviewImageTarget(node);
        if (target is null || string.IsNullOrWhiteSpace(target.Selector))
        {
            CanvasPreviewStatus = "Select an inspected IMG before previewing Canvas.";
            return;
        }

        try
        {
            var valueSelector = GetCanvasPreviewValueSelector(node);
            var document = valueSelector is null
                ? await canvasImageService.LoadFirstAsync(
                    target.PackagePath,
                    target.Selector,
                    options)
                : await canvasImageService.LoadAsync(
                    target.PackagePath,
                    target.Selector,
                    valueSelector,
                    options);
            var preview = canvasPreviewFactory(document);
            if (!IsCurrentCanvasPreviewRequest(node, requestId))
            {
                preview.Dispose();
                return;
            }

            ReplaceCanvasPreview(preview);
            CanvasPreviewStatus = $"Loaded Canvas preview: {preview.Title}";
            AddActivity("success", CanvasPreviewStatus);
        }
        catch (ResourceCanvasImageException ex)
        {
            if (!IsCurrentCanvasPreviewRequest(node, requestId))
            {
                return;
            }

            CanvasPreviewStatus = ex.Message;
            SelectedDiagnostics.Add(ResourceDiagnosticViewModel.FromDiagnostic(ex.Diagnostic));
            OnPropertyChanged(nameof(HasDiagnostics));
            AddActivity("error", $"Canvas preview failed: {ex.Message}");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or NotSupportedException)
        {
            if (!IsCurrentCanvasPreviewRequest(node, requestId))
            {
                return;
            }

            CanvasPreviewStatus = ex.Message;
            AddActivity("error", $"Canvas preview failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            if (!IsCurrentCanvasPreviewRequest(node, requestId))
            {
                return;
            }

            CanvasPreviewStatus = ex.Message;
            AddActivity("error", $"Canvas preview failed: {ex.Message}");
        }
    }

    private bool CanLoadCanvasPreview(ResourceInspectionNodeViewModel? node)
    {
        var target = ResolveCanvasPreviewImageTarget(node);
        return IsCanvasPreviewNode(node) &&
            !IsBusy &&
            target is not null &&
            !string.Equals(Path.GetExtension(target.PackagePath), ".json", StringComparison.OrdinalIgnoreCase) &&
            File.Exists(target.PackagePath);
    }

    private static bool IsCanvasPreviewNode(ResourceInspectionNodeViewModel? node)
    {
        return node is not null &&
            (node.Kind == "image" || node.Kind == "canvas");
    }

    private static string? GetCanvasPreviewValueSelector(ResourceInspectionNodeViewModel node)
    {
        return node.Kind == "canvas" ? node.Path : null;
    }

    private static string GetCanvasPreviewIdleStatus(ResourceInspectionNodeViewModel? node)
    {
        return node?.Kind == "image"
            ? "Select an IMG to preview its first Canvas value."
            : "Select a Canvas node to preview.";
    }

    private static ResourceCanvasPreviewViewModel CreateCanvasPreview(ResourceCanvasImageDocument document)
    {
        return new ResourceCanvasPreviewViewModel(document, ResourceCanvasBitmapFactory.Create(document));
    }

    private ResourceImageSelectorTarget? ResolveSelectedImageTarget()
    {
        return SelectedNode?.Kind == "image"
            ? ResourceImageSelector.Resolve(PathText.Trim(), SelectedNode.Path ?? SelectedNode.Name)
            : null;
    }

    private ResourceImageSelectorTarget? ResolveManualImageTarget()
    {
        return ResourceImageSelector.Resolve(PathText.Trim(), SelectorText);
    }

    private ResourceImageSelectorTarget? ResolveCanvasPreviewImageTarget(ResourceInspectionNodeViewModel? node)
    {
        return node?.Kind == "image"
            ? ResourceImageSelector.Resolve(PathText.Trim(), node.Path ?? node.Name)
            : ResolveManualImageTarget();
    }

    private bool IsCurrentCanvasPreviewRequest(ResourceInspectionNodeViewModel node, int requestId)
    {
        return requestId == canvasPreviewRequestId && ReferenceEquals(node, SelectedNode);
    }

    private void ReplaceCanvasPreview(ResourceCanvasPreviewViewModel? preview)
    {
        var previous = CanvasPreview;
        CanvasPreview = preview;
        previous?.Dispose();
    }

    private void ClearCanvasPreview(string status)
    {
        ReplaceCanvasPreview(null);
        CanvasPreviewStatus = status;
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
