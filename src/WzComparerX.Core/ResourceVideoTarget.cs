using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed class ResourceVideoTarget : IAsyncDisposable
{
    private readonly ResourceImageInspectionContext context;

    internal ResourceVideoTarget(
        ResourceImageInspectionContext context,
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
