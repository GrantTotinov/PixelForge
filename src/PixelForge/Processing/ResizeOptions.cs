namespace PixelForge.Processing;

/// <summary>Options for <see cref="IImageProcessingContext.Resize(ResizeOptions)"/>.</summary>
public sealed class ResizeOptions
{
    /// <summary>Gets or sets the target size. A zero width or height is computed from the aspect ratio.</summary>
    public Size Size { get; set; }

    /// <summary>Gets or sets how the target size is applied. Defaults to <see cref="ResizeMode.Crop"/>.</summary>
    public ResizeMode Mode { get; set; } = ResizeMode.Crop;

    /// <summary>Gets or sets the anchor for <see cref="ResizeMode.Crop"/> and <see cref="ResizeMode.Pad"/>. Defaults to center.</summary>
    public AnchorPositionMode Position { get; set; } = AnchorPositionMode.Center;

    /// <summary>Gets or sets the fill color for <see cref="ResizeMode.Pad"/>. Defaults to transparent.</summary>
    public Color PadColor { get; set; } = Color.Transparent;
}
