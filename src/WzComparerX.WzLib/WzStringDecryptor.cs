using System.Security.Cryptography;
using System.Text;

namespace WzComparerX.WzLib;

public sealed class WzStringDecryptor
{
    private static readonly byte[] GmsIv = [0x4d, 0x23, 0xc7, 0x2b];
    private static readonly byte[] KmsIv = [0xb9, 0x7d, 0x63, 0xe9];
    private static readonly byte[] AesKey =
    [
        0x13, 0x00, 0x00, 0x00,
        0x08, 0x00, 0x00, 0x00,
        0x06, 0x00, 0x00, 0x00,
        0xB4, 0x00, 0x00, 0x00,
        0x1B, 0x00, 0x00, 0x00,
        0x0F, 0x00, 0x00, 0x00,
        0x33, 0x00, 0x00, 0x00,
        0x52, 0x00, 0x00, 0x00
    ];

    private readonly WzStringEncryptionKind kind;
    private byte[]? keyStream;

    public WzStringDecryptor(WzStringEncryptionKind kind = WzStringEncryptionKind.Bms)
    {
        this.kind = kind;
    }

    public string Decode(ReadOnlySpan<byte> bytes, bool unicode)
    {
        var buffer = bytes.ToArray();
        DecryptKeyStream(buffer);

        if (unicode)
        {
            var chars = new char[buffer.Length / sizeof(char)];
            for (var i = 0; i < chars.Length; i++)
            {
                var value = (ushort)(buffer[i * 2] | (buffer[i * 2 + 1] << 8));
                chars[i] = (char)(value ^ (ushort)(0xAAAA + i));
            }

            return new string(chars);
        }

        var ascii = new byte[buffer.Length];
        for (var i = 0; i < buffer.Length; i++)
        {
            ascii[i] = (byte)(buffer[i] ^ (byte)(0xAA + i));
        }

        return Encoding.Latin1.GetString(ascii);
    }

    private void DecryptKeyStream(Span<byte> data)
    {
        if (kind == WzStringEncryptionKind.Bms || data.Length == 0)
        {
            return;
        }

        EnsureKeyStream(data.Length);
        for (var i = 0; i < data.Length; i++)
        {
            data[i] ^= keyStream![i];
        }
    }

    private void EnsureKeyStream(int length)
    {
        if (keyStream is not null && keyStream.Length >= length)
        {
            return;
        }

        var size = (length + 63) & ~63;
        var keys = new byte[size];
        if (keyStream is not null)
        {
            keyStream.CopyTo(keys.AsSpan());
        }

        var startIndex = keyStream?.Length ?? 0;
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Key = AesKey;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        using var encryptor = aes.CreateEncryptor();
        Span<byte> block = stackalloc byte[16];
        var iv = kind == WzStringEncryptionKind.Kms ? KmsIv : GmsIv;

        for (var i = startIndex; i < size; i += block.Length)
        {
            if (i == 0)
            {
                for (var j = 0; j < block.Length; j++)
                {
                    block[j] = iv[j % iv.Length];
                }
            }
            else
            {
                keys.AsSpan(i - block.Length, block.Length).CopyTo(block);
            }

            encryptor.TransformBlock(block.ToArray(), 0, block.Length, keys, i);
        }

        keyStream = keys;
    }
}
