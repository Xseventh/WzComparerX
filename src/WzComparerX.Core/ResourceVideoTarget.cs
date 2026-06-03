using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class ResourceVideoTarget : IAsyncDisposable
{
    private readonly CanvasImageInspectionContext context;

    internal ResourceVideoTarget(
        CanvasImageInspectionContext context,
        WzImageVideoInspection value,
        string? valuePath)
    {
        this.context = context;
        Value = value;
        ValuePath = valuePath;
    }

    public string SourcePath => context.SourcePath;

    public string Selector => context.ImageInspection.Selector;

    public string? ValuePath { get; }

    public Stream SourceStream => context.SourceStream;

    public WzImageVideoInspection Value { get; }

    public ValueTask DisposeAsync()
    {
        return context.DisposeAsync();
    }
}
