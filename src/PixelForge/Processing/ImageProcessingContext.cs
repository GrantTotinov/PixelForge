using SkiaSharp;

namespace PixelForge.Processing;

// Trade-off: every operation allocates a new bitmap and frees the old one. Chain on one SKCanvas if benchmarks show it matters.
internal sealed class ImageProcessingContext(SKBitmap bitmap, SKEncodedOrigin origin) : IImageProcessingContext
{
    public SKBitmap Bitmap { get; private set; } = bitmap;

    public SKEncodedOrigin Origin { get; private set; } = origin;

    public Size GetCurrentSize() => new(Bitmap.Width, Bitmap.Height);

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

    private SKBitmap NewBitmap(int width, int height) => new(Bitmap.Info.WithSize(width, height));

    private ImageProcessingContext Replace(SKBitmap next)
    {
        Bitmap.Dispose();
        Bitmap = next;
        return this;
    }
}
