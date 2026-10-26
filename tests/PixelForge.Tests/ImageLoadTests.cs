using SkiaSharp;

namespace PixelForge.Tests;

public class ImageLoadTests
{
    public static TheoryData<string, byte[], int, int, ImageFormat> AllFormats => new()
    {
        { "png", TestImageFactory.Png(40, 20), 40, 20, ImageFormat.Png },
        { "jpeg", TestImageFactory.Jpeg(40, 20), 40, 20, ImageFormat.Jpeg },
        { "webp", TestImageFactory.Webp(40, 20), 40, 20, ImageFormat.Webp },
        { "gif", TestImageFactory.TwoFrameGif(), 1, 1, ImageFormat.Gif },
        { "bmp", TestImageFactory.Bmp(5, 3, SKColors.Orange), 5, 3, ImageFormat.Bmp },
        { "ico", TestImageFactory.Ico(32), 32, 32, ImageFormat.Ico },
    };

    [Theory]
    [MemberData(nameof(AllFormats))]
    public void Load_and_Identify_handle_every_supported_format(string name, byte[] data, int width, int height, ImageFormat format)
    {
        using var image = Image.Load(data);

        Assert.True(image.Width == width && image.Height == height, $"{name}: {image.Size}");
        Assert.Equal(SKColorType.Rgba8888, image.Bitmap.ColorType);
        Assert.Equal(new ImageInfo(width, height, format), Image.Identify(data));
    }

    [Fact]
    public void Load_bmp_keeps_colors()
    {
        using var image = Image.Load(TestImageFactory.Bmp(5, 3, SKColors.Orange));

        Assert.Equal(SKColors.Orange, image.Bitmap.GetPixel(4, 2));
    }

    [Fact]
    public void Load_gif_decodes_first_frame_only()
    {
        using var image = Image.Load(TestImageFactory.TwoFrameGif());

        Assert.Equal(SKColors.White, image.Bitmap.GetPixel(0, 0));
    }

    [Fact]
    public void All_input_overloads_decode()
    {
        var data = TestImageFactory.Png(8, 4);
        var path = Path.Combine(Path.GetTempPath(), $"pixelforge-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, data);
        try
        {
            using var fromPath = Image.Load(path);
            using var fromStream = Image.Load(new MemoryStream(data));
            using var fromBytes = Image.Load(data);
            using var fromSpan = Image.Load(data.AsSpan());

            Assert.All(new[] { fromPath, fromStream, fromBytes, fromSpan }, i => Assert.Equal(new Size(8, 4), i.Size));
            Assert.Equal(ImageFormat.Png, Image.Identify(path).Format);
            Assert.Equal(ImageFormat.Png, Image.Identify(new MemoryStream(data)).Format);
            Assert.Equal(ImageFormat.Png, Image.Identify(data.AsSpan()).Format);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadAsync_decodes_and_leaves_stream_open()
    {
        var stream = new MemoryStream(TestImageFactory.Jpeg(30, 10));

        using var image = await Image.LoadAsync(stream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new Size(30, 10), image.Size);
        Assert.True(stream.CanRead);
    }

    [Fact]
    public void Load_leaves_caller_stream_open()
    {
        var stream = new MemoryStream(TestImageFactory.Png(4, 4));

        Image.Load(stream).Dispose();

        Assert.True(stream.CanRead);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(500)]
    public void Load_garbage_throws_UnknownImageFormatException(int length)
    {
        var garbage = Enumerable.Repeat((byte)'x', length).ToArray();

        Assert.Throws<UnknownImageFormatException>(() => Image.Load(garbage));
        Assert.Throws<UnknownImageFormatException>(() => Image.Identify(garbage));
    }

    [Fact]
    public void Load_truncated_jpeg_throws_InvalidImageContentException()
    {
        var jpeg = TestImageFactory.Jpeg(200, 200);

        Assert.Throws<InvalidImageContentException>(() => Image.Load(jpeg.AsSpan(0, jpeg.Length / 2)));
    }

    [Fact]
    public void All_decode_errors_share_a_base_type()
    {
        Assert.Throws<UnknownImageFormatException>(() => Image.Load("nope"u8));
        Assert.IsAssignableFrom<ImageFormatException>(new UnknownImageFormatException());
        Assert.IsAssignableFrom<ImageFormatException>(new InvalidImageContentException());
        Assert.IsAssignableFrom<ImageFormatException>(new ImageTooLargeException());
    }

    [Fact]
    public void Load_rejects_huge_header_before_allocating()
    {
        var bomb = TestImageFactory.PngHeaderOnly(50_000, 50_000);
        var before = GC.GetAllocatedBytesForCurrentThread();

        var ex = Assert.Throws<ImageTooLargeException>(() => Image.Load(bomb));

        // A decode would need 10 GB of native memory; throwing here proves the check ran first.
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 10_000_000);
        Assert.Contains("50000x50000", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Identify_reports_huge_header_without_throwing()
    {
        var info = Image.Identify(TestImageFactory.PngHeaderOnly(50_000, 50_000));

        Assert.Equal(new Size(50_000, 50_000), info.Size);
    }

    [Fact]
    public void MaxPixels_is_configurable()
    {
        var png = TestImageFactory.Png(100, 100);

        Assert.Throws<ImageTooLargeException>(() => Image.Load(png, new DecoderOptions { MaxPixels = 9_999 }));
        Image.Load(png, new DecoderOptions { MaxPixels = 10_000 }).Dispose();
    }

    [Fact]
    public void MaxBytes_applies_to_every_input()
    {
        var png = TestImageFactory.Png(64, 64);
        var options = new DecoderOptions { MaxBytes = png.Length - 1 };
        var path = Path.Combine(Path.GetTempPath(), $"pixelforge-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, png);
        try
        {
            Assert.Throws<ImageTooLargeException>(() => Image.Load(png, options));
            Assert.Throws<ImageTooLargeException>(() => Image.Load(png.AsSpan(), options));
            Assert.Throws<ImageTooLargeException>(() => Image.Load(path, options));
            Assert.Throws<ImageTooLargeException>(() => Image.Load(new MemoryStream(png), options));
            Assert.Throws<ImageTooLargeException>(() => Image.Identify(new MemoryStream(png), options));
            Image.Load(png, new DecoderOptions { MaxBytes = png.Length }).Dispose();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task MaxBytes_applies_to_non_seekable_async_streams()
    {
        var png = TestImageFactory.Png(64, 64);
        var options = new DecoderOptions { MaxBytes = png.Length - 1 };

        await Assert.ThrowsAsync<ImageTooLargeException>(
            () => Image.LoadAsync(new NonSeekableStream(png), options, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Limits_must_be_positive(long value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecoderOptions { MaxPixels = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecoderOptions { MaxBytes = value });
    }

    [Theory]
    [InlineData(200, 150, 200, 150)]    // 1/4 of 800x600
    [InlineData(201, 150, 300, 225)]    // 1/4 is too small, 3/8 covers it
    [InlineData(100, 0, 100, 75)]       // height unconstrained
    [InlineData(900, 900, 800, 600)]    // bigger than the image: full size
    public void TargetSize_decodes_jpeg_at_the_smallest_covering_scale(int targetWidth, int targetHeight, int width, int height)
    {
        var options = new DecoderOptions { TargetSize = new Size(targetWidth, targetHeight) };

        using var image = Image.Load(TestImageFactory.Jpeg(800, 600), options);

        Assert.Equal(new Size(width, height), image.Size);
    }

    [Fact]
    public void TargetSize_accounts_for_rotated_exif_orientation()
    {
        // Stored 800x600, displayed 600x800 after AutoOrient.
        var jpeg = TestImageFactory.WithExifOrientation(TestImageFactory.Jpeg(800, 600), 6);

        using var image = Image.Load(jpeg, new DecoderOptions { TargetSize = new Size(150, 200) });

        Assert.Equal(new Size(200, 150), image.Size);
    }

    [Fact]
    public void TargetSize_is_ignored_for_png()
    {
        using var image = Image.Load(TestImageFactory.Png(800, 600), new DecoderOptions { TargetSize = new Size(100, 100) });

        Assert.Equal(new Size(800, 600), image.Size);
    }

    [Fact]
    public void Disposed_image_throws()
    {
        var image = Image.Load(TestImageFactory.Png(4, 4));
        image.Dispose();

        Assert.Throws<ObjectDisposedException>(() => image.Width);
        image.Dispose();
    }
}
