using WzComparerX.WzLib;

namespace WzComparerX.Core;

internal sealed class CanvasImageTarget : IAsyncDisposable
{
    private readonly ResourceImageInspectionContext? ownedContext;

    private CanvasImageTarget(
        ResourceImageInspectionContext context,
        WzImageCanvasInspection value,
        string? path,
        bool ownsContext)
    {
        Context = context;
        Value = value;
        Path = path;
        ownedContext = ownsContext ? context : null;
    }

    public string SourcePath => Context.SourcePath;

    public string Selector => Context.ImageInspection.Selector;

    public Stream SourceStream => Context.SourceStream;

    public WzImageCanvasInspection Value { get; }

    public string? Path { get; }

    private ResourceImageInspectionContext Context { get; }

    public static CanvasImageTarget Local(
        ResourceImageInspectionContext context,
        WzImageCanvasInspection value,
        string? path)
    {
        return new CanvasImageTarget(context, value, path, ownsContext: false);
    }

    public static CanvasImageTarget Linked(
        ResourceImageInspectionContext context,
        WzImageCanvasInspection value,
        string? path)
    {
        return new CanvasImageTarget(context, value, path, ownsContext: true);
    }

    public static CanvasImageTarget Create(
        ResourceImageInspectionContext context,
        WzImageCanvasInspection value,
        string? path,
        bool ownsContext)
    {
        return new CanvasImageTarget(context, value, path, ownsContext);
    }

    public bool UsesContext(ResourceImageInspectionContext context)
    {
        return ReferenceEquals(Context, context);
    }

    public ValueTask DisposeAsync()
    {
        return ownedContext?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}
