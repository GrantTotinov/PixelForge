namespace PixelForge;

/// <summary>An encoded image format that PixelForge can decode.</summary>
public enum ImageFormat
{
    /// <summary>Portable Network Graphics. Can also be encoded.</summary>
    Png,

    /// <summary>JPEG. Can also be encoded.</summary>
    Jpeg,

    /// <summary>WebP. Can also be encoded.</summary>
    Webp,

    /// <summary>GIF. Only the first frame is decoded.</summary>
    Gif,

    /// <summary>Windows bitmap.</summary>
    Bmp,

    /// <summary>Windows icon. The largest image is decoded.</summary>
    Ico,

    /// <summary>Wireless bitmap.</summary>
    Wbmp,
}
