using WzComparerX.WzLib;

namespace WzComparerX.Core;

internal sealed class ResourceImageInspectionContext : IAsyncDisposable
{
    private ResourceImageInspectionContext(
        string sourcePath,
        WzImageInspection imageInspection,
        Stream sourceStream)
    {
        SourcePath = sourcePath;
        ImageInspection = imageInspection;
        SourceStream = sourceStream;
    }

    public string SourcePath { get; }

    public WzImageInspection ImageInspection { get; }

    public Stream SourceStream { get; }

    public static async Task<ResourceImageInspectionContext> LoadAsync(
        string path,
        string selector,
        ResourceInspectionOptions options,
        CancellationToken cancellationToken)
    {
        if (MsMnContainerKind.IsPath(path))
        {
            var msContext = await WzMsImageInspectionLoader.LoadAsync(
                path,
                selector,
                options.StringKey,
                options.MaxPropertyDepth,
                cancellationToken);
            return new ResourceImageInspectionContext(
                msContext.ContainerInspection.Header.SourcePath,
                msContext.ImageInspection,
                msContext.PayloadStream);
        }

        var context = await WzImageInspectionLoader.LoadAsync(
            path,
            selector,
            options.StringKey,
            options.MaxPropertyDepth,
            cancellationToken);
        return new ResourceImageInspectionContext(
            context.DirectoryInspection.Header.SourcePath,
            context.ImageInspection,
            File.OpenRead(context.DirectoryInspection.Header.SourcePath));
    }

    public ValueTask DisposeAsync()
    {
        return SourceStream.DisposeAsync();
    }
}
