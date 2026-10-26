namespace PixelForge.Processing;

/// <summary>
/// Chains image operations inside <see cref="Image.Mutate"/> or <see cref="Image.Clone"/>.
/// Operations run in order, each on the result of the previous one. Not meant to be implemented outside PixelForge.
/// </summary>
public interface IImageProcessingContext
{
    /// <summary>Gets the size of the image at this point in the chain.</summary>
    /// <returns>The current size.</returns>
    Size GetCurrentSize();

    /// <summary>
    /// Rotates and mirrors the image as its EXIF orientation says, so it displays upright. Call this before other
    /// operations: PixelForge never writes metadata, so a saved image without it would show sideways.
    /// </summary>
    /// <returns>This context.</returns>
    IImageProcessingContext AutoOrient();

    /// <summary>Scales to exactly this size. A zero width or height is computed from the aspect ratio.</summary>
    /// <param name="width">The new width.</param>
    /// <param name="height">The new height.</param>
    /// <returns>This context.</returns>
    IImageProcessingContext Resize(int width, int height);

    /// <summary>Scales to exactly this size. A zero width or height is computed from the aspect ratio.</summary>
    /// <param name="size">The new size.</param>
    /// <returns>This context.</returns>
    IImageProcessingContext Resize(Size size);

    /// <summary>Resizes with a <see cref="ResizeMode"/>, anchor and pad color.</summary>
    /// <param name="options">The options.</param>
    /// <returns>This context.</returns>
    IImageProcessingContext Resize(ResizeOptions options);

    /// <summary>Keeps the top-left <paramref name="width"/> × <paramref name="height"/> pixels.</summary>
    /// <param name="width">The width to keep.</param>
    /// <param name="height">The height to keep.</param>
    /// <returns>This context.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The area is empty or outside the image.</exception>
    IImageProcessingContext Crop(int width, int height);

    /// <summary>Keeps the pixels inside <paramref name="rectangle"/>.</summary>
    /// <param name="rectangle">The area to keep; it must lie inside the image.</param>
    /// <returns>This context.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The area is empty or outside the image.</exception>
    IImageProcessingContext Crop(Rectangle rectangle);

    /// <summary>Rotates clockwise by a multiple of 90 degrees. Lossless.</summary>
    /// <param name="rotateMode">The rotation.</param>
    /// <returns>This context.</returns>
    IImageProcessingContext Rotate(RotateMode rotateMode);

    /// <summary>
    /// Rotates clockwise by any angle. Multiples of 90 are lossless; other angles grow the canvas to fit and leave
    /// transparent corners.
    /// </summary>
    /// <param name="degrees">The angle in degrees; negative is counterclockwise.</param>
    /// <returns>This context.</returns>
    IImageProcessingContext Rotate(float degrees);

    /// <summary>Rotates clockwise by any angle, filling the uncovered corners with <paramref name="background"/>.</summary>
    /// <param name="degrees">The angle in degrees; negative is counterclockwise.</param>
    /// <param name="background">The corner fill color.</param>
    /// <returns>This context.</returns>
    IImageProcessingContext Rotate(float degrees, Color background);

    /// <summary>Mirrors the image.</summary>
    /// <param name="flipMode">The direction.</param>
    /// <returns>This context.</returns>
    IImageProcessingContext Flip(FlipMode flipMode);

    /// <summary>Converts to grayscale using BT.709 luma weights. Alpha is kept.</summary>
    /// <returns>This context.</returns>
    IImageProcessingContext Grayscale();
}
