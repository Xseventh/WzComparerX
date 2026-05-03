using System.Buffers.Binary;

namespace WzComparerX.WzLib;

internal static class WzMsChaCha20
{
    public const int KeyLength = 32;
    public const int NonceLength = 12;
    public const int BlockLength = 64;

    public static void XorBlock(ReadOnlySpan<byte> input, Span<byte> output, ReadOnlySpan<byte> key)
    {
        if (input.Length > BlockLength)
        {
            throw new ArgumentOutOfRangeException(nameof(input));
        }

        if (output.Length < input.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(output));
        }

        Span<byte> nonce = stackalloc byte[NonceLength];
        Span<byte> keyStream = stackalloc byte[BlockLength];
        GenerateBlock(key, nonce, counter: 0, keyStream);
        for (var i = 0; i < input.Length; i++)
        {
            output[i] = (byte)(input[i] ^ keyStream[i]);
        }
    }

    private static void GenerateBlock(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> nonce,
        uint counter,
        Span<byte> output)
    {
        if (key.Length != KeyLength)
        {
            throw new ArgumentException($"MS ChaCha20 keys must be {KeyLength} bytes.", nameof(key));
        }

        if (nonce.Length != NonceLength)
        {
            throw new ArgumentException($"MS ChaCha20 nonce must be {NonceLength} bytes.", nameof(nonce));
        }

        if (output.Length < BlockLength)
        {
            throw new ArgumentOutOfRangeException(nameof(output));
        }

        Span<uint> state = stackalloc uint[16];
        state[0] = 0x61707865;
        state[1] = 0x3320646e;
        state[2] = 0x79622d32;
        state[3] = 0x6b206574;
        for (var i = 0; i < 8; i++)
        {
            state[4 + i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * 4, 4));
        }

        state[12] = counter;
        state[13] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(0, 4));
        state[14] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(4, 4));
        state[15] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(8, 4));

        Span<uint> working = stackalloc uint[16];
        state.CopyTo(working);
        for (var round = 0; round < 10; round++)
        {
            QuarterRound(ref working[0], ref working[4], ref working[8], ref working[12]);
            QuarterRound(ref working[1], ref working[5], ref working[9], ref working[13]);
            QuarterRound(ref working[2], ref working[6], ref working[10], ref working[14]);
            QuarterRound(ref working[3], ref working[7], ref working[11], ref working[15]);
            QuarterRound(ref working[0], ref working[5], ref working[10], ref working[15]);
            QuarterRound(ref working[1], ref working[6], ref working[11], ref working[12]);
            QuarterRound(ref working[2], ref working[7], ref working[8], ref working[13]);
            QuarterRound(ref working[3], ref working[4], ref working[9], ref working[14]);
        }

        for (var i = 0; i < 16; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(i * 4, 4), unchecked(working[i] + state[i]));
        }
    }

    private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
    {
        unchecked
        {
            a += b;
            d ^= a;
            d = RotateLeft(d, 16);
            c += d;
            b ^= c;
            b = RotateLeft(b, 12);
            a += b;
            d ^= a;
            d = RotateLeft(d, 8);
            c += d;
            b ^= c;
            b = RotateLeft(b, 7);
        }
    }

    private static uint RotateLeft(uint value, int count)
    {
        return (value << count) | (value >> (32 - count));
    }
}
