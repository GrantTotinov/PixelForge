namespace PixelForge;

/// <summary>A rectangle in pixel coordinates; the origin is the top-left corner.</summary>
/// <param name="X">The left edge.</param>
/// <param name="Y">The top edge.</param>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct Rectangle(int X, int Y, int Width, int Height);
