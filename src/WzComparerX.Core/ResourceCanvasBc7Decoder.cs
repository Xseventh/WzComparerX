using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WzComparerX.WzLib;

#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace WzComparerX.Core;

internal static class ResourceCanvasBc7Decoder
{
    // BC7 block unpacking follows WC's port of Rich Geldreich's MIT-licensed bc7decomp.
    public static (int Width, int Height, byte[] Pixels) ConvertToBgra8888(WzImageCanvasBitmap bitmap, string? path)
    {
        var decodedWidth = bitmap.Width & ~3;
        var decodedHeight = bitmap.Height & ~3;
        if (decodedWidth <= 0 || decodedHeight <= 0)
        {
            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewDecodeFailed(path));
        }

        var expectedLength = checked(bitmap.Width * decodedHeight);
        if (bitmap.Pixels.Length < expectedLength)
        {
            throw new ResourceCanvasImageException(ResourceInspectionDiagnostics.CanvasPreviewDecodeFailed(path));
        }

        var source = bitmap.Pixels.AsSpan(0, expectedLength);
        var destination = new byte[checked(decodedWidth * decodedHeight * 4)];
        Span<BC7Decomp.color_rgba> rgba = stackalloc BC7Decomp.color_rgba[16];
        Span<byte> rgbaBytes = MemoryMarshal.AsBytes(rgba);
        var sourceStride = checked(bitmap.Width * 4);

        for (var y = 0; y < decodedHeight; y += 4)
        {
            var dataRow = source.Slice((y / 4) * sourceStride, sourceStride);
            for (var x = 0; x < decodedWidth; x += 4)
            {
                BC7Decomp.unpack_bc7(dataRow, rgba);
                CopyRgbaBlockToBgra(rgbaBytes, destination, decodedWidth, x, y);
                dataRow = dataRow[16..];
            }
        }

        return (decodedWidth, decodedHeight, destination);
    }

    private static void CopyRgbaBlockToBgra(ReadOnlySpan<byte> rgba, Span<byte> destination, int width, int x, int y)
    {
        for (var row = 0; row < 4; row++)
        {
            for (var column = 0; column < 4; column++)
            {
                var sourceOffset = ((row * 4) + column) * 4;
                var destinationOffset = (((y + row) * width) + x + column) * 4;
                destination[destinationOffset] = rgba[sourceOffset + 2];
                destination[destinationOffset + 1] = rgba[sourceOffset + 1];
                destination[destinationOffset + 2] = rgba[sourceOffset];
                destination[destinationOffset + 3] = rgba[sourceOffset + 3];
            }
        }
    }

internal static class BC7Decomp
{
    public struct color_rgba
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public ref byte this[int index]
        {
            get => ref this.AsBytes()[index];
        }

        private Span<byte> AsBytes()
        {
#if NET6_0_OR_GREATER
        var pThis = MemoryMarshal.CreateSpan(ref this, 1);
        return MemoryMarshal.Cast<color_rgba, byte>(pThis);
#else
            unsafe
            {
                fixed (color_rgba* p = &this)
                {
                    return new Span<byte>(p, sizeof(color_rgba));
                }
            }
#endif
        }

        public void set_noclamp_rgba(uint vr, uint vg, uint vb, uint va) => this.set(vr, vg, vb, va);
        private void set(uint vr, uint vg, uint vb, uint va) { r = (byte)vr; g = (byte)vg; b = (byte)vb; a = (byte)va; }
    }

#if NET6_0_OR_GREATER
    static readonly Vector128<short>[] g_bc7_weights4_sse2 = {
        Vector128.Create(0, 0, 0, 0, 4, 4, 4, 4),
        Vector128.Create(9, 9, 9, 9, 13, 13, 13, 13),
        Vector128.Create(17, 17, 17, 17, 21, 21, 21, 21),
        Vector128.Create(26, 26, 26, 26, 30, 30, 30, 30),
        Vector128.Create(34, 34, 34, 34, 38, 38, 38, 38),
        Vector128.Create(43, 43, 43, 43, 47, 47, 47, 47),
        Vector128.Create(51, 51, 51, 51, 55, 55, 55, 55),
        Vector128.Create(60, 60, 60, 60, 64, 64, 64, 64),
    };
#endif

    static readonly uint[] g_bc7_weights2 = { 0, 21, 43, 64 };
    static readonly uint[] g_bc7_weights3 = { 0, 9, 18, 27, 37, 46, 55, 64 };
    static readonly uint[] g_bc7_weights4 = { 0, 4, 9, 13, 17, 21, 26, 30, 34, 38, 43, 47, 51, 55, 60, 64 };

    static readonly byte[] g_bc7_partition2 =
    {
        0,0,1,1,0,0,1,1,0,0,1,1,0,0,1,1,        0,0,0,1,0,0,0,1,0,0,0,1,0,0,0,1,        0,1,1,1,0,1,1,1,0,1,1,1,0,1,1,1,        0,0,0,1,0,0,1,1,0,0,1,1,0,1,1,1,        0,0,0,0,0,0,0,1,0,0,0,1,0,0,1,1,        0,0,1,1,0,1,1,1,0,1,1,1,1,1,1,1,        0,0,0,1,0,0,1,1,0,1,1,1,1,1,1,1,        0,0,0,0,0,0,0,1,0,0,1,1,0,1,1,1,
        0,0,0,0,0,0,0,0,0,0,0,1,0,0,1,1,        0,0,1,1,0,1,1,1,1,1,1,1,1,1,1,1,        0,0,0,0,0,0,0,1,0,1,1,1,1,1,1,1,        0,0,0,0,0,0,0,0,0,0,0,1,0,1,1,1,        0,0,0,1,0,1,1,1,1,1,1,1,1,1,1,1,        0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,        0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,        0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,
        0,0,0,0,1,0,0,0,1,1,1,0,1,1,1,1,        0,1,1,1,0,0,0,1,0,0,0,0,0,0,0,0,        0,0,0,0,0,0,0,0,1,0,0,0,1,1,1,0,        0,1,1,1,0,0,1,1,0,0,0,1,0,0,0,0,        0,0,1,1,0,0,0,1,0,0,0,0,0,0,0,0,        0,0,0,0,1,0,0,0,1,1,0,0,1,1,1,0,        0,0,0,0,0,0,0,0,1,0,0,0,1,1,0,0,        0,1,1,1,0,0,1,1,0,0,1,1,0,0,0,1,
        0,0,1,1,0,0,0,1,0,0,0,1,0,0,0,0,        0,0,0,0,1,0,0,0,1,0,0,0,1,1,0,0,        0,1,1,0,0,1,1,0,0,1,1,0,0,1,1,0,        0,0,1,1,0,1,1,0,0,1,1,0,1,1,0,0,        0,0,0,1,0,1,1,1,1,1,1,0,1,0,0,0,        0,0,0,0,1,1,1,1,1,1,1,1,0,0,0,0,        0,1,1,1,0,0,0,1,1,0,0,0,1,1,1,0,        0,0,1,1,1,0,0,1,1,0,0,1,1,1,0,0,
        0,1,0,1,0,1,0,1,0,1,0,1,0,1,0,1,        0,0,0,0,1,1,1,1,0,0,0,0,1,1,1,1,        0,1,0,1,1,0,1,0,0,1,0,1,1,0,1,0,        0,0,1,1,0,0,1,1,1,1,0,0,1,1,0,0,        0,0,1,1,1,1,0,0,0,0,1,1,1,1,0,0,        0,1,0,1,0,1,0,1,1,0,1,0,1,0,1,0,        0,1,1,0,1,0,0,1,0,1,1,0,1,0,0,1,        0,1,0,1,1,0,1,0,1,0,1,0,0,1,0,1,
        0,1,1,1,0,0,1,1,1,1,0,0,1,1,1,0,        0,0,0,1,0,0,1,1,1,1,0,0,1,0,0,0,        0,0,1,1,0,0,1,0,0,1,0,0,1,1,0,0,        0,0,1,1,1,0,1,1,1,1,0,1,1,1,0,0,        0,1,1,0,1,0,0,1,1,0,0,1,0,1,1,0,        0,0,1,1,1,1,0,0,1,1,0,0,0,0,1,1,        0,1,1,0,0,1,1,0,1,0,0,1,1,0,0,1,        0,0,0,0,0,1,1,0,0,1,1,0,0,0,0,0,
        0,1,0,0,1,1,1,0,0,1,0,0,0,0,0,0,        0,0,1,0,0,1,1,1,0,0,1,0,0,0,0,0,        0,0,0,0,0,0,1,0,0,1,1,1,0,0,1,0,        0,0,0,0,0,1,0,0,1,1,1,0,0,1,0,0,        0,1,1,0,1,1,0,0,1,0,0,1,0,0,1,1,        0,0,1,1,0,1,1,0,1,1,0,0,1,0,0,1,        0,1,1,0,0,0,1,1,1,0,0,1,1,1,0,0,        0,0,1,1,1,0,0,1,1,1,0,0,0,1,1,0,
        0,1,1,0,1,1,0,0,1,1,0,0,1,0,0,1,        0,1,1,0,0,0,1,1,0,0,1,1,1,0,0,1,        0,1,1,1,1,1,1,0,1,0,0,0,0,0,0,1,        0,0,0,1,1,0,0,0,1,1,1,0,0,1,1,1,        0,0,0,0,1,1,1,1,0,0,1,1,0,0,1,1,        0,0,1,1,0,0,1,1,1,1,1,1,0,0,0,0,        0,0,1,0,0,0,1,0,1,1,1,0,1,1,1,0,        0,1,0,0,0,1,0,0,0,1,1,1,0,1,1,1
    };

    static readonly byte[] g_bc7_partition3 =
    {
        0,0,1,1,0,0,1,1,0,2,2,1,2,2,2,2,        0,0,0,1,0,0,1,1,2,2,1,1,2,2,2,1,        0,0,0,0,2,0,0,1,2,2,1,1,2,2,1,1,        0,2,2,2,0,0,2,2,0,0,1,1,0,1,1,1,        0,0,0,0,0,0,0,0,1,1,2,2,1,1,2,2,        0,0,1,1,0,0,1,1,0,0,2,2,0,0,2,2,        0,0,2,2,0,0,2,2,1,1,1,1,1,1,1,1,        0,0,1,1,0,0,1,1,2,2,1,1,2,2,1,1,
        0,0,0,0,0,0,0,0,1,1,1,1,2,2,2,2,        0,0,0,0,1,1,1,1,1,1,1,1,2,2,2,2,        0,0,0,0,1,1,1,1,2,2,2,2,2,2,2,2,        0,0,1,2,0,0,1,2,0,0,1,2,0,0,1,2,        0,1,1,2,0,1,1,2,0,1,1,2,0,1,1,2,        0,1,2,2,0,1,2,2,0,1,2,2,0,1,2,2,        0,0,1,1,0,1,1,2,1,1,2,2,1,2,2,2,        0,0,1,1,2,0,0,1,2,2,0,0,2,2,2,0,
        0,0,0,1,0,0,1,1,0,1,1,2,1,1,2,2,        0,1,1,1,0,0,1,1,2,0,0,1,2,2,0,0,        0,0,0,0,1,1,2,2,1,1,2,2,1,1,2,2,        0,0,2,2,0,0,2,2,0,0,2,2,1,1,1,1,        0,1,1,1,0,1,1,1,0,2,2,2,0,2,2,2,        0,0,0,1,0,0,0,1,2,2,2,1,2,2,2,1,        0,0,0,0,0,0,1,1,0,1,2,2,0,1,2,2,        0,0,0,0,1,1,0,0,2,2,1,0,2,2,1,0,
        0,1,2,2,0,1,2,2,0,0,1,1,0,0,0,0,        0,0,1,2,0,0,1,2,1,1,2,2,2,2,2,2,        0,1,1,0,1,2,2,1,1,2,2,1,0,1,1,0,        0,0,0,0,0,1,1,0,1,2,2,1,1,2,2,1,        0,0,2,2,1,1,0,2,1,1,0,2,0,0,2,2,        0,1,1,0,0,1,1,0,2,0,0,2,2,2,2,2,        0,0,1,1,0,1,2,2,0,1,2,2,0,0,1,1,        0,0,0,0,2,0,0,0,2,2,1,1,2,2,2,1,
        0,0,0,0,0,0,0,2,1,1,2,2,1,2,2,2,        0,2,2,2,0,0,2,2,0,0,1,2,0,0,1,1,        0,0,1,1,0,0,1,2,0,0,2,2,0,2,2,2,        0,1,2,0,0,1,2,0,0,1,2,0,0,1,2,0,        0,0,0,0,1,1,1,1,2,2,2,2,0,0,0,0,        0,1,2,0,1,2,0,1,2,0,1,2,0,1,2,0,        0,1,2,0,2,0,1,2,1,2,0,1,0,1,2,0,        0,0,1,1,2,2,0,0,1,1,2,2,0,0,1,1,
        0,0,1,1,1,1,2,2,2,2,0,0,0,0,1,1,        0,1,0,1,0,1,0,1,2,2,2,2,2,2,2,2,        0,0,0,0,0,0,0,0,2,1,2,1,2,1,2,1,        0,0,2,2,1,1,2,2,0,0,2,2,1,1,2,2,        0,0,2,2,0,0,1,1,0,0,2,2,0,0,1,1,        0,2,2,0,1,2,2,1,0,2,2,0,1,2,2,1,        0,1,0,1,2,2,2,2,2,2,2,2,0,1,0,1,        0,0,0,0,2,1,2,1,2,1,2,1,2,1,2,1,
        0,1,0,1,0,1,0,1,0,1,0,1,2,2,2,2,        0,2,2,2,0,1,1,1,0,2,2,2,0,1,1,1,        0,0,0,2,1,1,1,2,0,0,0,2,1,1,1,2,        0,0,0,0,2,1,1,2,2,1,1,2,2,1,1,2,        0,2,2,2,0,1,1,1,0,1,1,1,0,2,2,2,        0,0,0,2,1,1,1,2,1,1,1,2,0,0,0,2,        0,1,1,0,0,1,1,0,0,1,1,0,2,2,2,2,        0,0,0,0,0,0,0,0,2,1,1,2,2,1,1,2,
        0,1,1,0,0,1,1,0,2,2,2,2,2,2,2,2,        0,0,2,2,0,0,1,1,0,0,1,1,0,0,2,2,        0,0,2,2,1,1,2,2,1,1,2,2,0,0,2,2,        0,0,0,0,0,0,0,0,0,0,0,0,2,1,1,2,        0,0,0,2,0,0,0,1,0,0,0,2,0,0,0,1,        0,2,2,2,1,2,2,2,0,2,2,2,1,2,2,2,        0,1,0,1,2,2,2,2,2,2,2,2,2,2,2,2,        0,1,1,1,2,0,1,1,2,2,0,1,2,2,2,0,
    };

    static readonly byte[] g_bc7_table_anchor_index_second_subset = 
    {
        15,15,15,15,15,15,15,15,
        15,15,15,15,15,15,15,15,
        15, 2, 8, 2, 2, 8, 8,15,
        2, 8, 2, 2, 8, 8, 2, 2,
        15,15, 6, 8, 2, 8,15,15,
        2, 8, 2, 2, 2,15,15, 6,
        6, 2, 6, 8,15,15, 2, 2,
        15,15,15,15,15, 2, 2,15
    };

    static readonly byte[] g_bc7_table_anchor_index_third_subset_1 =
    {
        3, 3,15,15, 8, 3,15,15,
        8, 8, 6, 6, 6, 5, 3, 3,
        3, 3, 8,15, 3, 3, 6,10,
        5, 8, 8, 6, 8, 5,15,15,
        8,15, 3, 5, 6,10, 8,15,
        15, 3,15, 5,15,15,15,15,
        3,15, 5, 5, 5, 8, 5,10,
        5,10, 8,13,15,12, 3, 3
    };

    static readonly byte[] g_bc7_table_anchor_index_third_subset_2 =
    {
        15, 8, 8, 3,15,15, 3, 8,
        15,15,15,15,15,15,15, 8,
        15, 8,15, 3,15, 8,15, 8,
        3,15, 6,10,15,15,10, 8,
        15, 3,15,10,10, 8, 9,10,
        6,15, 8,15, 3, 6, 6, 8,
        15, 3,15,15,15,15,15,15,
        15,15,15,15, 3,15,15, 8
    };

    static readonly byte[] g_bc7_first_byte_to_mode =
    {
        8, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        5, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        6, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        5, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        7, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        5, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        6, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        5, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
    };

    static void insert_weight_zero(ref ulong index_bits, int bits_per_index, int offset)
    {
        ulong LOW_BIT_MASK = (1UL << ((bits_per_index * (offset + 1)) - 1)) - 1;
        ulong HIGH_BIT_MASK = ~LOW_BIT_MASK;

        index_bits = ((index_bits & HIGH_BIT_MASK) << 1) | (index_bits & LOW_BIT_MASK);
    }

    static uint bc7_dequant(uint val, uint pbit, int val_bits)
    {
        Debug.Assert(val < (1U << val_bits));
        Debug.Assert(pbit < 2);
        Debug.Assert(val_bits >= 4 && val_bits <= 8);
        int total_bits = val_bits + 1;
        val = (val << 1) | pbit;
        val <<= (8 - total_bits);
        val |= (val >> total_bits);
        Debug.Assert(val <= 255);
        return val;
    }

    static uint bc7_dequant(uint val, int val_bits)
    {
        Debug.Assert(val < (1U << val_bits));
        Debug.Assert(val_bits >= 4 && val_bits <= 8);
        val <<= (8 - val_bits);
        val |= (val >> val_bits);
        Debug.Assert(val <= 255);
        return val;
    }

    static uint bc7_interp2(uint l, uint h, int w)
    {
        Debug.Assert(w < 4);
        return (l * (64 - g_bc7_weights2[w]) + h * g_bc7_weights2[w] + 32) >> 6;
    }
    static uint bc7_interp3(uint l, uint h, int w)
    {
        Debug.Assert(w < 8);
        return (l * (64 - g_bc7_weights3[w]) + h * g_bc7_weights3[w] + 32) >> 6;
    }
    static uint bc7_interp4(uint l, uint h, int w)
    {
        Debug.Assert(w < 16);
        return (l * (64 - g_bc7_weights4[w]) + h * g_bc7_weights4[w] + 32) >> 6;
    }
    static uint bc7_interp(uint l, uint h, int w, int bits)
    {
        Debug.Assert(l <= 255 && h <= 255);
        switch (bits)
        {
            case 2: return bc7_interp2(l, h, w);
            case 3: return bc7_interp3(l, h, w);
            case 4: return bc7_interp4(l, h, w);
            default:
                break;
        }
        return 0;
    }

#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Vector128<short> bc7_interp_sse2(Vector128<short> l, Vector128<short> h, Vector128<short> w, Vector128<short> iw)
    {
        return Sse2.ShiftRightLogical(Sse2.Add(Sse2.Add(Sse2.MultiplyLow(l, iw), Sse2.MultiplyLow(h, w)), Vector128.Create((short)32)), 6);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Vector256<short> bc7_interp_avx2(Vector256<short> l, Vector256<short> h, Vector256<short> w, Vector256<short> iw)
    {
        return Avx2.ShiftRightLogical(Avx2.Add(Avx2.Add(Avx2.MultiplyLow(l, iw), Avx2.MultiplyLow(h, w)), Vector256.Create((short)32)), 6);
    }

    static unsafe void bc7_interp2_sse2(ReadOnlySpan<color_rgba> endpoint_pair, Span<color_rgba> out_colors)
    {
        Vector128<byte> endpoints;
        fixed (color_rgba* pInput = endpoint_pair)
            endpoints = Sse2.LoadScalarVector128((long*)pInput).AsByte();
        Vector128<short> endpoints_16 = Sse2.UnpackLow(endpoints, new Vector128<byte>()).AsInt16();
        Vector128<short> endpoints_16_swapped = Sse2.Shuffle(endpoints_16.AsInt32(), 0b_01_00_11_10).AsInt16();

        // Interpolated colors will be color 1 and 2
        Vector128<short> interpolated_colors = bc7_interp_sse2(endpoints_16, endpoints_16_swapped, Vector128.Create((short)21), Vector128.Create((short)43));

        // all_colors will be 1, 2, 0, 3
        Vector128<byte> all_colors = Sse2.PackUnsignedSaturate(interpolated_colors, endpoints_16);

        all_colors = Sse2.Shuffle(all_colors.AsInt32(), 0b_11_01_00_10).AsByte();

        fixed (color_rgba* pOutput = out_colors)
            Sse2.Store((byte*)pOutput, all_colors);
    }

    static unsafe void bc7_interp3_sse2(ReadOnlySpan<color_rgba> endpoint_pair, Span<color_rgba> out_colors)
    {
        Vector128<byte> endpoints;
        fixed (color_rgba* pInput = endpoint_pair)
            endpoints = Sse2.LoadScalarVector128((long*)pInput).AsByte();
        Vector128<short> endpoints_16bit = Sse2.UnpackLow(endpoints, new Vector128<byte>()).AsInt16();
        Vector128<short> endpoints_16bit_swapped = Sse2.Shuffle(endpoints_16bit.AsInt32(), 0b_01_00_11_10).AsInt16();

        Vector128<short> interpolated_16 = bc7_interp_sse2(endpoints_16bit, endpoints_16bit_swapped, Vector128.Create((short)9), Vector128.Create((short)55));
        Vector128<short> interpolated_23 = bc7_interp_sse2(endpoints_16bit, endpoints_16bit_swapped, Vector128.Create(18, 18, 18, 18, 37, 37, 37, 37), Vector128.Create(46, 46, 46, 46, 27, 27, 27, 27));
        Vector128<short> interpolated_45 = bc7_interp_sse2(endpoints_16bit, endpoints_16bit_swapped, Vector128.Create(37, 37, 37, 37, 18, 18, 18, 18), Vector128.Create(27, 27, 27, 27, 46, 46, 46, 46));

        Vector128<short> interpolated_01 = Sse2.UnpackLow(endpoints_16bit.AsInt64(), interpolated_16.AsInt64()).AsInt16();
        Vector128<short> interpolated_67 = Sse2.UnpackHigh(interpolated_16.AsInt64(), endpoints_16bit.AsInt64()).AsInt16();

        Vector128<byte> all_colors_0 = Sse2.PackUnsignedSaturate(interpolated_01, interpolated_23);
        Vector128<byte> all_colors_1 = Sse2.PackUnsignedSaturate(interpolated_45, interpolated_67);

        fixed (color_rgba* pOutput = out_colors)
        {
            Sse2.Store((byte*)pOutput, all_colors_0);
            Sse2.Store((byte*)(pOutput + 4), all_colors_1);
        }
    }

    static unsafe void bc7_interp3_avx2(ReadOnlySpan<color_rgba> endpoint_pair, Span<color_rgba> out_colors)
    {
        Vector128<byte> endpoints;
        fixed (color_rgba* pInput = endpoint_pair)
            endpoints = Sse2.LoadScalarVector128((long*)pInput).AsByte();
        Vector128<short> endpoints_16bit = Sse2.UnpackLow(endpoints, new Vector128<byte>()).AsInt16();
        Vector128<short> endpoints_16bit_swapped = Sse2.Shuffle(endpoints_16bit.AsInt32(), 0b_01_00_11_10).AsInt16();

        // Colors 1 and 6
        Vector128<short> interpolated_16 = bc7_interp_sse2(endpoints_16bit, endpoints_16bit_swapped, Vector128.Create((short)9), Vector128.Create((short)55));

        // Colors 2,3 and 4,5 (batched with AVX2)
        Vector256<short> ep_256 = Vector256.Create(endpoints_16bit, endpoints_16bit);
        Vector256<short> eps_256 = Vector256.Create(endpoints_16bit_swapped, endpoints_16bit_swapped);
        Vector256<short> interpolated_2345 = bc7_interp_avx2(ep_256, eps_256,
            Vector256.Create((short)18, 18, 18, 18, 37, 37, 37, 37, 37, 37, 37, 37, 18, 18, 18, 18),
            Vector256.Create((short)46, 46, 46, 46, 27, 27, 27, 27, 27, 27, 27, 27, 46, 46, 46, 46));

        Vector128<short> interpolated_23 = interpolated_2345.GetLower();
        Vector128<short> interpolated_45 = interpolated_2345.GetUpper();

        Vector128<short> interpolated_01 = Sse2.UnpackLow(endpoints_16bit.AsInt64(), interpolated_16.AsInt64()).AsInt16();
        Vector128<short> interpolated_67 = Sse2.UnpackHigh(interpolated_16.AsInt64(), endpoints_16bit.AsInt64()).AsInt16();

        Vector128<byte> all_colors_0 = Sse2.PackUnsignedSaturate(interpolated_01, interpolated_23);
        Vector128<byte> all_colors_1 = Sse2.PackUnsignedSaturate(interpolated_45, interpolated_67);

        fixed (color_rgba* pOutput = out_colors)
        {
            Sse2.Store((byte*)pOutput, all_colors_0);
            Sse2.Store((byte*)(pOutput + 4), all_colors_1);
        }
    }
#endif

    static void unpack_bc7_mode0_2(int mode, ReadOnlySpan<ulong> data_chunks, Span<color_rgba> pPixels)
    {
        //const uint SUBSETS = 3;
        const int ENDPOINTS = 6;
        const int COMPS = 3;
        int WEIGHT_BITS = (mode == 0) ? 3 : 2;
        uint WEIGHT_MASK = (1u << WEIGHT_BITS) - 1;
        int ENDPOINT_BITS = (mode == 0) ? 4 : 5;
        uint ENDPOINT_MASK = (1u << ENDPOINT_BITS) - 1;
        int PBITS = (mode == 0) ? 6 : 0;
        uint WEIGHT_VALS = 1u << WEIGHT_BITS;
        int PART_BITS = (mode == 0) ? 4 : 6;
        uint PART_MASK = (1u << PART_BITS) - 1;

        ulong low_chunk = data_chunks[0];
        ulong high_chunk = data_chunks[1];

        uint part = (uint)((low_chunk >> (mode + 1)) & PART_MASK);

        Span<ulong> channel_read_chunks = stackalloc ulong[3] { 0, 0, 0 };

        if (mode == 0)
        {
            channel_read_chunks[0] = low_chunk >> 5;
            channel_read_chunks[1] = low_chunk >> 29;
            channel_read_chunks[2] = ((low_chunk >> 53) | (high_chunk << 11));
        }
        else
        {
            channel_read_chunks[0] = low_chunk >> 9;
            channel_read_chunks[1] = ((low_chunk >> 39) | (high_chunk << 25));
            channel_read_chunks[2] = high_chunk >> 5;
        }

        Span<color_rgba> endpoints = stackalloc color_rgba[ENDPOINTS];
        for (int c = 0; c < COMPS; c++)
        {
            ulong channel_read_chunk = channel_read_chunks[c];
            for (int e = 0; e < ENDPOINTS; e++)
            {
                endpoints[e][c] = (byte)(channel_read_chunk & ENDPOINT_MASK);
                channel_read_chunk >>= ENDPOINT_BITS;
            }
        }

        Span<uint> pbits = stackalloc uint[6];
        if (mode == 0)
        {
            byte p_bits_chunk = (byte)((high_chunk >> 13) & 0xff);

            for (int p = 0; p < PBITS; p++)
                pbits[p] = (uint)(p_bits_chunk >> p) & 1;
        }

        ulong weights_read_chunk = high_chunk >> (67 - 16 * WEIGHT_BITS);
        insert_weight_zero(ref weights_read_chunk, WEIGHT_BITS, 0);
        insert_weight_zero(ref weights_read_chunk, WEIGHT_BITS, Math.Min(g_bc7_table_anchor_index_third_subset_1[part], g_bc7_table_anchor_index_third_subset_2[part]));
        insert_weight_zero(ref weights_read_chunk, WEIGHT_BITS, Math.Max(g_bc7_table_anchor_index_third_subset_1[part], g_bc7_table_anchor_index_third_subset_2[part]));

        Span<uint> weights = stackalloc uint[16];
        for (int i = 0; i < 16; i++)
        {
            weights[i] = (uint)(weights_read_chunk & WEIGHT_MASK);
            weights_read_chunk >>= WEIGHT_BITS;
        }

        for (int e = 0; e < ENDPOINTS; e++)
            for (int c = 0; c < 4; c++)
                endpoints[e][c] = (byte)((c == 3) ? 255 : (PBITS != 0 ? bc7_dequant(endpoints[e][c], pbits[e], ENDPOINT_BITS) : bc7_dequant(endpoints[e][c], ENDPOINT_BITS)));

        Span<color_rgba> block_colors = stackalloc color_rgba[3 * 8];

#if NET6_0_OR_GREATER
        if (Sse2.IsSupported)
        {
            for (int s = 0; s < 3; s++)
            {
                if (WEIGHT_BITS == 2)
                    bc7_interp2_sse2(endpoints.Slice(s * 2), block_colors.Slice(s * 8));
                else if (Avx2.IsSupported)
                    bc7_interp3_avx2(endpoints.Slice(s * 2), block_colors.Slice(s * 8));
                else
                    bc7_interp3_sse2(endpoints.Slice(s * 2), block_colors.Slice(s * 8));
            }
        }
        else
#endif
        {
            for (int s = 0; s < 3; s++)
                for (int i = 0; i < WEIGHT_VALS; i++)
                {
                    for (int c = 0; c < 3; c++)
                        block_colors[s * 8 + i][c] = (byte)(bc7_interp(endpoints[s * 2 + 0][c], endpoints[s * 2 + 1][c], i, WEIGHT_BITS));
                    block_colors[s * 8 + i][3] = 255;
                }
        }

        for (int i = 0; i < 16; i++)
            pPixels[i] = block_colors[g_bc7_partition3[part * 16 + i] * 8 + (int)weights[i]];
    }

    static void unpack_bc7_mode1_3_7(int mode, ReadOnlySpan<ulong> data_chunks, Span<color_rgba> pPixels)
    {
        //const uint SUBSETS = 2;
        const int ENDPOINTS = 4;
        int COMPS = (mode == 7) ? 4 : 3;
        int WEIGHT_BITS = (mode == 1) ? 3 : 2;
        uint WEIGHT_MASK = (1u << WEIGHT_BITS) - 1;
        int ENDPOINT_BITS = (mode == 7) ? 5 : ((mode == 1) ? 6 : 7);
        uint ENDPOINT_MASK = (1u << ENDPOINT_BITS) - 1;
        int PBITS = (mode == 1) ? 2 : 4;
        bool SHARED_PBITS = (mode == 1) ? true : false;
        uint WEIGHT_VALS = 1u << WEIGHT_BITS;

        ulong low_chunk = data_chunks[0];
        ulong high_chunk = data_chunks[1];

        uint part = (uint)((low_chunk >> (mode + 1)) & 0x3f);

        Span<color_rgba> endpoints = stackalloc color_rgba[ENDPOINTS];

        Span<ulong> channel_read_chunks = stackalloc ulong[] { 0, 0, 0, 0 };
        ulong p_read_chunk = 0;
        channel_read_chunks[0] = (low_chunk >> (mode + 7));
        ulong weight_read_chunk;

        switch (mode)
        {
            case 1:
                channel_read_chunks[1] = (low_chunk >> 32);
                channel_read_chunks[2] = ((low_chunk >> 56) | (high_chunk << 8));
                p_read_chunk = high_chunk >> 16;
                weight_read_chunk = high_chunk >> 18;
                break;
            case 3:
                channel_read_chunks[1] = ((low_chunk >> 38) | (high_chunk << 26));
                channel_read_chunks[2] = high_chunk >> 2;
                p_read_chunk = high_chunk >> 30;
                weight_read_chunk = high_chunk >> 34;
                break;
            case 7:
                channel_read_chunks[1] = low_chunk >> 34;
                channel_read_chunks[2] = ((low_chunk >> 54) | (high_chunk << 10));
                channel_read_chunks[3] = high_chunk >> 10;
                p_read_chunk = (high_chunk >> 30);
                weight_read_chunk = (high_chunk >> 34);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        };

        for (int c = 0; c < COMPS; c++)
        {
            ulong channel_read_chunk = channel_read_chunks[c];
            for (int e = 0; e < ENDPOINTS; e++)
            {
                endpoints[e][c] = (byte)(channel_read_chunk & ENDPOINT_MASK);
                channel_read_chunk >>= ENDPOINT_BITS;
            }
        }

        Span<uint> pbits = stackalloc uint[4];
        for (int p = 0; p < PBITS; p++)
            pbits[p] = (uint)(p_read_chunk >> p) & 1;

        insert_weight_zero(ref weight_read_chunk, WEIGHT_BITS, 0);
        insert_weight_zero(ref weight_read_chunk, WEIGHT_BITS, g_bc7_table_anchor_index_second_subset[part]);

        Span<uint> weights = stackalloc uint[16];
        for (int i = 0; i < 16; i++)
        {
            weights[i] = (uint)(weight_read_chunk & WEIGHT_MASK);
            weight_read_chunk >>= WEIGHT_BITS;
        }

        for (int e = 0; e < ENDPOINTS; e++)
            for (int c = 0; c < 4; c++)
                endpoints[e][c] = (byte)((mode != 7U && c == 3U) ? 255 : bc7_dequant(endpoints[e][c], pbits[SHARED_PBITS ? (e >> 1) : e], ENDPOINT_BITS));

        Span<color_rgba> block_colors = stackalloc color_rgba[2 * 8];
#if NET6_0_OR_GREATER
        if (Sse2.IsSupported)
        {
            for (int s = 0; s < 2; s++)
            {
                if (WEIGHT_BITS == 2)
                    bc7_interp2_sse2(endpoints.Slice(s * 2), block_colors.Slice(s * 8));
                else if (Avx2.IsSupported)
                    bc7_interp3_avx2(endpoints.Slice(s * 2), block_colors.Slice(s * 8));
                else
                    bc7_interp3_sse2(endpoints.Slice(s * 2), block_colors.Slice(s * 8));
            }
        }
        else
#endif
        {
            for (int s = 0; s < 2; s++)
                for (int i = 0; i < WEIGHT_VALS; i++)
                {
                    for (int c = 0; c < COMPS; c++)
                        block_colors[s * 8 + i][c] = (byte)(bc7_interp(endpoints[s * 2 + 0][c], endpoints[s * 2 + 1][c], i, WEIGHT_BITS));
                    block_colors[s * 8 + i][3] = (COMPS == 3) ? (byte)255 : block_colors[s * 8 + i][3];
                }
        }

        for (int i = 0; i < 16; i++)
            pPixels[i] = block_colors[(int)(g_bc7_partition2[part * 16 + i] * 8 + weights[i])];
    }

    static void unpack_bc7_mode4_5(int mode, ReadOnlySpan<ulong> data_chunks, Span<color_rgba> pPixels)
    {
        const int ENDPOINTS = 2;
        //const uint COMPS = 4;
        const int WEIGHT_BITS = 2;
        uint WEIGHT_MASK = (1u << WEIGHT_BITS) - 1;
        int A_WEIGHT_BITS = (mode == 4) ? 3 : 2;
        uint A_WEIGHT_MASK = (1u << A_WEIGHT_BITS) - 1;
        int ENDPOINT_BITS = (mode == 4) ? 5 : 7;
        uint ENDPOINT_MASK = (1u << ENDPOINT_BITS) - 1;
        int A_ENDPOINT_BITS = (mode == 4) ? 6 : 8;
        uint A_ENDPOINT_MASK = (1u << A_ENDPOINT_BITS) - 1;
        //const uint WEIGHT_VALS = 1 << WEIGHT_BITS;
        //const uint A_WEIGHT_VALS = 1 << A_WEIGHT_BITS;

        ulong low_chunk = data_chunks[0];
        ulong high_chunk = data_chunks[1];

        uint comp_rot = (uint)(low_chunk >> (mode + 1)) & 0x3;
        uint index_mode = (mode == 4) ? (uint)((low_chunk >> 7) & 1) : 0;

        ulong color_read_bits = low_chunk >> 8;

        Span<color_rgba> endpoints = stackalloc color_rgba[ENDPOINTS];
        for (int c = 0; c < 3; c++)
        {
            for (int e = 0; e < ENDPOINTS; e++)
            {
                endpoints[e][c] = (byte)(color_read_bits & ENDPOINT_MASK);
                color_read_bits >>= ENDPOINT_BITS;
            }
        }

        endpoints[0][3] = (byte)(color_read_bits & ENDPOINT_MASK);

        ulong rgb_weights_chunk;
        ulong a_weights_chunk;
        if (mode == 4)
        {
            endpoints[0][3] = (byte)(color_read_bits & A_ENDPOINT_MASK);
            endpoints[1][3] = (byte)((color_read_bits >> A_ENDPOINT_BITS) & A_ENDPOINT_MASK);
            rgb_weights_chunk = ((low_chunk >> 50) | (high_chunk << 14));
            a_weights_chunk = high_chunk >> 17;
        }
        else if (mode == 5)
        {
            endpoints[0][3] = (byte)(color_read_bits & A_ENDPOINT_MASK);
            endpoints[1][3] = (byte)(((low_chunk >> 58) | (high_chunk << 6)) & A_ENDPOINT_MASK);
            rgb_weights_chunk = high_chunk >> 2;
            a_weights_chunk = high_chunk >> 33;
        }
        else
            throw new ArgumentOutOfRangeException(nameof(mode));

        insert_weight_zero(ref rgb_weights_chunk, WEIGHT_BITS, 0);
        insert_weight_zero(ref a_weights_chunk, A_WEIGHT_BITS, 0);

        Span<int> weight_bits = stackalloc int[2] { index_mode != 0 ? A_WEIGHT_BITS : WEIGHT_BITS, index_mode != 0 ? WEIGHT_BITS : A_WEIGHT_BITS };
        Span<uint> weight_mask = stackalloc uint[2] { index_mode != 0 ? A_WEIGHT_MASK : WEIGHT_MASK, index_mode != 0 ? WEIGHT_MASK : A_WEIGHT_MASK };

        Span<uint> weights = stackalloc uint[16];
        Span<uint> a_weights = stackalloc uint[16];

        if (index_mode != 0)
            std_swap(ref a_weights_chunk, ref rgb_weights_chunk);


        for (int i = 0; i < 16; i++)
        {
            weights[i] = (uint)(rgb_weights_chunk & weight_mask[0]);
            rgb_weights_chunk >>= weight_bits[0];
        }

        for (int i = 0; i < 16; i++)
        {
            a_weights[i] = (uint)(a_weights_chunk & weight_mask[1]);
            a_weights_chunk >>= weight_bits[1];
        }

        for (int e = 0; e < ENDPOINTS; e++)
            for (int c = 0; c < 4; c++)
                endpoints[e][c] = (byte)(bc7_dequant(endpoints[e][c], (c == 3) ? A_ENDPOINT_BITS : ENDPOINT_BITS));

        Span<color_rgba> block_colors = stackalloc color_rgba[8];
#if NET6_0_OR_GREATER
        if (Sse2.IsSupported)
        {
            if (weight_bits[0] == 3)
            {
                if (Avx2.IsSupported)
                    bc7_interp3_avx2(endpoints, block_colors);
                else
                    bc7_interp3_sse2(endpoints, block_colors);
            }
            else
                bc7_interp2_sse2(endpoints, block_colors);
        }
        else
#endif
        {
            for (int i = 0; i < (1U << (int)weight_bits[0]); i++)
                for (int c = 0; c < 3; c++)
                    block_colors[i][c] = (byte)(bc7_interp(endpoints[0][c], endpoints[1][c], i, weight_bits[0]));
        }

        for (int i = 0; i < (1U << weight_bits[1]); i++)
            block_colors[i][3] = (byte)(bc7_interp(endpoints[0][3], endpoints[1][3], i, weight_bits[1]));

        for (int i = 0; i < 16; i++)
        {
            pPixels[i] = block_colors[(int)weights[i]];
            pPixels[i].a = block_colors[(int)a_weights[i]].a;
            if (comp_rot >= 1)
            {
                std_swap(ref pPixels[i][(int)comp_rot - 1], ref pPixels[i].a);
            }
        }
    }

    public struct bc7_mode_6
    {
        public ulong m_lo;
        public ulong m_hi;

        public ulong m_mode => this.m_lo & 0x7f;
        public ulong m_r0 => (this.m_lo >> 7) & 0x7f;
        public ulong m_r1 => (this.m_lo >> 14) & 0x7f;
        public ulong m_g0 => (this.m_lo >> 21) & 0x7f;
        public ulong m_g1 => (this.m_lo >> 28) & 0x7f;
        public ulong m_b0 => (this.m_lo >> 35) & 0x7f;
        public ulong m_b1 => (this.m_lo >> 42) & 0x7f;
        public ulong m_a0 => (this.m_lo >> 49) & 0x7f;
        public ulong m_a1 => (this.m_lo >> 56) & 0x7f;
        public ulong m_p0 => (this.m_lo >> 63) & 0x7f;

        public ulong m_p1 => this.m_hi & 0x01;
        public ulong m_s00 => (this.m_hi >> 1) & 0x07;
        public ulong m_s10 => (this.m_hi >> 4) & 0x0f;
        public ulong m_s20 => (this.m_hi >> 8) & 0x0f;
        public ulong m_s30 => (this.m_hi >> 12) & 0x0f;

        public ulong m_s01 => (this.m_hi >> 16) & 0x0f;
        public ulong m_s11 => (this.m_hi >> 20) & 0x0f;
        public ulong m_s21 => (this.m_hi >> 24) & 0x0f;
        public ulong m_s31 => (this.m_hi >> 28) & 0x0f;

        public ulong m_s02 => (this.m_hi >> 32) & 0x0f;
        public ulong m_s12 => (this.m_hi >> 36) & 0x0f;
        public ulong m_s22 => (this.m_hi >> 40) & 0x0f;
        public ulong m_s32 => (this.m_hi >> 44) & 0x0f;

        public ulong m_s03 => (this.m_hi >> 48) & 0x0f;
        public ulong m_s13 => (this.m_hi >> 52) & 0x0f;
        public ulong m_s23 => (this.m_hi >> 56) & 0x0f;
        public ulong m_s33 => (this.m_hi >> 60) & 0x0f;
    };

    static unsafe void unpack_bc7_mode6(ReadOnlySpan<ulong> pBlock_bits, Span<color_rgba> pPixels)
    {
        ref readonly bc7_mode_6 block = ref MemoryMarshal.Cast<ulong, bc7_mode_6>(pBlock_bits)[0];

        if (block.m_mode != (1 << 6))
            throw new ArgumentOutOfRangeException("mode");

        uint r0 = (uint)((block.m_r0 << 1) | block.m_p0);
        uint g0 = (uint)((block.m_g0 << 1) | block.m_p0);
        uint b0 = (uint)((block.m_b0 << 1) | block.m_p0);
        uint a0 = (uint)((block.m_a0 << 1) | block.m_p0);
        uint r1 = (uint)((block.m_r1 << 1) | block.m_p1);
        uint g1 = (uint)((block.m_g1 << 1) | block.m_p1);
        uint b1 = (uint)((block.m_b1 << 1) | block.m_p1);
        uint a1 = (uint)((block.m_a1 << 1) | block.m_p1);

        Span<color_rgba> vals = stackalloc color_rgba[16];
#if NET6_0_OR_GREATER
        if (Sse2.IsSupported)
        {
            Vector128<short> vep0 = Vector128.Create((short)r0, (short)g0, (short)b0, (short)a0, (short)r0, (short)g0, (short)b0, (short)a0);
            Vector128<short> vep1 = Vector128.Create((short)r1, (short)g1, (short)b1, (short)a1, (short)r1, (short)g1, (short)b1, (short)a1);

            for (int i = 0; i < 16; i += 4)
            {
                Vector128<short> w0 = g_bc7_weights4_sse2[i / 4 * 2 + 0];
                Vector128<short> w1 = g_bc7_weights4_sse2[i / 4 * 2 + 1];

                Vector128<short> iw0 = Sse2.Subtract(Vector128.Create((short)64), w0);
                Vector128<short> iw1 = Sse2.Subtract(Vector128.Create((short)64), w1);

                Vector128<short> first_half = Sse2.ShiftRightLogical(Sse2.Add(Sse2.Add(Sse2.MultiplyLow(vep0, iw0), Sse2.MultiplyLow(vep1, w0)), Vector128.Create((short)32)), 6);
                Vector128<short> second_half = Sse2.ShiftRightLogical(Sse2.Add(Sse2.Add(Sse2.MultiplyLow(vep0, iw1), Sse2.MultiplyLow(vep1, w1)), Vector128.Create((short)32)), 6);
                Vector128<byte> combined = Sse2.PackUnsignedSaturate(first_half, second_half);

                fixed (color_rgba* pVals = vals)
                    Sse2.Store((byte*)(pVals + i), combined);
            }
        }
        else
#endif
        {
            for (int i = 0; i < 16; i++)
            {
                uint w = g_bc7_weights4[i];
                uint iw = 64 - w;
                vals[i].set_noclamp_rgba(
                    (r0 * iw + r1 * w + 32) >> 6,
                    (g0 * iw + g1 * w + 32) >> 6,
                    (b0 * iw + b1 * w + 32) >> 6,
                    (a0 * iw + a1 * w + 32) >> 6);
            }
        }

        pPixels[0] = vals[(int)block.m_s00];
        pPixels[1] = vals[(int)block.m_s10];
        pPixels[2] = vals[(int)block.m_s20];
        pPixels[3] = vals[(int)block.m_s30];

        pPixels[4] = vals[(int)block.m_s01];
        pPixels[5] = vals[(int)block.m_s11];
        pPixels[6] = vals[(int)block.m_s21];
        pPixels[7] = vals[(int)block.m_s31];

        pPixels[8] = vals[(int)block.m_s02];
        pPixels[9] = vals[(int)block.m_s12];
        pPixels[10] = vals[(int)block.m_s22];
        pPixels[11] = vals[(int)block.m_s32];

        pPixels[12] = vals[(int)block.m_s03];
        pPixels[13] = vals[(int)block.m_s13];
        pPixels[14] = vals[(int)block.m_s23];
        pPixels[15] = vals[(int)block.m_s33];
    }

    public static void unpack_bc7(ReadOnlySpan<byte> block_bytes, Span<color_rgba> pPixels)
    {
        byte mode = g_bc7_first_byte_to_mode[block_bytes[0]];
        ReadOnlySpan<ulong> data_chunks = MemoryMarshal.Cast<byte, ulong>(block_bytes).Slice(0, 2);
        // skip endianess checking
        switch (mode)
        {
            case 0:
            case 2:
                unpack_bc7_mode0_2(mode, data_chunks, pPixels);
                break;
            case 1:
            case 3:
            case 7:
                unpack_bc7_mode1_3_7(mode, data_chunks, pPixels);
                break;
            case 4:
            case 5:
                unpack_bc7_mode4_5(mode, data_chunks, pPixels);
                break;
            case 6:
                unpack_bc7_mode6(data_chunks, pPixels);
                break;
            default:
                Unsafe.InitBlockUnaligned(ref MemoryMarshal.AsBytes(pPixels)[0], 0, 4 * 16);
                break;
        }
    }

    static void std_swap<T>(ref T left, ref T right)
    {
        T temp = left;
        left = right;
        right = temp;
    }
}
}
