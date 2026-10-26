namespace PixelForge.Processing;

/// <summary>A lossless clockwise rotation by a multiple of 90 degrees.</summary>
public enum RotateMode
{
    /// <summary>No rotation.</summary>
    None,

    /// <summary>90 degrees clockwise.</summary>
    Rotate90,

    /// <summary>180 degrees.</summary>
    Rotate180,

    /// <summary>270 degrees clockwise (90 counterclockwise).</summary>
    Rotate270,
}
