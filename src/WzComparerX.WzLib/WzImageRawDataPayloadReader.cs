namespace WzComparerX.WzLib;

public static class WzImageRawDataPayloadReader
{
    public static byte[] Read(Stream stream, WzImageRawDataInspection rawData)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(rawData);

        if (!stream.CanRead || !stream.CanSeek)
        {
            throw new ArgumentException("RawData payload streams must be readable and seekable.", nameof(stream));
        }

        if (rawData.DataOffset < 0 || rawData.DataLength < 0)
        {
            throw new InvalidDataException("RawData payload metadata contains a negative offset or length.");
        }

        var payloadEnd = checked(rawData.DataOffset + rawData.DataLength);
        if (payloadEnd > stream.Length)
        {
            throw new InvalidDataException(
                $"RawData payload extends past the source stream: {payloadEnd} > {stream.Length}.");
        }

        var oldPosition = stream.Position;
        try
        {
            stream.Position = rawData.DataOffset;
            var payload = new byte[rawData.DataLength];
            stream.ReadExactly(payload);
            return payload;
        }
        finally
        {
            stream.Position = oldPosition;
        }
    }
}
