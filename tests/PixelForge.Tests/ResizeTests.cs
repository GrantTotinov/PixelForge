using PixelForge.Processing;
using SkiaSharp;

namespace PixelForge.Tests;

public class ResizeTests
{
    [Theory]
    // source 400x200
    [InlineData(ResizeMode.Stretch, 100, 100, 100, 100)]
    [InlineData(ResizeMode.Stretch, 100, 0, 100, 50)]
    [InlineData(ResizeMode.Max, 100, 100, 100, 50)]
    [InlineData(ResizeMode.Max, 0, 100, 200, 100)]
    [InlineData(ResizeMode.Max, 800, 800, 800, 400)]
    [InlineData(ResizeMode.Min, 100, 100, 200, 100)]
    [InlineData(ResizeMode.Min, 800, 800, 400, 200)]
    [InlineData(ResizeMode.Crop, 100, 100, 100, 100)]
    [InlineData(ResizeMode.Crop, 300, 50, 300, 50)]
    [InlineData(ResizeMode.Pad, 100, 100, 100, 100)]
    [InlineData(ResizeMode.Pad, 50, 0, 50, 25)]
    public void Resize_modes_produce_expected_size(ResizeMode mode, int width, int height, int expectedWidth, int expectedHeight)
    {
        using var image = Load(400, 200);

        image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(width, height), Mode = mode }));

        Assert.Equal(new Size(expectedWidth, expectedHeight), image.Size);
    }

    [Fact]
    public void Resize_with_width_and_height_stretches_and_zero_keeps_aspect()
    {
        using var image = Load(400, 200);

        image.Mutate(x => x.Resize(100, 100));
        Assert.Equal(new Size(100, 100), image.Size);

        image.Mutate(x => x.Resize(new Size(0, 50)));
        Assert.Equal(new Size(50, 50), image.Size);
    }

    [Fact]
    public void Default_mode_is_crop_centered_with_transparent_pad()
    {
        var options = new ResizeOptions();

        Assert.Equal(ResizeMode.Crop, options.Mode);
        Assert.Equal(AnchorPositionMode.Center, options.Position);
        Assert.Equal(Color.Transparent, options.PadColor);
    }

    [Fact]
    public void Pad_fills_the_corners_with_PadColor()
    {
        using var image = Load(400, 200);

        image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(200, 200), Mode = ResizeMode.Pad, PadColor = Color.FromRgb(255, 0, 255) }));

        Assert.Equal(new SKColor(255, 0, 255), image.Bitmap.GetPixel(0, 0));
        Assert.Equal(new SKColor(255, 0, 255), image.Bitmap.GetPixel(199, 199));
        AssertClose(SKColors.Red, image.Bitmap.GetPixel(10, 60));      // content spans y 50..150
        AssertClose(SKColors.White, image.Bitmap.GetPixel(190, 140));
    }

    [Theory]
    [InlineData(AnchorPositionMode.Top, 0, 150)]
    [InlineData(AnchorPositionMode.Bottom, 199, 50)]
    public void Pad_anchor_places_the_content(AnchorPositionMode position, int contentRow, int padRow)
    {
        using var image = Load(400, 200);

        image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(200, 200), Mode = ResizeMode.Pad, Position = position, PadColor = Color.Black }));

        Assert.NotEqual(SKColors.Black, image.Bitmap.GetPixel(10, contentRow));
        Assert.Equal(SKColors.Black, image.Bitmap.GetPixel(10, padRow));
    }

    [Theory]
    [InlineData(AnchorPositionMode.Center, 0x00, 0xFF, 0x00)]
    [InlineData(AnchorPositionMode.Left, 0xFF, 0x00, 0x00)]
    [InlineData(AnchorPositionMode.TopLeft, 0xFF, 0x00, 0x00)]
    [InlineData(AnchorPositionMode.Right, 0x00, 0x00, 0xFF)]
    [InlineData(AnchorPositionMode.BottomRight, 0x00, 0x00, 0xFF)]
    public void Crop_keeps_the_anchored_part(AnchorPositionMode position, byte r, byte g, byte b)
    {
        // 400x100: red | green | blue bands of 150, 100 and 150 pixels.
        using var image = LoadBands();

        image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(100, 100), Mode = ResizeMode.Crop, Position = position }));

        Assert.Equal(new Size(100, 100), image.Size);
        foreach (var (px, py) in new[] { (5, 5), (50, 50), (94, 94) })
        {
            AssertClose(new SKColor(r, g, b), image.Bitmap.GetPixel(px, py));
        }
    }

    [Fact]
    public void Large_downscale_does_not_alias()
    {
        // 1px black/white checkerboard: a correct filter averages it to mid gray; point sampling gives black or white.
        var info = new SKImageInfo(1000, 1000, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var checker = new SKBitmap(info);
        var pixels = checker.GetPixelSpan();
        for (var y = 0; y < 1000; y++)
        {
            for (var x = 0; x < 1000; x++)
            {
                var v = (byte)((x + y) % 2 == 0 ? 0 : 255);
                pixels.Slice(((y * 1000) + x) * 4, 4).Fill(v);
                pixels[(((y * 1000) + x) * 4) + 3] = 255;
            }
        }
        using var image = Image.Load(TestImageFactory.Encode(checker, SKEncodedImageFormat.Png));

        image.Mutate(x => x.Resize(37, 37));

        for (var y = 0; y < 37; y++)
        {
            for (var x = 0; x < 37; x++)
            {
                var red = image.Bitmap.GetPixel(x, y).Red;
                Assert.InRange(red, 110, 145);
            }
        }
    }

    [Fact]
    public void Upscale_works()
    {
        using var image = Load(40, 20);

        image.Mutate(x => x.Resize(400, 200));

        AssertClose(SKColors.Red, image.Bitmap.GetPixel(50, 50));
        AssertClose(SKColors.White, image.Bitmap.GetPixel(350, 150));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 10)]
    [InlineData(10, -1)]
    public void Invalid_sizes_throw(int width, int height)
    {
        using var image = Load(40, 20);

        Assert.ThrowsAny<ArgumentException>(() => image.Mutate(x => x.Resize(width, height)));
    }

    [Fact]
    public void GetCurrentSize_tracks_the_chain()
    {
        using var image = Load(400, 200);
        Size seen = default;

        image.Mutate(x => seen = x.Resize(100, 0).GetCurrentSize());

        Assert.Equal(new Size(100, 50), seen);
    }

    [Fact]
    public void Clone_leaves_the_original_unchanged()
    {
        using var image = Load(400, 200);

        using var thumb = image.Clone(x => x.Resize(40, 20));

        Assert.Equal(new Size(400, 200), image.Size);
        Assert.Equal(new Size(40, 20), thumb.Size);
    }

    [Fact]
    public void Failed_mutate_leaves_a_usable_image()
    {
        using var image = Load(400, 200);

        Assert.Throws<InvalidOperationException>(() => image.Mutate(x =>
        {
            x.Resize(100, 50);
            throw new InvalidOperationException("boom");
        }));

        Assert.Equal(new Size(100, 50), image.Size);
        image.SaveAsPng(new MemoryStream());
    }

    private static Image Load(int width, int height) => Image.Load(TestImageFactory.Png(width, height));

    private static Image LoadBands()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(400, 100, SKColorType.Rgba8888, SKAlphaType.Premul));
        bitmap.Erase(SKColors.Red, new SKRectI(0, 0, 150, 100));
        bitmap.Erase(SKColors.Lime, new SKRectI(150, 0, 250, 100));
        bitmap.Erase(SKColors.Blue, new SKRectI(250, 0, 400, 100));
        return Image.Load(TestImageFactory.Encode(bitmap, SKEncodedImageFormat.Png));
    }

    internal static void AssertClose(SKColor expected, SKColor actual, int tolerance = 12) =>
        Assert.True(
            Math.Abs(expected.Red - actual.Red) <= tolerance
            && Math.Abs(expected.Green - actual.Green) <= tolerance
            && Math.Abs(expected.Blue - actual.Blue) <= tolerance
            && Math.Abs(expected.Alpha - actual.Alpha) <= tolerance,
            $"Expected {expected}, got {actual}.");
}
