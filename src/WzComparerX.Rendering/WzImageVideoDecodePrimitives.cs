using WzComparerX.WzLib;

namespace WzComparerX.Rendering;

internal static class WzImageVideoDecodePrimitives
{
    public static WzVideoDecodeDiagnostic? ValidateVideo(
        Stream imagePayloadStream,
        WzImageVideoInspection video,
        out WzImageVideoHeaderInspection? header)
    {
        ArgumentNullException.ThrowIfNull(imagePayloadStream);
        ArgumentNullException.ThrowIfNull(video);

        header = null;
        if (!imagePayloadStream.CanRead || !imagePayloadStream.CanSeek)
        {
            return WzVideoDecodeDiagnostic.InvalidMetadata("Video frame decode requires a readable and seekable image payload stream.");
        }

        if (video.Header is null)
        {
            var message = video.HeaderError is null
                ? "Video metadata does not include a readable MCV header."
                : $"Video metadata does not include a readable MCV header: {video.HeaderError}";
            return WzVideoDecodeDiagnostic.InvalidMetadata(message);
        }

        if (video.Header.FourCcText is not ("VP90" or "VP80"))
        {
            return WzVideoDecodeDiagnostic.UnsupportedCodec(video.Header.FourCcText);
        }

        header = video.Header;
        return null;
    }

    public static PacketReadResult ReadPacket(
        Stream imagePayloadStream,
        WzImageVideoInspection video,
        long packetOffsetInVideoPayload,
        int packetLength,
        string packetName)
    {
        var absoluteOffset = video.DataOffset + packetOffsetInVideoPayload;
        if (packetLength < 0 ||
            absoluteOffset < 0 ||
            absoluteOffset > imagePayloadStream.Length ||
            packetLength > imagePayloadStream.Length - absoluteOffset)
        {
            return PacketReadResult.Fail(WzVideoDecodeDiagnostic.PacketOutOfBounds(
                packetName,
                absoluteOffset,
                packetLength,
                imagePayloadStream.Length));
        }

        var packet = new byte[packetLength];
        imagePayloadStream.Position = absoluteOffset;
        imagePayloadStream.ReadExactly(packet);
        return PacketReadResult.Success(packet);
    }
}

internal sealed record PacketReadResult(
    ReadOnlyMemory<byte> Packet,
    WzVideoDecodeDiagnostic? Diagnostic)
{
    public static PacketReadResult Success(byte[] packet)
    {
        return new PacketReadResult(packet, null);
    }

    public static PacketReadResult Fail(WzVideoDecodeDiagnostic diagnostic)
    {
        return new PacketReadResult(ReadOnlyMemory<byte>.Empty, diagnostic);
    }
}
