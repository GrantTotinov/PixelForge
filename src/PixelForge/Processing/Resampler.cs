using SkiaSharp;

namespace PixelForge.Processing;

internal static class Resampler
{
    private static readonly SKSamplingOptions Box = new(SKFilterMode.Linear);
    private static readonly SKSamplingOptions Downscale = new(SKCubicResampler.Mitchell);
    private static readonly SKSamplingOptions Upscale = new(SKCubicResampler.CatmullRom);

    /// <summary>Returns a new bitmap of the given size. The source is not modified or disposed.</summary>
    public static SKBitmap Resize(SKBitmap source, int width, int height)
    {
        if (width == source.Width && height == source.Height)
        {
            return source.Copy();
        }

        // A cubic filter only looks at a 4x4 neighborhood, so a single pass aliases when shrinking a lot.
        // Halve first: bilinear at exactly 1/2 averages each 2x2 block, which is a box filter.
        // Trade-off: odd sizes make a halving step slightly off 1/2; a real box/Lanczos kernel would be exact.
        var current = source;
        while (current.Width >= width * 2 || current.Height >= height * 2)
        {
            var halfWidth = current.Width >= width * 2 ? current.Width / 2 : current.Width;
            var halfHeight = current.Height >= height * 2 ? current.Height / 2 : current.Height;
            var half = Scale(current, halfWidth, halfHeight, Box);
            DisposeIntermediate(current, source);
            current = half;
        }

        var sampling = width > current.Width || height > current.Height ? Upscale : Downscale;
        var result = Scale(current, width, height, sampling);
        DisposeIntermediate(current, source);
        return result;
    }

    private static SKBitmap Scale(SKBitmap bitmap, int width, int height, SKSamplingOptions sampling) =>
        bitmap.Resize(new SKSizeI(width, height), sampling) ?? throw new InvalidOperationException($"SkiaSharp failed to resize to {width}x{height}.");

    private static void DisposeIntermediate(SKBitmap bitmap, SKBitmap source)
    {
        if (!ReferenceEquals(bitmap, source))
        {
            bitmap.Dispose();
        }
    }
}
