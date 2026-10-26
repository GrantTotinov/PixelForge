using SkiaSharp;

namespace PixelForge.Processing;

// Trade-off: every operation allocates a new bitmap and frees the old one. Chain on one SKCanvas if benchmarks show it matters.
internal sealed class ImageProcessingContext(SKBitmap bitmap, SKEncodedOrigin origin) : IImageProcessingContext
{
    public SKBitmap Bitmap { get; private set; } = bitmap;

    public SKEncodedOrigin Origin { get; private set; } = origin;

    public Size GetCurrentSize() => new(Bitmap.Width, Bitmap.Height);

    public IImageProcessingContext AutoOrient()
    {
        if (Origin != SKEncodedOrigin.TopLeft)
        {
            Replace(Orient(Origin));
            Origin = SKEncodedOrigin.TopLeft;
        }

        return this;
    }

    public IImageProcessingContext Resize(int width, int height) =>
        Resize(new ResizeOptions { Size = new Size(width, height), Mode = ResizeMode.Stretch });

    public IImageProcessingContext Resize(Size size) => Resize(size.Width, size.Height);

    public IImageProcessingContext Resize(ResizeOptions options)
    {
        var (canvas, target) = ResizeCalculator.Calculate(GetCurrentSize(), options);
        var scaled = Resampler.Resize(Bitmap, target.Width, target.Height);
        if (target == new Rectangle(0, 0, canvas.Width, canvas.Height))
        {
            return Replace(scaled);
        }

        using (scaled)
        {
            var result = NewBitmap(canvas.Width, canvas.Height);
            result.Erase(options.PadColor.ToSKColor());
            using var surface = new SKCanvas(result);
            surface.DrawBitmap(scaled, target.X, target.Y, default, null);
            return Replace(result);
        }
    }

    /// <summary>
    /// Redraws the bitmap with an exact integer transform. Each <see cref="SKEncodedOrigin"/> value (EXIF orientation 1-8)
    /// says where the stored rows and columns belong; the matrix maps stored coordinates to display coordinates.
    /// </summary>
    private SKBitmap Orient(SKEncodedOrigin origin)
    {
        float w = Bitmap.Width, h = Bitmap.Height;
        var swap = origin >= SKEncodedOrigin.LeftTop;
        var matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),     // mirror horizontally
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1), // rotate 180
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),   // mirror vertically
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),       // transpose
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),     // rotate 90 clockwise
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1), // transverse
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),   // rotate 270 clockwise
            _ => SKMatrix.Identity,
        };

        var result = swap ? NewBitmap(Bitmap.Height, Bitmap.Width) : NewBitmap(Bitmap.Width, Bitmap.Height);
        using var canvas = new SKCanvas(result);
        canvas.SetMatrix(in matrix);
        canvas.DrawBitmap(Bitmap, 0, 0, default, null);
        return result;
    }

    /// <summary>A transparent bitmap with the current color type and color space.</summary>
    private SKBitmap NewBitmap(int width, int height)
    {
        var bitmap = new SKBitmap(Bitmap.Info.WithSize(width, height));
        bitmap.Erase(SKColors.Transparent);
        return bitmap;
    }

    private ImageProcessingContext Replace(SKBitmap next)
    {
        Bitmap.Dispose();
        Bitmap = next;
        return this;
    }
}
