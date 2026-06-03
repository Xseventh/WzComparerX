using WzComparerX.Core;

namespace WzComparerX.Rendering;

public sealed class ResourceVideoSequenceService
{
    private readonly ResourceVideoTargetService targetService;
    private readonly WzImageVideoSequenceDecoder decoder;

    public ResourceVideoSequenceService(
        ResourceVideoTargetService? targetService = null,
        WzImageVideoSequenceDecoder? decoder = null)
    {
        this.targetService = targetService ?? new ResourceVideoTargetService();
        this.decoder = decoder ?? new WzImageVideoSequenceDecoder();
    }

    public async Task<ResourceVideoSequenceDocument> LoadAsync(
        string path,
        string selector,
        string? valueSelector,
        ResourceInspectionOptions? inspectionOptions = null,
        WzVideoDecodeOptions? decodeOptions = null,
        CancellationToken cancellationToken = default)
    {
        await using var target = await targetService.LoadAsync(
            path,
            selector,
            valueSelector,
            inspectionOptions,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        var result = decoder.DecodeSequence(
            target.SourceStream,
            target.Value,
            decodeOptions);
        if (!result.Succeeded)
        {
            throw new ResourceVideoSequenceException(result.Diagnostic!);
        }

        var sequence = result.Sequence!;
        return new ResourceVideoSequenceDocument(
            target.SourcePath,
            target.Selector,
            target.ValuePath,
            target.Value.Header?.FourCcText ?? string.Empty,
            sequence.Width,
            sequence.Height,
            sequence.PixelFormat,
            sequence.Frames);
    }
}
