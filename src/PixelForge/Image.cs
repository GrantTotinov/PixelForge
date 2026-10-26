using SkiaSharp;

namespace PixelForge;

/// <summary>
/// A decoded image held in memory as 8-bit RGBA. Create one with <c>Image.Load</c>; dispose it when done.
/// </summary>
public sealed partial class Image : IDisposable
{
    private SKBitmap? _bitmap;

    internal Image(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        _bitmap = bitmap;
        Origin = origin;
    }

    /// <summary>Gets the width in pixels.</summary>
    public int Width => Bitmap.Width;

    /// <summary>Gets the height in pixels.</summary>
    public int Height => Bitmap.Height;

    /// <summary>Gets the size in pixels.</summary>
    public Size Size => new(Width, Height);

    internal SKBitmap Bitmap => _bitmap ?? throw new ObjectDisposedException(nameof(Image));

    /// <summary>The EXIF orientation read at decode time. <c>AutoOrient()</c> applies it and resets it.</summary>
    internal SKEncodedOrigin Origin { get; set; }

    /// <summary>Releases the pixel memory.</summary>
    public void Dispose()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
