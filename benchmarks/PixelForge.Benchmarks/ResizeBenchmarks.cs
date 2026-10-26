using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using ImageMagick;
using ImageMagick.Formats;
using PixelForge.Processing;
using SkiaSharp;
using IS = SixLabors.ImageSharp;
using ISFormats = SixLabors.ImageSharp.Formats;
using ISJpeg = SixLabors.ImageSharp.Formats.Jpeg;
using ISProcessing = SixLabors.ImageSharp.Processing;

namespace PixelForge.Benchmarks;

/// <summary>
/// Decode a 4000x3000 JPEG, resize, encode JPEG q75 to memory. Each library uses its documented API;
/// "Thumbnail" uses each library's decode-time downscaling where it has one.
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ResizeBenchmarks
{
    private byte[] _jpeg = [];

    [GlobalSetup]
    public void Setup()
    {
        _jpeg = SourceImage.Create(4000, 3000);
        NetVips.Cache.Max = 0; // no result caching between iterations
    }

    [Benchmark(Baseline = true, Description = "PixelForge")]
    [BenchmarkCategory("Resize 4000x3000 -> 800x600")]
    public long PixelForgeResize()
    {
        using var image = Image.Load(_jpeg);
        image.Mutate(x => x.Resize(800, 600));
        return SaveJpeg(image);
    }

    [Benchmark(Description = "ImageSharp 3.1.12")]
    [BenchmarkCategory("Resize 4000x3000 -> 800x600")]
    public long ImageSharpResize()
    {
        using var image = IS.Image.Load(_jpeg);
        ISProcessing.ProcessingExtensions.Mutate(image, x => ISProcessing.ResizeExtensions.Resize(x, 800, 600));
        var ms = new MemoryStream();
        IS.ImageExtensions.SaveAsJpeg(image, ms, new ISJpeg.JpegEncoder { Quality = 75 });
        return ms.Length;
    }

    [Benchmark(Description = "NetVips")]
    [BenchmarkCategory("Resize 4000x3000 -> 800x600")]
    public long NetVipsResize()
    {
        using var image = NetVips.Image.NewFromBuffer(_jpeg);
        using var resized = image.Resize(800.0 / image.Width);
        return resized.JpegsaveBuffer(q: 75).Length;
    }

    [Benchmark(Description = "Magick.NET Q8")]
    [BenchmarkCategory("Resize 4000x3000 -> 800x600")]
    public long MagickResize()
    {
        using var image = new MagickImage(_jpeg);
        image.Resize(800, 600);
        image.Quality = 75;
        return image.ToByteArray(MagickFormat.Jpeg).Length;
    }

    [Benchmark(Baseline = true, Description = "PixelForge")]
    [BenchmarkCategory("Thumbnail 200px")]
    public long PixelForgeThumbnail()
    {
        var size = new Size(200, 200);
        using var image = Image.Load(_jpeg, new DecoderOptions { TargetSize = size });
        image.Mutate(x => x.AutoOrient().Resize(new ResizeOptions { Size = size, Mode = ResizeMode.Max }));
        return SaveJpeg(image);
    }

    [Benchmark(Description = "ImageSharp 3.1.12")]
    [BenchmarkCategory("Thumbnail 200px")]
    public long ImageSharpThumbnail()
    {
        var size = new IS.Size(200, 200);
        using var image = IS.Image.Load(new ISFormats.DecoderOptions { TargetSize = size }, _jpeg);
        ISProcessing.ProcessingExtensions.Mutate(image, x => ISProcessing.ResizeExtensions.Resize(
            ISProcessing.AutoOrientExtensions.AutoOrient(x),
            new ISProcessing.ResizeOptions { Size = size, Mode = ISProcessing.ResizeMode.Max }));
        var ms = new MemoryStream();
        IS.ImageExtensions.SaveAsJpeg(image, ms, new ISJpeg.JpegEncoder { Quality = 75 });
        return ms.Length;
    }

    [Benchmark(Description = "NetVips")]
    [BenchmarkCategory("Thumbnail 200px")]
    public long NetVipsThumbnail()
    {
        using var image = NetVips.Image.ThumbnailBuffer(_jpeg, 200, height: 200);
        return image.JpegsaveBuffer(q: 75).Length;
    }

    [Benchmark(Description = "Magick.NET Q8")]
    [BenchmarkCategory("Thumbnail 200px")]
    public long MagickThumbnail()
    {
        var settings = new MagickReadSettings();
        settings.SetDefines(new JpegReadDefines { Size = new MagickGeometry(200, 200) });
        using var image = new MagickImage(_jpeg, settings);
        image.AutoOrient();
        image.Thumbnail(200, 200);
        image.Quality = 75;
        return image.ToByteArray(MagickFormat.Jpeg).Length;
    }

    private static long SaveJpeg(Image image)
    {
        var ms = new MemoryStream();
        image.SaveAsJpeg(ms, 75);
        return ms.Length;
    }
}

internal static class SourceImage
{
    /// <summary>A photo-like JPEG (gradients, shapes, sensor-like noise) at quality 90.</summary>
    public static byte[] Create(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(bitmap))
        using (var paint = new SKPaint { IsAntialias = true })
        {
            paint.Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(width, height),
                [new SKColor(0x1E, 0x3A, 0x8A), new SKColor(0xF9, 0x73, 0x16), new SKColor(0x14, 0x53, 0x2D)], SKShaderTileMode.Clamp);
            canvas.DrawRect(0, 0, width, height, paint);
            paint.Shader = null;
            var random = new Random(1);
            for (var i = 0; i < 300; i++)
            {
                paint.Color = new SKColor((uint)random.Next()).WithAlpha(120);
                canvas.DrawCircle(random.Next(width), random.Next(height), random.Next(10, 200), paint);
            }
        }

        var noise = new Random(2);
        var pixels = bitmap.GetPixelSpan();
        for (var i = 0; i < pixels.Length; i++)
        {
            if (i % 4 != 3)
            {
                pixels[i] = (byte)Math.Clamp(pixels[i] + noise.Next(-12, 13), 0, 255);
            }
        }

        using var data = bitmap.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }
}
