namespace WzComparerX.Rendering;

internal static class WzVideoFrameComposer
{
    public static WzRawVideoPacketDecodeResult MergeBgraWithBgraAlpha(
        WzRawDecodedVideoFrame colorFrame,
        WzRawDecodedVideoFrame alphaFrame,
        WzVideoPixelFormat outputFormat)
    {
        if (colorFrame.Width != alphaFrame.Width || colorFrame.Height != alphaFrame.Height)
        {
            return WzRawVideoPacketDecodeResult.Fail(WzVideoDecodeDiagnostic.AlphaDimensionMismatch(
                colorFrame.Width,
                colorFrame.Height,
                alphaFrame.Width,
                alphaFrame.Height));
        }

        if (colorFrame.PixelFormat != WzVideoPixelFormat.Bgra8888 ||
            alphaFrame.PixelFormat != WzVideoPixelFormat.Bgra8888)
        {
            return WzRawVideoPacketDecodeResult.Fail(WzVideoDecodeDiagnostic.AlphaPixelFormatUnsupported(
                colorFrame.PixelFormat,
                alphaFrame.PixelFormat));
        }

        if (!IsPackedFrameBufferValid(colorFrame) || !IsPackedFrameBufferValid(alphaFrame))
        {
            return WzRawVideoPacketDecodeResult.Fail(WzVideoDecodeDiagnostic.InvalidMetadata("Video frame pixel buffer is smaller than the declared dimensions and stride."));
        }

        var pixels = colorFrame.Pixels.ToArray();
        for (var row = 0; row < colorFrame.Height; row++)
        {
            var colorRowOffset = row * colorFrame.Stride;
            var alphaRowOffset = row * alphaFrame.Stride;
            for (var column = 0; column < colorFrame.Width; column++)
            {
                pixels[colorRowOffset + column * 4 + 3] = alphaFrame.Pixels[alphaRowOffset + column * 4 + 2];
            }
        }

        var merged = new WzRawDecodedVideoFrame(
            colorFrame.Width,
            colorFrame.Height,
            WzVideoPixelFormat.Bgra8888,
            pixels,
            colorFrame.Stride);

        return ConvertResult(merged, outputFormat);
    }

    public static WzRawVideoPacketDecodeResult ConvertResult(WzRawDecodedVideoFrame frame, WzVideoPixelFormat outputFormat)
    {
        if (frame.PixelFormat == outputFormat)
        {
            return WzRawVideoPacketDecodeResult.Success(frame);
        }

        if (!Enum.IsDefined(outputFormat))
        {
            return WzRawVideoPacketDecodeResult.Fail(
                WzVideoDecodeDiagnostic.InvalidOptions($"Unsupported WCX video output pixel format value {outputFormat}."));
        }

        if (frame.PixelFormat is not (WzVideoPixelFormat.Bgra8888 or WzVideoPixelFormat.Rgba8888))
        {
            return WzRawVideoPacketDecodeResult.Fail(
                WzVideoDecodeDiagnostic.InvalidMetadata($"Unsupported WCX video source pixel format: {frame.PixelFormat}."));
        }

        if (!IsPackedFrameBufferValid(frame))
        {
            return WzRawVideoPacketDecodeResult.Fail(
                WzVideoDecodeDiagnostic.InvalidMetadata("Video frame pixel buffer is smaller than the declared dimensions and stride."));
        }

        var convertedStrideLong = (long)frame.Width * 4;
        var convertedLengthLong = convertedStrideLong * frame.Height;
        if (convertedStrideLong > int.MaxValue || convertedLengthLong > int.MaxValue)
        {
            return WzRawVideoPacketDecodeResult.Fail(
                WzVideoDecodeDiagnostic.InvalidMetadata("Video frame pixel buffer is too large to convert to a packed output buffer."));
        }

        var convertedStride = (int)convertedStrideLong;
        var converted = new byte[(int)convertedLengthLong];
        for (var row = 0; row < frame.Height; row++)
        {
            var sourceRowOffset = row * frame.Stride;
            var targetRowOffset = row * convertedStride;
            for (var column = 0; column < frame.Width; column++)
            {
                var sourceOffset = sourceRowOffset + column * 4;
                var targetOffset = targetRowOffset + column * 4;
                converted[targetOffset] = frame.Pixels[sourceOffset + 2];
                converted[targetOffset + 1] = frame.Pixels[sourceOffset + 1];
                converted[targetOffset + 2] = frame.Pixels[sourceOffset];
                converted[targetOffset + 3] = frame.Pixels[sourceOffset + 3];
            }
        }

        return WzRawVideoPacketDecodeResult.Success(
            new WzRawDecodedVideoFrame(
                frame.Width,
                frame.Height,
                outputFormat,
                converted,
                convertedStride));
    }

    private static bool IsPackedFrameBufferValid(WzRawDecodedVideoFrame frame)
    {
        if (frame.Width < 0 || frame.Height < 0 || frame.Stride < 0)
        {
            return false;
        }

        var minimumStride = (long)frame.Width * 4;
        var minimumLength = (long)frame.Stride * frame.Height;
        return frame.Width >= 0 &&
            frame.Height >= 0 &&
            frame.Stride >= minimumStride &&
            frame.Pixels.LongLength >= minimumLength;
    }
}
