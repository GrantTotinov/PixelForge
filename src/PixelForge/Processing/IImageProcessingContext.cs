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
}
