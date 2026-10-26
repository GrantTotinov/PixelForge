namespace PixelForge.Processing;

/// <summary>Which part of the image is kept by <see cref="ResizeMode.Crop"/>, or where the image sits in <see cref="ResizeMode.Pad"/>.</summary>
public enum AnchorPositionMode
{
    /// <summary>Centered.</summary>
    Center,

    /// <summary>Top edge, centered horizontally.</summary>
    Top,

    /// <summary>Bottom edge, centered horizontally.</summary>
    Bottom,

    /// <summary>Left edge, centered vertically.</summary>
    Left,

    /// <summary>Right edge, centered vertically.</summary>
    Right,

    /// <summary>Top-left corner.</summary>
    TopLeft,

    /// <summary>Top-right corner.</summary>
    TopRight,

    /// <summary>Bottom-right corner.</summary>
    BottomRight,

    /// <summary>Bottom-left corner.</summary>
    BottomLeft,
}
