using SkiaSharp;

namespace PixelForge.Tests;

public class OrientationTests
{
    private static readonly SKColor R = SKColors.Red, G = SKColors.Lime, B = SKColors.Blue, W = SKColors.White;

    // Stored image: red top-left, green top-right, blue bottom-left, white bottom-right (40x20).
    // Expected display corners follow the EXIF definition of where stored row 0 and column 0 go.
    public static TheoryData<ushort, int, int, SKColor[]> Orientations => new()
    {
        { 1, 40, 20, [R, G, B, W] },
        { 2, 40, 20, [G, R, W, B] },
        { 3, 40, 20, [W, B, G, R] },
        { 4, 40, 20, [B, W, R, G] },
        { 5, 20, 40, [R, B, G, W] },
        { 6, 20, 40, [B, R, W, G] },
        { 7, 20, 40, [W, G, B, R] },
        { 8, 20, 40, [G, W, R, B] },
    };

    [Theory]
    [MemberData(nameof(Orientations))]
    public void AutoOrient_applies_every_exif_orientation(ushort orientation, int width, int height, SKColor[] corners)
    {
        var jpeg = TestImageFactory.WithExifOrientation(TestImageFactory.Jpeg(40, 20, quality: 100), orientation);
        using var image = Image.Load(jpeg);

        image.Mutate(x => x.AutoOrient());

        Assert.Equal(new Size(width, height), image.Size);
        AssertCorners(image, corners);
    }

    [Fact]
    public void AutoOrient_runs_once()
    {
        var jpeg = TestImageFactory.WithExifOrientation(TestImageFactory.Jpeg(40, 20, quality: 100), 6);
        using var image = Image.Load(jpeg);

        image.Mutate(x => x.AutoOrient().AutoOrient());
        image.Mutate(x => x.AutoOrient());

        Assert.Equal(new Size(20, 40), image.Size);
    }

    [Fact]
    public void Clone_keeps_pending_orientation()
    {
        var jpeg = TestImageFactory.WithExifOrientation(TestImageFactory.Jpeg(40, 20, quality: 100), 6);
        using var image = Image.Load(jpeg);

        using var copy = image.Clone(x => x.Resize(80, 40));
        copy.Mutate(x => x.AutoOrient());

        Assert.Equal(new Size(40, 80), copy.Size);
    }

    [Fact]
    public void AutoOrient_without_exif_is_a_no_op()
    {
        var png = TestImageFactory.Png(7, 5);
        using var image = Image.Load(png);
        var before = image.Bitmap.Bytes;

        image.Mutate(x => x.AutoOrient());

        Assert.Equal(before, image.Bitmap.Bytes);
    }

    internal static void AssertCorners(Image image, SKColor[] corners)
    {
        int w = image.Width, h = image.Height;
        ResizeTests.AssertClose(corners[0], image.Bitmap.GetPixel(w / 4, h / 4));
        ResizeTests.AssertClose(corners[1], image.Bitmap.GetPixel(3 * w / 4, h / 4));
        ResizeTests.AssertClose(corners[2], image.Bitmap.GetPixel(w / 4, 3 * h / 4));
        ResizeTests.AssertClose(corners[3], image.Bitmap.GetPixel(3 * w / 4, 3 * h / 4));
    }
}
