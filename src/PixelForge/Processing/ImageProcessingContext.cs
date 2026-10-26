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

    public IImageProcessingContext Crop(int width, int height) => Crop(new Rectangle(0, 0, width, height));

    public IImageProcessingContext Crop(Rectangle rectangle)
    {
        var (x, y, width, height) = rectangle;
        if (width <= 0 || height <= 0 || x < 0 || y < 0 || x + width > Bitmap.Width || y + height > Bitmap.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(rectangle), rectangle, $"Crop area must be non-empty and inside the {Bitmap.Width}x{Bitmap.Height} image.");
        }

        using var subset = new SKBitmap();
        if (!Bitmap.ExtractSubset(subset, new SKRectI(x, y, x + width, y + height)))
        {
            throw new InvalidOperationException("SkiaSharp failed to extract the crop area.");
        }

        return Replace(subset.Copy());
    }

    public IImageProcessingContext Rotate(RotateMode rotateMode) => rotateMode switch
    {
        RotateMode.None => this,
        RotateMode.Rotate90 => Replace(Orient(SKEncodedOrigin.RightTop)),
        RotateMode.Rotate180 => Replace(Orient(SKEncodedOrigin.BottomRight)),
        RotateMode.Rotate270 => Replace(Orient(SKEncodedOrigin.LeftBottom)),
        _ => throw new ArgumentOutOfRangeException(nameof(rotateMode), rotateMode, null),
    };

    public IImageProcessingContext Rotate(float degrees) => Rotate(degrees, Color.Transparent);

    public IImageProcessingContext Rotate(float degrees, Color background)
    {
        if (!float.IsFinite(degrees))
        {
            throw new ArgumentOutOfRangeException(nameof(degrees), degrees, "Angle must be a finite number.");
        }

        var normalized = ((degrees % 360) + 360) % 360;
        if (normalized % 90 == 0)
        {
            return Rotate((RotateMode)(int)(normalized / 90));
        }

        int w = Bitmap.Width, h = Bitmap.Height;
        var radians = normalized * Math.PI / 180;
        double cos = Math.Abs(Math.Cos(radians)), sin = Math.Abs(Math.Sin(radians));
        // The epsilon keeps float noise from adding a pixel.
        var width = (int)Math.Ceiling((w * cos) + (h * sin) - 1e-6);
        var height = (int)Math.Ceiling((w * sin) + (h * cos) - 1e-6);

        var result = NewBitmap(width, height);
        result.Erase(background.ToSKColor());
        using var canvas = new SKCanvas(result);
        canvas.Translate(width / 2f, height / 2f);
        canvas.RotateDegrees(normalized);
        canvas.Translate(-w / 2f, -h / 2f);
        using var paint = new SKPaint();
        paint.IsAntialias = true;
        canvas.DrawBitmap(Bitmap, 0, 0, new SKSamplingOptions(SKCubicResampler.Mitchell), paint);
        return Replace(result);
    }

    public IImageProcessingContext Flip(FlipMode flipMode) => flipMode switch
    {
        FlipMode.None => this,
        FlipMode.Horizontal => Replace(Orient(SKEncodedOrigin.TopRight)),
        FlipMode.Vertical => Replace(Orient(SKEncodedOrigin.BottomLeft)),
        _ => throw new ArgumentOutOfRangeException(nameof(flipMode), flipMode, null),
    };

    public IImageProcessingContext Grayscale()
    {
        // BT.709 luma, applied by Skia to unpremultiplied color.
        ReadOnlySpan<float> matrix =
        [
            0.2126f, 0.7152f, 0.0722f, 0, 0,
            0.2126f, 0.7152f, 0.0722f, 0, 0,
            0.2126f, 0.7152f, 0.0722f, 0, 0,
            0, 0, 0, 1, 0,
        ];
        var result = NewBitmap(Bitmap.Width, Bitmap.Height);
        using var canvas = new SKCanvas(result);
        using var filter = SKColorFilter.CreateColorMatrix(matrix);
        using var paint = new SKPaint();
        paint.ColorFilter = filter;
        canvas.DrawBitmap(Bitmap, 0, 0, default, paint);
        return Replace(result);
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
