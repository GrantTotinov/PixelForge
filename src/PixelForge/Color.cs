using SkiaSharp;

namespace PixelForge;

/// <summary>An sRGB color with 8 bits per channel and straight (not premultiplied) alpha.</summary>
/// <param name="R">Red.</param>
/// <param name="G">Green.</param>
/// <param name="B">Blue.</param>
/// <param name="A">Alpha; 255 is opaque.</param>
public readonly record struct Color(byte R, byte G, byte B, byte A = 255)
{
    /// <summary>Gets fully transparent black.</summary>
    public static Color Transparent => new(0, 0, 0, 0);

    /// <summary>Gets opaque black.</summary>
    public static Color Black => new(0, 0, 0);

    /// <summary>Gets opaque white.</summary>
    public static Color White => new(255, 255, 255);

    /// <summary>Creates an opaque color.</summary>
    /// <param name="r">Red.</param>
    /// <param name="g">Green.</param>
    /// <param name="b">Blue.</param>
    /// <returns>The color.</returns>
    public static Color FromRgb(byte r, byte g, byte b) => new(r, g, b);

    /// <summary>Creates a color with alpha.</summary>
    /// <param name="r">Red.</param>
    /// <param name="g">Green.</param>
    /// <param name="b">Blue.</param>
    /// <param name="a">Alpha; 255 is opaque.</param>
    /// <returns>The color.</returns>
    public static Color FromRgba(byte r, byte g, byte b, byte a) => new(r, g, b, a);

    internal SKColor ToSKColor() => new(R, G, B, A);
}
