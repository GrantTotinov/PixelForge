using PixelForge.Processing;
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

    /// <summary>Applies operations to this image in place.</summary>
    /// <param name="operation">The operations, for example <c>x =&gt; x.AutoOrient().Resize(800, 0)</c>.</param>
    public void Mutate(Action<IImageProcessingContext> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var context = new ImageProcessingContext(Bitmap, Origin);
        try
        {
            operation(context);
        }
        finally
        {
            // Operations dispose the bitmap they replace, so keep whatever the chain reached even on failure.
            _bitmap = context.Bitmap;
            Origin = context.Origin;
        }
    }

    /// <summary>Applies operations to a copy and returns it. This image is not changed.</summary>
    /// <param name="operation">The operations.</param>
    /// <returns>The new image; dispose it when done.</returns>
    public Image Clone(Action<IImageProcessingContext> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var context = new ImageProcessingContext(Bitmap.Copy(), Origin);
        try
        {
            operation(context);
            return new Image(context.Bitmap, context.Origin);
        }
        catch
        {
            context.Bitmap.Dispose();
            throw;
        }
    }

    /// <summary>Releases the pixel memory.</summary>
    public void Dispose()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
