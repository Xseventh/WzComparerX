using System.Buffers.Binary;
using WzComparerX.WzLib;

namespace WzComparerX.Tests;

internal static class WzListFileFixture
{
    public static byte[] Create(WzStringEncryptionKind stringKey, params string[] entries)
    {
        var bytes = new List<byte>();
        var lengthBytes = new byte[sizeof(int)];
        foreach (var entry in entries)
        {
            var plain = new byte[checked(entry.Length * sizeof(char))];
            for (var i = 0; i < entry.Length; i++)
            {
                var character = entry[i];
                if (character > byte.MaxValue)
                {
                    throw new ArgumentException("Synthetic List.wz entries currently support Latin-1 characters only.", nameof(entries));
                }

                plain[i * sizeof(char)] = (byte)character;
            }

            var encrypted = new WzStringDecryptor(stringKey).DecryptPayload(plain);
            BinaryPrimitives.WriteInt32LittleEndian(lengthBytes, entry.Length);
            bytes.AddRange(lengthBytes);
            bytes.AddRange(encrypted);
            bytes.Add(0);
            bytes.Add(0);
        }

        return bytes.ToArray();
    }
}
