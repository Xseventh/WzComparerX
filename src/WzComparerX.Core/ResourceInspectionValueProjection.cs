using WzComparerX.WzLib;

namespace WzComparerX.Core;

internal static class ResourceInspectionValueProjection
{
    public static void AddMetadata(List<ResourceInspectionMetadata> metadata, object? value)
    {
        switch (value)
        {
            case WzImageCanvasInspection canvas:
                metadata.Add(new ResourceInspectionMetadata("valueType", "canvas"));
                metadata.Add(new ResourceInspectionMetadata("width", canvas.Width));
                metadata.Add(new ResourceInspectionMetadata("height", canvas.Height));
                metadata.Add(new ResourceInspectionMetadata("format", canvas.Format));
                metadata.Add(new ResourceInspectionMetadata("scale", canvas.Scale));
                metadata.Add(new ResourceInspectionMetadata("actualScale", canvas.ActualScale));
                metadata.Add(new ResourceInspectionMetadata("pages", canvas.Pages));
                metadata.Add(new ResourceInspectionMetadata("actualPages", canvas.ActualPages));
                metadata.Add(new ResourceInspectionMetadata("unknown1", canvas.Unknown1));
                metadata.Add(new ResourceInspectionMetadata("dataOffset", canvas.DataOffset));
                metadata.Add(new ResourceInspectionMetadata("dataLength", canvas.DataLength));
                metadata.Add(new ResourceInspectionMetadata("compressionKind", canvas.CompressionKind.ToString()));
                AddOptional(metadata, "uncompressedDataLength", canvas.UncompressedDataLength);
                break;
            case WzImageRawDataInspection rawData:
                metadata.Add(new ResourceInspectionMetadata("valueType", "rawData"));
                metadata.Add(new ResourceInspectionMetadata("version", rawData.Version));
                metadata.Add(new ResourceInspectionMetadata("dataOffset", rawData.DataOffset));
                metadata.Add(new ResourceInspectionMetadata("dataLength", rawData.DataLength));
                break;
            case WzImageVideoInspection video:
                metadata.Add(new ResourceInspectionMetadata("valueType", "video"));
                metadata.Add(new ResourceInspectionMetadata("unknown", video.Unknown));
                metadata.Add(new ResourceInspectionMetadata("dataOffset", video.DataOffset));
                metadata.Add(new ResourceInspectionMetadata("dataLength", video.DataLength));
                AddVideoHeaderMetadata(metadata, video);
                break;
            case WzImageSoundInspection sound:
                metadata.Add(new ResourceInspectionMetadata("valueType", "sound"));
                metadata.Add(new ResourceInspectionMetadata("version", sound.Version));
                metadata.Add(new ResourceInspectionMetadata("duration", sound.Duration));
                metadata.Add(new ResourceInspectionMetadata("soundDeclaration", sound.SoundDeclaration));
                metadata.Add(new ResourceInspectionMetadata("majorType", sound.MajorType));
                metadata.Add(new ResourceInspectionMetadata("subType", sound.SubType));
                metadata.Add(new ResourceInspectionMetadata("fixedSizeSamples", sound.FixedSizeSamples));
                metadata.Add(new ResourceInspectionMetadata("temporalCompression", sound.TemporalCompression));
                metadata.Add(new ResourceInspectionMetadata("formatType", sound.FormatType));
                AddOptional(metadata, "formatExtraLength", sound.FormatExtraLength);
                metadata.Add(new ResourceInspectionMetadata("dataOffset", sound.DataOffset));
                metadata.Add(new ResourceInspectionMetadata("dataLength", sound.DataLength));
                break;
            case WzImageLuaInspection lua:
                metadata.Add(new ResourceInspectionMetadata("valueType", "lua"));
                metadata.Add(new ResourceInspectionMetadata("scriptLength", lua.ScriptLength));
                metadata.Add(new ResourceInspectionMetadata("snippet", lua.Snippet));
                break;
            case WzImageTextInspection text:
                metadata.Add(new ResourceInspectionMetadata("valueType", "textImage"));
                metadata.Add(new ResourceInspectionMetadata("textFormat", text.Format));
                metadata.Add(new ResourceInspectionMetadata("textLength", text.TextLength));
                break;
            case WzImageVectorInspection vector:
                metadata.Add(new ResourceInspectionMetadata("valueType", "vector"));
                metadata.Add(new ResourceInspectionMetadata("x", vector.X));
                metadata.Add(new ResourceInspectionMetadata("y", vector.Y));
                break;
            case WzImageConvexInspection convex:
                metadata.Add(new ResourceInspectionMetadata("valueType", "convex"));
                metadata.Add(new ResourceInspectionMetadata("pointCount", convex.Points.Count));
                break;
            case not null:
                metadata.Add(new ResourceInspectionMetadata("valueType", value.GetType().Name));
                break;
        }
    }

    public static IReadOnlyList<ResourceInspectionDiagnostic>? BuildDiagnostics(object? value, string? path)
    {
        var diagnostic = value switch
        {
            WzImageCanvasInspection => ResourceInspectionDiagnostics.CanvasPixelDecodingPartial(path),
            WzImageRawDataInspection => ResourceInspectionDiagnostics.RawDataPayloadDecodingUnsupported(path),
            WzImageVideoInspection => ResourceInspectionDiagnostics.VideoPayloadDecodingUnsupported(path),
            WzImageSoundInspection => ResourceInspectionDiagnostics.AudioPayloadDecodingUnsupported(path),
            _ => null
        };

        return diagnostic is null
            ? null
            : [diagnostic];
    }

    private static void AddOptional(List<ResourceInspectionMetadata> metadata, string name, object? value)
    {
        if (value is not null)
        {
            metadata.Add(new ResourceInspectionMetadata(name, value));
        }
    }

    private static void AddVideoHeaderMetadata(List<ResourceInspectionMetadata> metadata, WzImageVideoInspection video)
    {
        if (video.Header is null)
        {
            AddOptional(metadata, "headerError", video.HeaderError);
            return;
        }

        var header = video.Header;
        metadata.Add(new ResourceInspectionMetadata("signature", header.Signature));
        metadata.Add(new ResourceInspectionMetadata("headerLength", header.HeaderLength));
        metadata.Add(new ResourceInspectionMetadata("fourCc", header.FourCcText));
        metadata.Add(new ResourceInspectionMetadata("fourCcValue", header.FourCc));
        metadata.Add(new ResourceInspectionMetadata("width", header.Width));
        metadata.Add(new ResourceInspectionMetadata("height", header.Height));
        metadata.Add(new ResourceInspectionMetadata("frameCount", header.FrameCount));
        metadata.Add(new ResourceInspectionMetadata("dataFlags", header.DataFlags.ToString()));
        metadata.Add(new ResourceInspectionMetadata("hasAlphaMap", header.DataFlags.HasFlag(WzImageVideoDataFlags.AlphaMap)));
        metadata.Add(new ResourceInspectionMetadata("hasPerFrameDelay", header.DataFlags.HasFlag(WzImageVideoDataFlags.PerFrameDelay)));
        metadata.Add(new ResourceInspectionMetadata("hasPerFrameTimeline", header.DataFlags.HasFlag(WzImageVideoDataFlags.PerFrameTimeline)));
        metadata.Add(new ResourceInspectionMetadata("frameDelayUnit", header.FrameDelayUnit));
        metadata.Add(new ResourceInspectionMetadata("defaultDelay", header.DefaultDelay));

        if (header.Frames.Count == 0)
        {
            return;
        }

        var firstFrame = header.Frames[0];
        metadata.Add(new ResourceInspectionMetadata("firstFrameDataOffset", firstFrame.DataOffset));
        metadata.Add(new ResourceInspectionMetadata("firstFrameDataLength", firstFrame.DataLength));
        metadata.Add(new ResourceInspectionMetadata("firstFrameAlphaDataOffset", firstFrame.AlphaDataOffset));
        metadata.Add(new ResourceInspectionMetadata("firstFrameAlphaDataLength", firstFrame.AlphaDataLength));
        metadata.Add(new ResourceInspectionMetadata("firstFrameDelayNanoseconds", firstFrame.DelayInNanoseconds));
        metadata.Add(new ResourceInspectionMetadata("firstFrameStartNanoseconds", firstFrame.StartTimeInNanoseconds));
    }
}
