namespace WzComparerX.WzLib;

internal sealed class WzLimitedReadStream : Stream
{
    private readonly Stream inner;
    private readonly int length;
    private int remaining;

    public WzLimitedReadStream(Stream inner, int length)
    {
        this.inner = inner;
        this.length = length;
        remaining = length;
    }

    public override bool CanRead => inner.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => length;

    public override long Position
    {
        get => length - remaining;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (remaining <= 0)
        {
            return 0;
        }

        var read = inner.Read(buffer, offset, Math.Min(count, remaining));
        remaining -= read;
        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        if (remaining <= 0)
        {
            return 0;
        }

        var read = inner.Read(buffer[..Math.Min(buffer.Length, remaining)]);
        remaining -= read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
