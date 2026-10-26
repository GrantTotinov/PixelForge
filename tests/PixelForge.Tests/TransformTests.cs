using PixelForge.Processing;
using SkiaSharp;

namespace PixelForge.Tests;

public class TransformTests
{
    private static readonly SKColor R = SKColors.Red, G = SKColors.Lime, B = SKColors.Blue, W = SKColors.White;

    [Fact]
    public void Crop_rectangle_keeps_that_area()
    {
        using var image = Load(40, 20);

        image.Mutate(x => x.Crop(new Rectangle(20, 10, 20, 10)));

        Assert.Equal(new Size(20, 10), image.Size);
        Assert.Equal(W, image.Bitmap.GetPixel(0, 0));
        Assert.Equal(W, image.Bitmap.GetPixel(19, 9));
    }

    [Fact]
    public void Crop_width_height_keeps_top_left()
    {
        using var image = Load(40, 20);

        image.Mutate(x => x.Crop(20, 10));

        Assert.Equal(new Size(20, 10), image.Size);
        Assert.Equal(R, image.Bitmap.GetPixel(19, 9));
    }

    [Theory]
    [InlineData(-1, 0, 10, 10)]
    [InlineData(0, 0, 0, 10)]
    [InlineData(35, 0, 10, 10)]
    [InlineData(0, 15, 10, 10)]
    public void Crop_outside_the_image_throws(int x, int y, int width, int height)
    {
        using var image = Load(40, 20);

        Assert.Throws<ArgumentOutOfRangeException>(() => image.Mutate(c => c.Crop(new Rectangle(x, y, width, height))));
    }

    [Theory]
    [InlineData(RotateMode.Rotate90, 20, 40, new[] { 2, 0, 3, 1 })]
    [InlineData(RotateMode.Rotate180, 40, 20, new[] { 3, 2, 1, 0 })]
    [InlineData(RotateMode.Rotate270, 20, 40, new[] { 1, 3, 0, 2 })]
    [InlineData(RotateMode.None, 40, 20, new[] { 0, 1, 2, 3 })]
    public void Rotate_by_mode_and_by_degrees_agree(RotateMode mode, int width, int height, int[] corners)
    {
        SKColor[] colors = [R, G, B, W];
        using var byMode = Load(40, 20);
        using var byDegrees = Load(40, 20);

        byMode.Mutate(x => x.Rotate(mode));
        byDegrees.Mutate(x => x.Rotate((int)mode * 90f));

        Assert.Equal(new Size(width, height), byMode.Size);
        OrientationTests.AssertCorners(byMode, [.. corners.Select(i => colors[i])]);
        Assert.Equal(byMode.Bitmap.Bytes, byDegrees.Bitmap.Bytes);
    }

    [Fact]
    public void Negative_and_large_angles_normalize()
    {
        using var a = Load(40, 20);
        using var b = Load(40, 20);

        a.Mutate(x => x.Rotate(-90));
        b.Mutate(x => x.Rotate(630));

        using var expected = Load(40, 20);
        expected.Mutate(x => x.Rotate(RotateMode.Rotate270));
        Assert.Equal(expected.Bitmap.Bytes, a.Bitmap.Bytes);
        Assert.Equal(expected.Bitmap.Bytes, b.Bitmap.Bytes);
    }

    [Fact]
    public void Four_quarter_turns_are_lossless()
    {
        using var image = Load(7, 5);
        var original = image.Bitmap.Bytes;

        image.Mutate(x => x.Rotate(90).Rotate(90).Rotate(90).Rotate(90));

        Assert.Equal(original, image.Bitmap.Bytes);
    }

    [Fact]
    public void Arbitrary_rotation_grows_canvas_and_fills_corners()
    {
        using var image = Load(40, 20);

        image.Mutate(x => x.Rotate(45, Color.FromRgb(255, 0, 255)));

        // 40*cos45 + 20*sin45 = 42.43
        Assert.Equal(new Size(43, 43), image.Size);
        Assert.Equal(new SKColor(255, 0, 255), image.Bitmap.GetPixel(0, 0));
        Assert.Equal(new SKColor(255, 0, 255), image.Bitmap.GetPixel(42, 42));
    }

    [Fact]
    public void Arbitrary_rotation_defaults_to_transparent_corners()
    {
        using var image = Load(40, 20);

        image.Mutate(x => x.Rotate(30));

        Assert.Equal(0, image.Bitmap.GetPixel(0, 0).Alpha);
        Assert.Equal(255, image.Bitmap.GetPixel(image.Width / 2, image.Height / 2).Alpha);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void Rotate_rejects_non_finite_angles(float degrees)
    {
        using var image = Load(4, 4);

        Assert.Throws<ArgumentOutOfRangeException>(() => image.Mutate(x => x.Rotate(degrees)));
    }

    [Theory]
    [InlineData(FlipMode.Horizontal, new[] { 1, 0, 3, 2 })]
    [InlineData(FlipMode.Vertical, new[] { 2, 3, 0, 1 })]
    [InlineData(FlipMode.None, new[] { 0, 1, 2, 3 })]
    public void Flip_mirrors(FlipMode mode, int[] corners)
    {
        SKColor[] colors = [R, G, B, W];
        using var image = Load(40, 20);

        image.Mutate(x => x.Flip(mode));

        OrientationTests.AssertCorners(image, [.. corners.Select(i => colors[i])]);
    }

    [Fact]
    public void Grayscale_uses_bt709_weights_and_keeps_alpha()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(3, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        bitmap.SetPixel(0, 0, SKColors.Red);
        bitmap.SetPixel(1, 0, SKColors.Lime);
        bitmap.SetPixel(2, 0, new SKColor(0, 0, 255, 128));
        using var image = Image.Load(TestImageFactory.Encode(bitmap, SKEncodedImageFormat.Png));

        image.Mutate(x => x.Grayscale());

        AssertGray(54, 255, image.Bitmap.GetPixel(0, 0));
        AssertGray(182, 255, image.Bitmap.GetPixel(1, 0));
        AssertGray(18, 128, image.Bitmap.GetPixel(2, 0));
    }

    private static void AssertGray(byte value, byte alpha, SKColor actual)
    {
        Assert.True(actual.Red == actual.Green && actual.Green == actual.Blue, $"Not gray: {actual}");
        Assert.InRange(actual.Red, value - 2, value + 2);
        Assert.InRange(actual.Alpha, alpha - 1, alpha + 1);
    }

    private static Image Load(int width, int height) => Image.Load(TestImageFactory.Png(width, height));
}
