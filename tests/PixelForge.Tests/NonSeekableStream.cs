namespace PixelForge.Tests;

/// <summary>Behaves like an ASP.NET Core request body: forward-only.</summary>
internal sealed class NonSeekableStream(byte[] bytes) : MemoryStream(bytes)
{
    public override bool CanSeek => false;

    public override long Length => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
}
