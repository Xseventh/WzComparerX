using System.Buffers.Binary;
using System.Text;

namespace WzComparerX.WzLib;

using static WzImageBinaryReaderPrimitives;

internal static class WzImageVideoHeaderReader
{
    private const int FixedHeaderLength = 36;

    public static WzImageVideoHeaderInspection? TryRead(
        Stream stream,
        long dataOffset,
        int dataLength,
        out string? error)
    {
        error = null;

        if (dataLength < 4)
        {
            error = "Video payload is too short for an MCV signature.";
            return null;
        }

        var position = stream.Position;
        var payloadEnd = dataOffset + dataLength;
        try
        {
            stream.Position = dataOffset;
            var signature = Encoding.ASCII.GetString(ReadBytes(stream, 4));
            if (signature != "MCV0")
            {
                error = $"Unsupported video signature: {signature}.";
                return null;
            }

            if (dataLength < FixedHeaderLength)
            {
                error = "MCV video payload is too short for the fixed header.";
                return null;
            }

            SkipBytes(stream, 2);
            var headerLength = unchecked((ushort)ReadInt16LittleEndian(stream));
            if (headerLength < FixedHeaderLength || headerLength > dataLength)
            {
                error = $"Invalid MCV header length: {headerLength}.";
                return null;
            }

            var fourCc = unchecked((uint)ReadInt32LittleEndian(stream)) ^ 0xa5a5a5a5u;
            var width = unchecked((ushort)ReadInt16LittleEndian(stream));
            var height = unchecked((ushort)ReadInt16LittleEndian(stream));
            var frameCount = ReadInt32LittleEndian(stream);
            if (frameCount < 0)
            {
                error = $"Invalid MCV frame count: {frameCount}.";
                return null;
            }

            var dataFlags = (WzImageVideoDataFlags)ReadByte(stream);
            SkipBytes(stream, 3);
            var frameDelayUnit = ReadInt64LittleEndian(stream);
            var defaultDelay = ReadInt32LittleEndian(stream);

            stream.Position = dataOffset + headerLength;
            if (!EnsureReadable(stream, payloadEnd, checked((long)frameCount * 8), out error))
            {
                return null;
            }

            var frames = new FrameBuilder[frameCount];
            for (var i = 0; i < frames.Length; i++)
            {
                frames[i] = new FrameBuilder(
                    i,
                    ReadInt32LittleEndian(stream),
                    ReadInt32LittleEndian(stream));
            }

            if ((dataFlags & WzImageVideoDataFlags.AlphaMap) != 0)
            {
                if (!EnsureReadable(stream, payloadEnd, checked((long)frameCount * 8), out error))
                {
                    return null;
                }

                for (var i = 0; i < frames.Length; i++)
                {
                    frames[i].AlphaDataOffset = ReadInt32LittleEndian(stream);
                    frames[i].AlphaDataLength = ReadInt32LittleEndian(stream);
                }
            }

            ReadFrameDelays(stream, payloadEnd, frameCount, frameDelayUnit, defaultDelay, dataFlags, frames, out error);
            if (error is not null)
            {
                return null;
            }

            ReadFrameTimeline(stream, payloadEnd, frameCount, frameDelayUnit, dataFlags, frames, out error);
            if (error is not null)
            {
                return null;
            }

            var dataStartPosition = stream.Position - dataOffset;
            var inspectedFrames = new WzImageVideoFrameInspection[frames.Length];
            for (var i = 0; i < frames.Length; i++)
            {
                inspectedFrames[i] = frames[i].ToInspection(dataStartPosition);
            }

            return new WzImageVideoHeaderInspection(
                signature,
                headerLength,
                fourCc,
                FormatFourCc(fourCc),
                width,
                height,
                frameCount,
                dataFlags,
                frameDelayUnit,
                defaultDelay,
                inspectedFrames);
        }
        catch (EndOfStreamException)
        {
            error = "MCV video header extends past the payload.";
            return null;
        }
        catch (OverflowException)
        {
            error = "MCV video timing or table size overflowed while parsing.";
            return null;
        }
        finally
        {
            stream.Position = position;
        }
    }

    private static void ReadFrameDelays(
        Stream stream,
        long payloadEnd,
        int frameCount,
        long frameDelayUnit,
        int defaultDelay,
        WzImageVideoDataFlags dataFlags,
        FrameBuilder[] frames,
        out string? error)
    {
        if ((dataFlags & WzImageVideoDataFlags.PerFrameDelay) == 0)
        {
            var delay = checked(defaultDelay * frameDelayUnit);
            for (var i = 0; i < frames.Length; i++)
            {
                frames[i].DelayInNanoseconds = delay;
            }

            error = null;
            return;
        }

        if (!EnsureReadable(stream, payloadEnd, checked((long)frameCount * 4), out error))
        {
            return;
        }

        for (var i = 0; i < frames.Length; i++)
        {
            frames[i].DelayInNanoseconds = checked(ReadInt32LittleEndian(stream) * frameDelayUnit);
        }
    }

    private static void ReadFrameTimeline(
        Stream stream,
        long payloadEnd,
        int frameCount,
        long frameDelayUnit,
        WzImageVideoDataFlags dataFlags,
        FrameBuilder[] frames,
        out string? error)
    {
        if ((dataFlags & WzImageVideoDataFlags.PerFrameTimeline) == 0)
        {
            long time = 0;
            for (var i = 0; i < frames.Length; i++)
            {
                frames[i].StartTimeInNanoseconds = time;
                time = checked(time + frames[i].DelayInNanoseconds);
            }

            error = null;
            return;
        }

        if (!EnsureReadable(stream, payloadEnd, checked((long)frameCount * 8), out error))
        {
            return;
        }

        for (var i = 0; i < frames.Length; i++)
        {
            frames[i].StartTimeInNanoseconds = checked(ReadInt64LittleEndian(stream) * frameDelayUnit);
        }
    }

    private static bool EnsureReadable(Stream stream, long payloadEnd, long byteCount, out string? error)
    {
        if (byteCount < 0)
        {
            error = $"Invalid MCV table byte count: {byteCount}.";
            return false;
        }

        if (stream.Position + byteCount > payloadEnd)
        {
            error = "MCV video frame table extends past the payload.";
            return false;
        }

        error = null;
        return true;
    }

    private static string FormatFourCc(uint fourCc)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, fourCc);
        return Encoding.ASCII.GetString(bytes);
    }

    private sealed class FrameBuilder
    {
        public FrameBuilder(int index, int dataOffset, int dataLength)
        {
            Index = index;
            DataOffset = dataOffset;
            DataLength = dataLength;
            AlphaDataOffset = -1;
        }

        public int Index { get; }
        public long DataOffset { get; }
        public int DataLength { get; }
        public long AlphaDataOffset { get; set; }
        public int AlphaDataLength { get; set; }
        public long DelayInNanoseconds { get; set; }
        public long StartTimeInNanoseconds { get; set; }

        public WzImageVideoFrameInspection ToInspection(long dataStartPosition)
        {
            var dataOffset = DataOffset + dataStartPosition;
            var alphaDataOffset = AlphaDataLength > 0 && AlphaDataOffset > -1
                ? AlphaDataOffset + dataStartPosition
                : AlphaDataOffset;

            return new WzImageVideoFrameInspection(
                Index,
                dataOffset,
                DataLength,
                alphaDataOffset,
                AlphaDataLength,
                DelayInNanoseconds,
                StartTimeInNanoseconds);
        }
    }
}
