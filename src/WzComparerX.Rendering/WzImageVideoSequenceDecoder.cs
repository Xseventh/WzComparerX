using WzComparerX.WzLib;

namespace WzComparerX.Rendering;

public sealed class WzImageVideoSequenceDecoder
{
    private readonly Func<string, IWzRawVideoPacketDecoder> decoderFactory;

    public WzImageVideoSequenceDecoder()
        : this(CreateDefaultDecoder)
    {
    }

    public WzImageVideoSequenceDecoder(Func<IWzRawVideoPacketDecoder> decoderFactory)
        : this(_ => (decoderFactory ?? throw new ArgumentNullException(nameof(decoderFactory)))())
    {
    }

    public WzImageVideoSequenceDecoder(Func<string, IWzRawVideoPacketDecoder> decoderFactory)
    {
        this.decoderFactory = decoderFactory ?? throw new ArgumentNullException(nameof(decoderFactory));
    }

    public WzVideoSequenceDecodeResult DecodeSequence(
        Stream imagePayloadStream,
        WzImageVideoInspection video,
        WzVideoDecodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(imagePayloadStream);
        ArgumentNullException.ThrowIfNull(video);
        options ??= WzVideoDecodeOptions.Default;

        if (!Enum.IsDefined(options.OutputFormat))
        {
            return WzVideoSequenceDecodeResult.Fail(
                WzVideoDecodeDiagnostic.InvalidOptions($"Unsupported WCX video output pixel format value {options.OutputFormat}."));
        }

        var diagnostic = WzImageVideoDecodePrimitives.ValidateVideo(imagePayloadStream, video, out var header);
        if (diagnostic is not null)
        {
            return WzVideoSequenceDecodeResult.Fail(diagnostic);
        }

        var requiresAlphaState = header!.DataFlags.HasFlag(WzImageVideoDataFlags.AlphaMap) ||
            header.Frames.Any(frame => frame.AlphaDataOffset >= 0 && frame.AlphaDataLength > 0);
        var decodeOptions = requiresAlphaState && options.OutputFormat != WzVideoPixelFormat.Bgra8888
            ? options with { OutputFormat = WzVideoPixelFormat.Bgra8888 }
            : options;
        var colorDecoder = decoderFactory(header!.FourCcText);
        var alphaDecoder = requiresAlphaState
            ? decoderFactory(header.FourCcText)
            : null;
        var frames = new List<WzDecodedVideoFrame>(header.Frames.Count);

        foreach (var frame in header.Frames)
        {
            var colorPacket = WzImageVideoDecodePrimitives.ReadPacket(imagePayloadStream, video, frame.DataOffset, frame.DataLength, "Color");
            if (colorPacket.Diagnostic is not null)
            {
                return WzVideoSequenceDecodeResult.Fail(colorPacket.Diagnostic);
            }

            var colorResult = colorDecoder.Decode(
                colorPacket.Packet,
                alphaPacket: null,
                header.Width,
                header.Height,
                decodeOptions);
            if (!colorResult.Succeeded)
            {
                return WzVideoSequenceDecodeResult.Fail(WzVideoDecodeDiagnostic.FrameDecodeFailed(frame.Index, colorResult.Diagnostic!));
            }

            if (colorResult.NoDisplayFrame)
            {
                if (frame.AlphaDataOffset >= 0 && frame.AlphaDataLength > 0)
                {
                    var alphaPacket = WzImageVideoDecodePrimitives.ReadPacket(imagePayloadStream, video, frame.AlphaDataOffset, frame.AlphaDataLength, "Alpha");
                    if (alphaPacket.Diagnostic is not null)
                    {
                        return WzVideoSequenceDecodeResult.Fail(alphaPacket.Diagnostic);
                    }

                    var alphaResult = alphaDecoder!.Decode(
                        alphaPacket.Packet,
                        alphaPacket: null,
                        header.Width,
                        header.Height,
                        decodeOptions);
                    if (!alphaResult.Succeeded)
                    {
                        return WzVideoSequenceDecodeResult.Fail(WzVideoDecodeDiagnostic.FrameDecodeFailed(frame.Index, alphaResult.Diagnostic!));
                    }
                }

                continue;
            }

            var rawFrame = colorResult.Frame!;
            if (frame.AlphaDataOffset >= 0 && frame.AlphaDataLength > 0)
            {
                var alphaPacket = WzImageVideoDecodePrimitives.ReadPacket(imagePayloadStream, video, frame.AlphaDataOffset, frame.AlphaDataLength, "Alpha");
                if (alphaPacket.Diagnostic is not null)
                {
                    return WzVideoSequenceDecodeResult.Fail(alphaPacket.Diagnostic);
                }

                var alphaResult = alphaDecoder!.Decode(
                    alphaPacket.Packet,
                    alphaPacket: null,
                    header.Width,
                    header.Height,
                    decodeOptions);
                if (!alphaResult.Succeeded)
                {
                    return WzVideoSequenceDecodeResult.Fail(WzVideoDecodeDiagnostic.FrameDecodeFailed(frame.Index, alphaResult.Diagnostic!));
                }

                if (alphaResult.NoDisplayFrame)
                {
                    continue;
                }

                var merged = WzVideoFrameComposer.MergeBgraWithBgraAlpha(
                    rawFrame,
                    alphaResult.Frame!,
                    options.OutputFormat);
                if (!merged.Succeeded)
                {
                    return WzVideoSequenceDecodeResult.Fail(merged.Diagnostic!);
                }

                rawFrame = merged.Frame!;
            }
            else if (rawFrame.PixelFormat != options.OutputFormat)
            {
                var converted = WzVideoFrameComposer.ConvertResult(rawFrame, options.OutputFormat);
                if (!converted.Succeeded)
                {
                    return WzVideoSequenceDecodeResult.Fail(converted.Diagnostic!);
                }

                rawFrame = converted.Frame!;
            }

            frames.Add(new WzDecodedVideoFrame(
                rawFrame.Width,
                rawFrame.Height,
                rawFrame.PixelFormat,
                rawFrame.Pixels,
                rawFrame.Stride,
                frame.Index,
                frame.StartTimeInNanoseconds,
                frame.DelayInNanoseconds));
        }

        return WzVideoSequenceDecodeResult.Success(new WzDecodedVideoSequence(
            header.Width,
            header.Height,
            options.OutputFormat,
            frames));
    }

    private static IWzRawVideoPacketDecoder CreateDefaultDecoder(string fourCc)
    {
        return fourCc switch
        {
            "VP90" => new Vp9RawVideoPacketDecoder(),
            "VP80" => new Vp8RawVideoPacketDecoder(),
            _ => throw new ArgumentOutOfRangeException(nameof(fourCc), fourCc, "Unsupported video codec.")
        };
    }
}
