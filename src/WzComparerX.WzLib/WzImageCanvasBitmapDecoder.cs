namespace WzComparerX.WzLib;

public sealed class WzImageCanvasBitmapDecoder
{
    public WzImageCanvasBgraBitmap DecodeToBgra8888(
        Stream stream,
        WzImageCanvasInspection canvas,
        string? path = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(canvas);

        ValidateCanvas(canvas, path);

        WzImageCanvasBitmap bitmap;
        try
        {
            bitmap = new WzImageCanvasPayloadDecoder().Decode(stream, canvas);
        }
        catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException or NotSupportedException)
        {
            throw WzImageCanvasBitmapDecodeException.DecodeFailed(path, ex);
        }

        return ConvertToBgra8888(bitmap, path);
    }

    private static void ValidateCanvas(WzImageCanvasInspection canvas, string? path)
    {
        if (canvas.CompressionKind != WzImageCanvasCompressionKind.Zlib)
        {
            throw WzImageCanvasBitmapDecodeException.CompressionUnsupported(canvas.CompressionKind, path);
        }

        if (canvas.Format is not 1 and not 2 and not 257 and not 513 and not 769 and not 1026 and not 2050 and not 2304 and not 2562 and not 4097 and not 4098 and not 4100)
        {
            throw WzImageCanvasBitmapDecodeException.FormatUnsupported(canvas.Format, path);
        }

        if (canvas.ActualScale != 1 && canvas is not { Format: 513, ActualScale: 16 })
        {
            throw WzImageCanvasBitmapDecodeException.ScaleUnsupported(canvas.Scale, path);
        }
    }

    private static WzImageCanvasBgraBitmap ConvertToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        if (bitmap.Format == 4098)
        {
            var decoded = WzImageCanvasBc7Decoder.ConvertToBgra8888(bitmap, path);
            return new WzImageCanvasBgraBitmap(decoded.Width, decoded.Height, bitmap.Format, "bgra8888", decoded.Pixels);
        }

        var pixels = bitmap.Format switch
        {
            1 => ConvertBgra4444ToBgra8888(bitmap, path),
            257 => ConvertBgra1555ToBgra8888(bitmap, path),
            513 => ConvertBgr565ToBgra8888(bitmap, path),
            769 => ConvertR16ToBgra8888(bitmap, path),
            1026 => ConvertDxt3ToBgra8888(bitmap, path),
            2050 => ConvertDxt5ToBgra8888(bitmap, path),
            2304 => ConvertA8ToBgra8888(bitmap, path),
            2562 => ConvertRgba1010102ToBgra8888(bitmap, path),
            4097 => ConvertDxt1ToBgra8888(bitmap, path),
            4100 => ConvertRgba32FloatToBgra8888(bitmap, path),
            2 => TrimOrCopy(bitmap.Pixels, checked(bitmap.Width * bitmap.Height * 4), path),
            _ => throw WzImageCanvasBitmapDecodeException.FormatUnsupported(bitmap.Format, path)
        };

        return new WzImageCanvasBgraBitmap(bitmap.Width, bitmap.Height, bitmap.Format, "bgra8888", pixels);
    }

    private static byte[] ConvertBgra4444ToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var source = TrimOrCopy(bitmap.Pixels, checked(bitmap.Width * bitmap.Height * 2), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        for (var i = 0; i < source.Length; i++)
        {
            var value = source[i];
            var low = value & 0x0f;
            var high = value & 0xf0;
            destination[i * 2] = (byte)(low | (low << 4));
            destination[(i * 2) + 1] = (byte)(high | (high >> 4));
        }

        return destination;
    }

    private static byte[] ConvertDxt3ToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var blocksWide = (bitmap.Width + 3) / 4;
        var blocksHigh = (bitmap.Height + 3) / 4;
        var source = TrimOrCopy(bitmap.Pixels, checked(blocksWide * blocksHigh * 16), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        Span<byte> alphaTable = stackalloc byte[16];
        Span<byte> colorTable = stackalloc byte[16];

        for (var blockY = 0; blockY < blocksHigh; blockY++)
        {
            for (var blockX = 0; blockX < blocksWide; blockX++)
            {
                var block = source.AsSpan(((blockY * blocksWide) + blockX) * 16, 16);
                BuildDxt3AlphaTable(block[..8], alphaTable);
                BuildDxtColorTable(block, colorTable);
                var colorBits = ReadUInt32LittleEndian(block[12..16]);

                for (var y = 0; y < 4; y++)
                {
                    var destinationY = (blockY * 4) + y;
                    if (destinationY >= bitmap.Height)
                    {
                        continue;
                    }

                    for (var x = 0; x < 4; x++)
                    {
                        var destinationX = (blockX * 4) + x;
                        if (destinationX >= bitmap.Width)
                        {
                            continue;
                        }

                        var blockPixel = (y * 4) + x;
                        var colorIndex = (int)((colorBits >> (blockPixel * 2)) & 0x03);
                        var colorOffset = colorIndex * 4;
                        var destinationOffset = ((destinationY * bitmap.Width) + destinationX) * 4;
                        destination[destinationOffset] = colorTable[colorOffset];
                        destination[destinationOffset + 1] = colorTable[colorOffset + 1];
                        destination[destinationOffset + 2] = colorTable[colorOffset + 2];
                        destination[destinationOffset + 3] = alphaTable[blockPixel];
                    }
                }
            }
        }

        return destination;
    }

    private static byte[] ConvertDxt1ToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var blocksWide = (bitmap.Width + 3) / 4;
        var blocksHigh = (bitmap.Height + 3) / 4;
        var source = TrimOrCopy(bitmap.Pixels, checked(blocksWide * blocksHigh * 8), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        Span<byte> colorTable = stackalloc byte[16];

        for (var blockY = 0; blockY < blocksHigh; blockY++)
        {
            for (var blockX = 0; blockX < blocksWide; blockX++)
            {
                var block = source.AsSpan(((blockY * blocksWide) + blockX) * 8, 8);
                BuildDxt1ColorTable(block, colorTable);
                var colorBits = ReadUInt32LittleEndian(block[4..8]);

                for (var y = 0; y < 4; y++)
                {
                    var destinationY = (blockY * 4) + y;
                    if (destinationY >= bitmap.Height)
                    {
                        continue;
                    }

                    for (var x = 0; x < 4; x++)
                    {
                        var destinationX = (blockX * 4) + x;
                        if (destinationX >= bitmap.Width)
                        {
                            continue;
                        }

                        var blockPixel = (y * 4) + x;
                        var colorIndex = (int)((colorBits >> (blockPixel * 2)) & 0x03);
                        var colorOffset = colorIndex * 4;
                        var destinationOffset = ((destinationY * bitmap.Width) + destinationX) * 4;
                        destination[destinationOffset] = colorTable[colorOffset];
                        destination[destinationOffset + 1] = colorTable[colorOffset + 1];
                        destination[destinationOffset + 2] = colorTable[colorOffset + 2];
                        destination[destinationOffset + 3] = colorTable[colorOffset + 3];
                    }
                }
            }
        }

        return destination;
    }

    private static byte[] ConvertDxt5ToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var blocksWide = (bitmap.Width + 3) / 4;
        var blocksHigh = (bitmap.Height + 3) / 4;
        var source = TrimOrCopy(bitmap.Pixels, checked(blocksWide * blocksHigh * 16), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        Span<byte> alphaTable = stackalloc byte[8];
        Span<byte> colorTable = stackalloc byte[16];

        for (var blockY = 0; blockY < blocksHigh; blockY++)
        {
            for (var blockX = 0; blockX < blocksWide; blockX++)
            {
                var block = source.AsSpan(((blockY * blocksWide) + blockX) * 16, 16);
                BuildDxt5AlphaTable(block[0], block[1], alphaTable);
                BuildDxtColorTable(block, colorTable);
                var alphaBits = ReadUInt48LittleEndian(block[2..8]);
                var colorBits = ReadUInt32LittleEndian(block[12..16]);

                for (var y = 0; y < 4; y++)
                {
                    var destinationY = (blockY * 4) + y;
                    if (destinationY >= bitmap.Height)
                    {
                        continue;
                    }

                    for (var x = 0; x < 4; x++)
                    {
                        var destinationX = (blockX * 4) + x;
                        if (destinationX >= bitmap.Width)
                        {
                            continue;
                        }

                        var blockPixel = (y * 4) + x;
                        var alphaIndex = (int)((alphaBits >> (blockPixel * 3)) & 0x07);
                        var colorIndex = (int)((colorBits >> (blockPixel * 2)) & 0x03);
                        var colorOffset = colorIndex * 4;
                        var destinationOffset = ((destinationY * bitmap.Width) + destinationX) * 4;
                        destination[destinationOffset] = colorTable[colorOffset];
                        destination[destinationOffset + 1] = colorTable[colorOffset + 1];
                        destination[destinationOffset + 2] = colorTable[colorOffset + 2];
                        destination[destinationOffset + 3] = alphaTable[alphaIndex];
                    }
                }
            }
        }

        return destination;
    }

    private static void BuildDxt3AlphaTable(ReadOnlySpan<byte> block, Span<byte> alphaTable)
    {
        for (var i = 0; i < 16; i += 2)
        {
            var value = block[i / 2];
            var low = value & 0x0f;
            var high = value >> 4;
            alphaTable[i] = (byte)(low | (low << 4));
            alphaTable[i + 1] = (byte)(high | (high << 4));
        }
    }

    private static void BuildDxt5AlphaTable(byte alpha0, byte alpha1, Span<byte> alphaTable)
    {
        alphaTable[0] = alpha0;
        alphaTable[1] = alpha1;
        if (alpha0 > alpha1)
        {
            for (var i = 2; i < 8; i++)
            {
                alphaTable[i] = (byte)((((8 - i) * alpha0) + ((i - 1) * alpha1) + 3) / 7);
            }
        }
        else
        {
            for (var i = 2; i < 6; i++)
            {
                alphaTable[i] = (byte)((((6 - i) * alpha0) + ((i - 1) * alpha1) + 2) / 5);
            }

            alphaTable[6] = 0;
            alphaTable[7] = byte.MaxValue;
        }
    }

    private static void BuildDxtColorTable(ReadOnlySpan<byte> block, Span<byte> colorTable)
    {
        var color0 = ReadUInt16LittleEndian(block[8..10]);
        var color1 = ReadUInt16LittleEndian(block[10..12]);
        WriteRgb565AsBgra(color0, colorTable[0..4]);
        WriteRgb565AsBgra(color1, colorTable[4..8]);

        if (color0 > color1)
        {
            InterpolateBgra(colorTable[0..4], colorTable[4..8], colorTable[8..12], firstWeight: 2, secondWeight: 1, divisor: 3, bias: 1);
            InterpolateBgra(colorTable[0..4], colorTable[4..8], colorTable[12..16], firstWeight: 1, secondWeight: 2, divisor: 3, bias: 1);
        }
        else
        {
            InterpolateBgra(colorTable[0..4], colorTable[4..8], colorTable[8..12], firstWeight: 1, secondWeight: 1, divisor: 2, bias: 0);
            colorTable[12] = 0;
            colorTable[13] = 0;
            colorTable[14] = 0;
            colorTable[15] = byte.MaxValue;
        }
    }

    private static void BuildDxt1ColorTable(ReadOnlySpan<byte> block, Span<byte> colorTable)
    {
        var color0 = ReadUInt16LittleEndian(block[0..2]);
        var color1 = ReadUInt16LittleEndian(block[2..4]);
        WriteRgb565AsBgra(color0, colorTable[0..4]);
        WriteRgb565AsBgra(color1, colorTable[4..8]);

        if (color0 > color1)
        {
            InterpolateBgra(colorTable[0..4], colorTable[4..8], colorTable[8..12], firstWeight: 2, secondWeight: 1, divisor: 3, bias: 1);
            InterpolateBgra(colorTable[0..4], colorTable[4..8], colorTable[12..16], firstWeight: 1, secondWeight: 2, divisor: 3, bias: 1);
        }
        else
        {
            InterpolateBgra(colorTable[0..4], colorTable[4..8], colorTable[8..12], firstWeight: 1, secondWeight: 1, divisor: 2, bias: 0);
            colorTable[12] = 0;
            colorTable[13] = 0;
            colorTable[14] = 0;
            colorTable[15] = 0;
        }
    }

    private static void WriteRgb565AsBgra(ushort value, Span<byte> destination)
    {
        destination[0] = Expand5To8(value & 0x1f);
        destination[1] = Expand6To8((value >> 5) & 0x3f);
        destination[2] = Expand5To8((value >> 11) & 0x1f);
        destination[3] = byte.MaxValue;
    }

    private static void InterpolateBgra(
        ReadOnlySpan<byte> first,
        ReadOnlySpan<byte> second,
        Span<byte> destination,
        int firstWeight,
        int secondWeight,
        int divisor,
        int bias)
    {
        for (var i = 0; i < 3; i++)
        {
            destination[i] = (byte)(((first[i] * firstWeight) + (second[i] * secondWeight) + bias) / divisor);
        }

        destination[3] = byte.MaxValue;
    }

    private static byte[] ConvertRgba1010102ToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var source = TrimOrCopy(bitmap.Pixels, checked(bitmap.Width * bitmap.Height * 4), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        for (var sourceIndex = 0; sourceIndex < source.Length; sourceIndex += 4)
        {
            var value = ReadUInt32LittleEndian(source.AsSpan(sourceIndex, 4));
            var destinationIndex = sourceIndex;
            destination[destinationIndex] = (byte)(((value >> 20) & 0x3ff) >> 2);
            destination[destinationIndex + 1] = (byte)(((value >> 10) & 0x3ff) >> 2);
            destination[destinationIndex + 2] = (byte)((value & 0x3ff) >> 2);
            destination[destinationIndex + 3] = (byte)(((value >> 30) & 0x03) * 85);
        }

        return destination;
    }

    private static byte[] ConvertR16ToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var source = TrimOrCopy(bitmap.Pixels, checked(bitmap.Width * bitmap.Height * 2), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        for (var sourceIndex = 0; sourceIndex < source.Length; sourceIndex += 2)
        {
            var value = ReadUInt16LittleEndian(source.AsSpan(sourceIndex, 2));
            var destinationIndex = (sourceIndex / 2) * 4;
            destination[destinationIndex] = 0;
            destination[destinationIndex + 1] = 0;
            destination[destinationIndex + 2] = Expand16To8(value);
            destination[destinationIndex + 3] = byte.MaxValue;
        }

        return destination;
    }

    private static byte[] ConvertA8ToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var source = TrimOrCopy(bitmap.Pixels, checked(bitmap.Width * bitmap.Height), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        for (var sourceIndex = 0; sourceIndex < source.Length; sourceIndex++)
        {
            var destinationIndex = sourceIndex * 4;
            destination[destinationIndex] = byte.MaxValue;
            destination[destinationIndex + 1] = byte.MaxValue;
            destination[destinationIndex + 2] = byte.MaxValue;
            destination[destinationIndex + 3] = source[sourceIndex];
        }

        return destination;
    }

    private static byte[] ConvertRgba32FloatToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var source = TrimOrCopy(bitmap.Pixels, checked(bitmap.Width * bitmap.Height * 16), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        for (var sourceIndex = 0; sourceIndex < source.Length; sourceIndex += 16)
        {
            var r = ReadSingleLittleEndian(source.AsSpan(sourceIndex, 4));
            var g = ReadSingleLittleEndian(source.AsSpan(sourceIndex + 4, 4));
            var b = ReadSingleLittleEndian(source.AsSpan(sourceIndex + 8, 4));
            var a = ReadSingleLittleEndian(source.AsSpan(sourceIndex + 12, 4));
            var destinationIndex = (sourceIndex / 16) * 4;
            destination[destinationIndex] = FloatToByte(b);
            destination[destinationIndex + 1] = FloatToByte(g);
            destination[destinationIndex + 2] = FloatToByte(r);
            destination[destinationIndex + 3] = FloatToByte(a);
        }

        return destination;
    }

    private static byte[] ConvertBgra1555ToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var source = TrimOrCopy(bitmap.Pixels, checked(bitmap.Width * bitmap.Height * 2), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        for (var sourceIndex = 0; sourceIndex < source.Length; sourceIndex += 2)
        {
            var value = source[sourceIndex] | (source[sourceIndex + 1] << 8);
            var destinationIndex = (sourceIndex / 2) * 4;
            destination[destinationIndex] = Expand5To8(value & 0x1f);
            destination[destinationIndex + 1] = Expand5To8((value >> 5) & 0x1f);
            destination[destinationIndex + 2] = Expand5To8((value >> 10) & 0x1f);
            destination[destinationIndex + 3] = (value & 0x8000) != 0 ? byte.MaxValue : (byte)0;
        }

        return destination;
    }

    private static byte[] ConvertBgr565ToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var actualScale = GetActualScale(bitmap.Scale);
        if (actualScale == 16)
        {
            return ConvertScaledBgr565ToBgra8888(bitmap, path, actualScale);
        }

        var source = TrimOrCopy(bitmap.Pixels, checked(bitmap.Width * bitmap.Height * 2), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        for (var sourceIndex = 0; sourceIndex < source.Length; sourceIndex += 2)
        {
            var value = source[sourceIndex] | (source[sourceIndex + 1] << 8);
            var destinationIndex = (sourceIndex / 2) * 4;
            destination[destinationIndex] = Expand5To8(value & 0x1f);
            destination[destinationIndex + 1] = Expand6To8((value >> 5) & 0x3f);
            destination[destinationIndex + 2] = Expand5To8((value >> 11) & 0x1f);
            destination[destinationIndex + 3] = byte.MaxValue;
        }

        return destination;
    }

    private static byte[] ConvertScaledBgr565ToBgra8888(WzImageCanvasBitmap bitmap, string? path, int actualScale)
    {
        if (bitmap.Width % actualScale != 0 || bitmap.Height % actualScale != 0)
        {
            throw WzImageCanvasBitmapDecodeException.DecodeFailed(path);
        }

        var sourceWidth = bitmap.Width / actualScale;
        var sourceHeight = bitmap.Height / actualScale;
        var source = TrimOrCopy(bitmap.Pixels, checked(sourceWidth * sourceHeight * 2), path);
        var destination = new byte[checked(bitmap.Width * bitmap.Height * 4)];

        for (var sourceY = 0; sourceY < sourceHeight; sourceY++)
        {
            for (var sourceX = 0; sourceX < sourceWidth; sourceX++)
            {
                var sourceIndex = ((sourceY * sourceWidth) + sourceX) * 2;
                var value = source[sourceIndex] | (source[sourceIndex + 1] << 8);
                var b = Expand5To8(value & 0x1f);
                var g = Expand6To8((value >> 5) & 0x3f);
                var r = Expand5To8((value >> 11) & 0x1f);
                const byte a = byte.MaxValue;

                var destinationX = sourceX * actualScale;
                var destinationY = sourceY * actualScale;
                for (var scaledY = 0; scaledY < actualScale; scaledY++)
                {
                    for (var scaledX = 0; scaledX < actualScale; scaledX++)
                    {
                        var destinationIndex = (((destinationY + scaledY) * bitmap.Width) + destinationX + scaledX) * 4;
                        destination[destinationIndex] = b;
                        destination[destinationIndex + 1] = g;
                        destination[destinationIndex + 2] = r;
                        destination[destinationIndex + 3] = a;
                    }
                }
            }
        }

        return destination;
    }

    private static int GetActualScale(int scale)
    {
        return scale > 0 ? 1 << scale : 1;
    }

    private static byte Expand5To8(int value)
    {
        return (byte)((value << 3) | (value >> 2));
    }

    private static byte Expand6To8(int value)
    {
        return (byte)((value << 2) | (value >> 4));
    }

    private static byte Expand16To8(ushort value)
    {
        return (byte)(((value * 255) + 32767) / 65535);
    }

    private static ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> bytes)
    {
        return (ushort)(bytes[0] | (bytes[1] << 8));
    }

    private static uint ReadUInt32LittleEndian(ReadOnlySpan<byte> bytes)
    {
        return (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
    }

    private static float ReadSingleLittleEndian(ReadOnlySpan<byte> bytes)
    {
        return BitConverter.Int32BitsToSingle((int)ReadUInt32LittleEndian(bytes));
    }

    private static byte FloatToByte(float value)
    {
        if (float.IsNaN(value) || value <= 0)
        {
            return 0;
        }

        if (value >= 1)
        {
            return byte.MaxValue;
        }

        return (byte)((value * byte.MaxValue) + 0.5f);
    }

    private static ulong ReadUInt48LittleEndian(ReadOnlySpan<byte> bytes)
    {
        ulong value = 0;
        for (var i = 0; i < 6; i++)
        {
            value |= (ulong)bytes[i] << (i * 8);
        }

        return value;
    }

    private static byte[] TrimOrCopy(byte[] source, int expectedLength, string? path)
    {
        if (source.Length < expectedLength)
        {
            throw WzImageCanvasBitmapDecodeException.DecodeFailed(path);
        }

        return source.Length == expectedLength ? source : source[..expectedLength];
    }
}
