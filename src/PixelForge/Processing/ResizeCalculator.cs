namespace PixelForge.Processing;

/// <summary>Turns a source size and <see cref="ResizeOptions"/> into an output canvas and the rectangle the scaled image is drawn to.</summary>
internal static class ResizeCalculator
{
    public static (Size Canvas, Rectangle Target) Calculate(Size source, ResizeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var (width, height) = options.Size;
        ArgumentOutOfRangeException.ThrowIfNegative(width, "options.Size.Width");
        ArgumentOutOfRangeException.ThrowIfNegative(height, "options.Size.Height");
        if (width == 0 && height == 0)
        {
            throw new ArgumentException("Width and height cannot both be zero.", nameof(options));
        }

        if (width == 0)
        {
            width = Scale(source.Width, (double)height / source.Height);
        }
        else if (height == 0)
        {
            height = Scale(source.Height, (double)width / source.Width);
        }

        var fit = Math.Min((double)width / source.Width, (double)height / source.Height);
        var fill = Math.Max((double)width / source.Width, (double)height / source.Height);

        switch (options.Mode)
        {
            case ResizeMode.Stretch:
                return Whole(new Size(width, height));

            case ResizeMode.Max:
                return Whole(ScaleSize(source, fit));

            case ResizeMode.Min:
                return Whole(fill >= 1 ? source : ScaleSize(source, fill));

            case ResizeMode.Pad:
                return Anchor(new Size(width, height), ScaleSize(source, fit), options.Position);

            case ResizeMode.Crop:
                var scaled = ScaleSize(source, fill);
                // Rounding must never leave a gap at the edge of the canvas.
                scaled = new Size(Math.Max(scaled.Width, width), Math.Max(scaled.Height, height));
                return Anchor(new Size(width, height), scaled, options.Position);

            default:
                throw new ArgumentOutOfRangeException(nameof(options), options.Mode, "Unknown resize mode.");
        }
    }

    private static (Size, Rectangle) Whole(Size size) => (size, new Rectangle(0, 0, size.Width, size.Height));

    private static (Size, Rectangle) Anchor(Size canvas, Size scaled, AnchorPositionMode position)
    {
        var x = position switch
        {
            AnchorPositionMode.Left or AnchorPositionMode.TopLeft or AnchorPositionMode.BottomLeft => 0,
            AnchorPositionMode.Right or AnchorPositionMode.TopRight or AnchorPositionMode.BottomRight => canvas.Width - scaled.Width,
            _ => (canvas.Width - scaled.Width) / 2,
        };
        var y = position switch
        {
            AnchorPositionMode.Top or AnchorPositionMode.TopLeft or AnchorPositionMode.TopRight => 0,
            AnchorPositionMode.Bottom or AnchorPositionMode.BottomLeft or AnchorPositionMode.BottomRight => canvas.Height - scaled.Height,
            _ => (canvas.Height - scaled.Height) / 2,
        };
        return (canvas, new Rectangle(x, y, scaled.Width, scaled.Height));
    }

    private static Size ScaleSize(Size size, double ratio) => new(Scale(size.Width, ratio), Scale(size.Height, ratio));

    private static int Scale(int value, double ratio) => Math.Max(1, (int)Math.Round(value * ratio));
}
