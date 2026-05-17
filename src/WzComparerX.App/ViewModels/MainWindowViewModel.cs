using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WzComparerX.App.Services;
using WzComparerX.Core;

namespace WzComparerX.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ResourceInspectionService inspectionService;
    private readonly ResourceFolderInspectionService folderInspectionService;
    private readonly ResourceImageContentWorkflow imageContentWorkflow;
    private readonly ResourceCanvasPreviewWorkflow canvasPreviewWorkflow;
    private readonly Func<ResourceCanvasImageDocument, ResourceCanvasPreviewViewModel> canvasPreviewFactory;
    private int canvasPreviewRequestId;
    private int imageContentRequestId;
    private double canvasPreviewViewportWidth;
    private double canvasPreviewViewportHeight;
    private double canvasPreviewScale = ResourceCanvasPreviewViewModel.DefaultScale;
    private ResourceImageSelectorTarget? currentImageContentTarget;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadImageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ActivateSelectedNodeCommand))]
    private string pathText = "fixtures/synthetic/basic-tree.json";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadImageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ActivateSelectedNodeCommand))]
    private string selectorText = string.Empty;

    [ObservableProperty]
    private string keyText = "auto";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadImageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ActivateSelectedNodeCommand))]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private ResourceInspectionNodeViewModel? selectedNode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private ResourceInspectionNodeViewModel? selectedImageContentNode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadImageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ActivateSelectedNodeCommand))]
    private string currentFormat = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCanvasPreview))]
    private ResourceCanvasPreviewViewModel? canvasPreview;

    [ObservableProperty]
    private string canvasPreviewStatus = "Select a Canvas node to preview.";

    [ObservableProperty]
    private string imageContentStatus = "Select an IMG resource.";

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
        ResourceImageContentWorkflow? imageContentWorkflow = null,
        ResourceCanvasImageService? canvasImageService = null,
        ResourceCanvasPreviewWorkflow? canvasPreviewWorkflow = null,
        Func<ResourceCanvasImageDocument, ResourceCanvasPreviewViewModel>? canvasPreviewFactory = null)
    {
        this.inspectionService = inspectionService;
        this.folderInspectionService = folderInspectionService ?? new ResourceFolderInspectionService();
        this.imageContentWorkflow = imageContentWorkflow ?? new ResourceImageContentWorkflow(inspectionService);
        this.canvasPreviewWorkflow = canvasPreviewWorkflow ?? new ResourceCanvasPreviewWorkflow(canvasImageService);
        this.canvasPreviewFactory = canvasPreviewFactory ?? CreateCanvasPreview;
    }

    public ObservableCollection<ResourceInspectionNodeViewModel> RootNodes { get; } = [];

    public ObservableCollection<ResourceInspectionNodeViewModel> ImageContentNodes { get; } = [];

    public ObservableCollection<ResourceMetadataItemViewModel> DocumentMetadata { get; } = [];

    public ObservableCollection<ResourceMetadataItemViewModel> SelectedMetadata { get; } = [];

    public ObservableCollection<ResourceDiagnosticViewModel> SelectedDiagnostics { get; } = [];

    public ObservableCollection<ResourceActivityLogItemViewModel> ActivityLog { get; } = [];

    public bool HasSelection => SelectedNode is not null || SelectedImageContentNode is not null;

    public bool HasDiagnostics => SelectedDiagnostics.Count > 0;

    public bool HasCanvasPreview => CanvasPreview is not null;

    public bool HasImageContent => ImageContentNodes.Count > 0;

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

    [RelayCommand(CanExecute = nameof(CanActivateSelectedNode))]
    public async Task ActivateSelectedNodeAsync()
    {
        if (CanOpenSelectedPackageNode() && SelectedNode?.Path is not null)
        {
            await OpenPathAsync(SelectedNode.Path);
            return;
        }

        if (CanInspectSelectedImageNode())
        {
            await LoadSelectedImageContentAsync(updateSelector: true);
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
                ClearImageContent("Select an IMG resource.");
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

            var imageTarget = !string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase)
                ? imageContentWorkflow.ResolveManualTarget(path, SelectorText)
                : null;
            if (imageTarget is not null && !PathsEqual(imageTarget.PackagePath, path))
            {
                path = imageTarget.PackagePath;
                PathText = path;
            }

            if (imageTarget is not null &&
                !string.Equals(imageTarget.Selector, NormalizeOptional(SelectorText), StringComparison.Ordinal))
            {
                SelectorText = imageTarget.Selector;
            }

            var document = await inspectionService.InspectAsync(
                path,
                selector: null,
                options);

            ApplyDocument(document);
            StatusMessage = $"Loaded {document.Format}: {Path.GetFileName(document.SourcePath)}";
            AddActivity("success", StatusMessage);

            if (imageTarget is not null)
            {
                await LoadImageContentAsync(imageTarget, updateSelector: true);
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            RootNodes.Clear();
            ClearImageContent("Select an IMG resource.");
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

    [RelayCommand(CanExecute = nameof(CanLoadImage))]
    public async Task LoadImageAsync()
    {
        if (!CanLoadImage())
        {
            return;
        }

        var target = ResolveManualImageTarget();
        if (target is null)
        {
            return;
        }

        SelectorText = target.Selector;
        await LoadImageContentAsync(target, updateSelector: true);
    }

    private bool CanLoad()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(PathText);
    }

    private bool CanInspectSelectedImageNode()
    {
        if (IsBusy ||
            SelectedNode?.Kind != "image" ||
            string.Equals(CurrentFormat, "synthetic", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return imageContentWorkflow.CanLoadSelectedImage(PathText, CurrentFormat, IsBusy, SelectedNode);
    }

    private bool CanLoadImage()
    {
        return CanInspectManualSelector();
    }

    private bool CanInspectManualSelector()
    {
        return imageContentWorkflow.CanLoadManualImage(PathText, CurrentFormat, IsBusy, SelectorText);
    }

    private bool CanOpenSelectedPackageNode()
    {
        return !IsBusy &&
            SelectedNode?.Kind == "package" &&
            !string.IsNullOrWhiteSpace(SelectedNode.Path) &&
            File.Exists(SelectedNode.Path) &&
            !PathsEqual(SelectedNode.Path, PathText);
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
        QueueImageContentInspection(value);
        LoadImageCommand.NotifyCanExecuteChanged();
        ActivateSelectedNodeCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedImageContentNodeChanged(ResourceInspectionNodeViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        SetSelectedMetadata(value);
        SetSelectedDiagnostics(value);
        QueueCanvasPreview(value);
    }

    private void ApplyDocument(ResourceInspectionDocument document)
    {
        ClearImageContent("Select an IMG resource.");
        ClearCanvasPreview("Select a Canvas node to preview.");
        RootNodes.Clear();
        RootNodes.Add(ResourceInspectionNodeViewModel.FromNode(document.Root));
        SetDocumentMetadata(document);
        SelectedNode = RootNodes[0];
        CurrentFormat = document.Format;
    }

    private void QueueImageContentInspection(ResourceInspectionNodeViewModel? node)
    {
        if (node?.Kind != "image")
        {
            return;
        }

        var target = ResolveSelectedImageTarget();
        if (target is null ||
            imageContentWorkflow.IsCurrentTarget(currentImageContentTarget, target))
        {
            return;
        }

        var requestId = Interlocked.Increment(ref imageContentRequestId);
        ImageContentStatus = $"Loading IMG: {target.Selector}";
        _ = LoadImageContentAsync(target, updateSelector: false, requestId);
    }

    private Task LoadSelectedImageContentAsync(bool updateSelector)
    {
        var target = ResolveSelectedImageTarget();
        if (target is null)
        {
            return Task.CompletedTask;
        }

        if (updateSelector)
        {
            SelectorText = target.Selector;
        }

        return LoadImageContentAsync(target, updateSelector);
    }

    private Task LoadImageContentAsync(ResourceImageSelectorTarget target, bool updateSelector)
    {
        return LoadImageContentAsync(target, updateSelector, Interlocked.Increment(ref imageContentRequestId));
    }

    private async Task LoadImageContentAsync(
        ResourceImageSelectorTarget target,
        bool updateSelector,
        int requestId)
    {
        if (!TryCreateInspectionOptions(out var options))
        {
            return;
        }

        try
        {
            ImageContentStatus = $"Loading IMG: {target.Selector}";
            var document = await imageContentWorkflow.LoadAsync(target, options);
            if (requestId != imageContentRequestId)
            {
                return;
            }

            currentImageContentTarget = target;
            ImageContentNodes.Clear();
            ImageContentNodes.Add(ResourceInspectionNodeViewModel.FromNode(document.Root));
            OnPropertyChanged(nameof(HasImageContent));
            SelectedImageContentNode = ImageContentNodes[0];
            if (updateSelector)
            {
                SelectorText = target.Selector;
            }

            ImageContentStatus = $"Loaded IMG: {target.Selector}";
            AddActivity("success", ImageContentStatus);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or NotSupportedException)
        {
            if (requestId != imageContentRequestId)
            {
                return;
            }

            ClearImageContent("Select an IMG resource.");
            ImageContentStatus = ex.Message;
            AddActivity("error", ImageContentStatus);
        }
        catch (ResourceInspectionException ex)
        {
            if (requestId != imageContentRequestId)
            {
                return;
            }

            ClearImageContent("Select an IMG resource.");
            ImageContentStatus = ex.Message;
            SelectedDiagnostics.Add(ResourceDiagnosticViewModel.FromDiagnostic(ex.Diagnostic));
            OnPropertyChanged(nameof(HasDiagnostics));
            AddActivity("error", ImageContentStatus);
        }
    }

    private void ClearImageContent(string status)
    {
        Interlocked.Increment(ref imageContentRequestId);
        currentImageContentTarget = null;
        ImageContentNodes.Clear();
        SelectedImageContentNode = null;
        ImageContentStatus = status;
        OnPropertyChanged(nameof(HasImageContent));
        ClearCanvasPreview("Select a Canvas node to preview.");
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
        ReplaceCollection(DocumentMetadata, ResourceDetailsProjection.CreateDocumentMetadata(document));
    }

    private void SetSelectedMetadata(ResourceInspectionNodeViewModel? node)
    {
        ReplaceCollection(SelectedMetadata, ResourceDetailsProjection.CreateSelectionMetadata(node));
    }

    private void SetSelectedDiagnostics(ResourceInspectionNodeViewModel? node)
    {
        ReplaceCollection(SelectedDiagnostics, ResourceDetailsProjection.CreateSelectionDiagnostics(node));
        OnPropertyChanged(nameof(HasDiagnostics));
    }

    private void QueueCanvasPreview(ResourceInspectionNodeViewModel? node)
    {
        var requestId = Interlocked.Increment(ref canvasPreviewRequestId);
        ClearCanvasPreview(canvasPreviewWorkflow.GetIdleStatus(node));
        var target = ResolveCanvasPreviewImageTarget(node);
        if (!canvasPreviewWorkflow.CanLoad(node, target, IsBusy))
        {
            return;
        }

        CanvasPreviewStatus = "Loading Canvas preview...";
        _ = LoadCanvasPreviewAsync(node!, target!, requestId);
    }

    public Task LoadCanvasPreviewAsync(ResourceInspectionNodeViewModel node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var target = ResolveCanvasPreviewImageTarget(node);
        return target is null
            ? Task.CompletedTask
            : LoadCanvasPreviewAsync(node, target, Interlocked.Increment(ref canvasPreviewRequestId));
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
            canvasPreviewScale = CanvasPreview.CalculateViewportFitScale(
                canvasPreviewViewportWidth,
                canvasPreviewViewportHeight);
            CanvasPreview.SetScale(canvasPreviewScale);
            return;
        }

        var normalizedScale = scale?.Trim().TrimEnd('x', 'X');
        if (double.TryParse(
            normalizedScale,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var parsedScale))
        {
            CanvasPreview.SetScale(parsedScale);
            canvasPreviewScale = CanvasPreview.Scale;
        }
    }

    public void SetCanvasPreviewViewport(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (Math.Abs(width - canvasPreviewViewportWidth) < 0.5 &&
            Math.Abs(height - canvasPreviewViewportHeight) < 0.5)
        {
            return;
        }

        canvasPreviewViewportWidth = width;
        canvasPreviewViewportHeight = height;
    }

    private async Task LoadCanvasPreviewAsync(
        ResourceInspectionNodeViewModel node,
        ResourceImageSelectorTarget target,
        int requestId)
    {
        if (!canvasPreviewWorkflow.CanLoad(node, target, IsBusy))
        {
            return;
        }

        if (!TryCreateInspectionOptions(out var options))
        {
            return;
        }

        try
        {
            var document = await canvasPreviewWorkflow.LoadAsync(node, target, options);
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
        catch (ResourceInspectionException ex)
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

    private static ResourceCanvasPreviewViewModel CreateCanvasPreview(ResourceCanvasImageDocument document)
    {
        return new ResourceCanvasPreviewViewModel(document, ResourceCanvasBitmapFactory.Create(document));
    }

    private ResourceImageSelectorTarget? ResolveSelectedImageTarget()
    {
        return imageContentWorkflow.ResolveSelectedTarget(PathText, SelectedNode);
    }

    private ResourceImageSelectorTarget? ResolveManualImageTarget()
    {
        return imageContentWorkflow.ResolveManualTarget(PathText, SelectorText);
    }

    private ResourceImageSelectorTarget? ResolveCanvasPreviewImageTarget(ResourceInspectionNodeViewModel? node)
    {
        return node is not null ? currentImageContentTarget : null;
    }

    private bool IsCurrentCanvasPreviewRequest(ResourceInspectionNodeViewModel node, int requestId)
    {
        return requestId == canvasPreviewRequestId && ReferenceEquals(node, SelectedImageContentNode);
    }

    private void ReplaceCanvasPreview(ResourceCanvasPreviewViewModel? preview)
    {
        var previous = CanvasPreview;
        if (preview is not null)
        {
            preview.SetScale(canvasPreviewScale);
        }

        CanvasPreview = preview;
        previous?.Dispose();
    }

    private void ClearCanvasPreview(string status)
    {
        ReplaceCanvasPreview(null);
        CanvasPreviewStatus = status;
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

    private static void ReplaceCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
