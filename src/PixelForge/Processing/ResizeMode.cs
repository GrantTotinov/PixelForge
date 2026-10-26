namespace PixelForge.Processing;

/// <summary>How <see cref="ResizeOptions.Size"/> is applied.</summary>
public enum ResizeMode
{
    /// <summary>Fill the target size keeping the aspect ratio, then cut off what overflows at <see cref="ResizeOptions.Position"/>.</summary>
    Crop,

    /// <summary>Fit inside the target size keeping the aspect ratio, then fill the rest with <see cref="ResizeOptions.PadColor"/>.</summary>
    Pad,

    /// <summary>Fit inside the target size keeping the aspect ratio. The result can be smaller than the target in one dimension.</summary>
    Max,

    /// <summary>Scale down until the image just covers the target size, keeping the aspect ratio. Never upscales.</summary>
    Min,

    /// <summary>Scale to exactly the target size, ignoring the aspect ratio.</summary>
    Stretch,
}
