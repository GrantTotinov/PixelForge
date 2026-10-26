namespace PixelForge;

/// <summary>Image size and format read from the file header, without decoding pixels.</summary>
/// <param name="Width">The stored width in pixels, before any EXIF orientation is applied.</param>
/// <param name="Height">The stored height in pixels, before any EXIF orientation is applied.</param>
/// <param name="Format">The encoded format.</param>
public sealed record ImageInfo(int Width, int Height, ImageFormat Format)
{
    /// <summary>Gets the stored size in pixels.</summary>
    public Size Size => new(Width, Height);
}
