using SkiaSharp;

namespace PixelForge.Tests;

public sealed class ImageSaveTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("pixelforge-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Theory]
    [InlineData(ImageFormat.Jpeg)]
    [InlineData(ImageFormat.Png)]
    [InlineData(ImageFormat.Webp)]
    public void Save_to_stream_round_trips_size_and_format(ImageFormat format)
    {
        using var image = Image.Load(TestImageFactory.Png(40, 20));
        var ms = new MemoryStream();

        image.Save(ms, format);

        Assert.Equal(new ImageInfo(40, 20, format), Image.Identify(ms.ToArray()));
    }

    [Fact]
    public void SaveAs_methods_write_their_format()
    {
        using var image = Image.Load(TestImageFactory.Png(40, 20));
        MemoryStream jpeg = new(), png = new(), webp = new();

        image.SaveAsJpeg(jpeg, 80);
        image.SaveAsPng(png);
        image.SaveAsWebp(webp, 80);

        Assert.Equal(ImageFormat.Jpeg, Image.Identify(jpeg.ToArray()).Format);
        Assert.Equal(ImageFormat.Png, Image.Identify(png.ToArray()).Format);
        Assert.Equal(ImageFormat.Webp, Image.Identify(webp.ToArray()).Format);
    }

    [Theory]
    [InlineData("a.jpg", ImageFormat.Jpeg)]
    [InlineData("a.JPEG", ImageFormat.Jpeg)]
    [InlineData("a.png", ImageFormat.Png)]
    [InlineData("a.webp", ImageFormat.Webp)]
    public async Task Save_by_extension_picks_the_encoder(string name, ImageFormat format)
    {
        using var image = Image.Load(TestImageFactory.Png(8, 8));
        var path = Path.Combine(_dir, name);
        var asyncPath = Path.Combine(_dir, "async-" + name);

        image.Save(path);
        await image.SaveAsync(asyncPath, TestContext.Current.CancellationToken);

        Assert.Equal(format, Image.Identify(path).Format);
        Assert.Equal(format, Image.Identify(asyncPath).Format);
    }

    [Theory]
    [InlineData("a.gif")]
    [InlineData("a.bmp")]
    [InlineData("noextension")]
    public void Save_rejects_unknown_extensions(string name)
    {
        using var image = Image.Load(TestImageFactory.Png(8, 8));

        Assert.Throws<NotSupportedException>(() => image.Save(Path.Combine(_dir, name)));
    }

    [Theory]
    [InlineData(ImageFormat.Gif)]
    [InlineData(ImageFormat.Bmp)]
    public void Save_rejects_formats_without_encoder(ImageFormat format)
    {
        using var image = Image.Load(TestImageFactory.Png(8, 8));

        Assert.Throws<NotSupportedException>(() => image.Save(new MemoryStream(), format));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Quality_must_be_1_to_100(int quality)
    {
        using var image = Image.Load(TestImageFactory.Png(8, 8));

        Assert.Throws<ArgumentOutOfRangeException>(() => image.SaveAsJpeg(new MemoryStream(), quality));
        Assert.Throws<ArgumentOutOfRangeException>(() => image.SaveAsWebp(new MemoryStream(), quality));
    }

    [Fact]
    public void Quality_changes_output_size()
    {
        using var image = Image.Load(TestImageFactory.Jpeg(256, 256));
        AddNoise(image.Bitmap);
        MemoryStream jpegLow = new(), jpegHigh = new(), webpLow = new(), webpHigh = new();

        image.SaveAsJpeg(jpegLow, 30);
        image.SaveAsJpeg(jpegHigh, 95);
        image.SaveAsWebp(webpLow, 30);
        image.SaveAsWebp(webpHigh, 95);

        Assert.True(jpegLow.Length < jpegHigh.Length);
        Assert.True(webpLow.Length < webpHigh.Length);
    }

    [Fact]
    public void Webp_lossless_round_trip_is_pixel_identical()
    {
        using var image = Image.Load(TestImageFactory.Png(64, 48));
        AddNoise(image.Bitmap);
        var ms = new MemoryStream();

        image.SaveAsWebp(ms, lossless: true);

        using var decoded = Image.Load(ms.ToArray());
        Assert.Equal(image.Bitmap.Bytes, decoded.Bitmap.Bytes);
    }

    [Fact]
    public async Task Async_overloads_write_the_same_bytes_as_sync()
    {
        using var image = Image.Load(TestImageFactory.Png(32, 32));
        var ct = TestContext.Current.CancellationToken;
        MemoryStream sync = new(), async = new();

        image.SaveAsJpeg(sync, 80);
        image.SaveAsPng(sync);
        image.SaveAsWebp(sync, 80, lossless: true);
        image.Save(sync, ImageFormat.Webp);
        await image.SaveAsJpegAsync(async, 80, ct);
        await image.SaveAsPngAsync(async, ct);
        await image.SaveAsWebpAsync(async, 80, lossless: true, ct);
        await image.SaveAsync(async, ImageFormat.Webp, ct);

        Assert.Equal(sync.ToArray(), async.ToArray());
    }

    [Fact]
    public async Task Path_overloads_write_files()
    {
        using var image = Image.Load(TestImageFactory.Png(16, 16));
        var ct = TestContext.Current.CancellationToken;
        string P(string name) => Path.Combine(_dir, name);

        image.SaveAsJpeg(P("1.out"));
        image.SaveAsPng(P("2.out"));
        image.SaveAsWebp(P("3.out"));
        await image.SaveAsJpegAsync(P("4.out"), cancellationToken: ct);
        await image.SaveAsPngAsync(P("5.out"), ct);
        await image.SaveAsWebpAsync(P("6.out"), cancellationToken: ct);

        ImageFormat[] expected = [ImageFormat.Jpeg, ImageFormat.Png, ImageFormat.Webp, ImageFormat.Jpeg, ImageFormat.Png, ImageFormat.Webp];
        Assert.Equal(expected, Enumerable.Range(1, 6).Select(i => Image.Identify(P($"{i}.out")).Format));
    }

    [Fact]
    public void Encoding_strips_exif_metadata()
    {
        var source = TestImageFactory.WithExifOrientation(TestImageFactory.Jpeg(40, 20), 6);
        Assert.True(ContainsAscii(source, "Exif"));
        using var image = Image.Load(source);
        var ms = new MemoryStream();

        image.SaveAsJpeg(ms);

        var output = ms.ToArray();
        Assert.False(ContainsAscii(output, "Exif"));
        Assert.False(ContainsAscii(output, "ICC_PROFILE"));
        using var codec = SKCodec.Create(new MemoryStream(output));
        Assert.Equal(SKEncodedOrigin.TopLeft, codec.EncodedOrigin);
    }

    private static bool ContainsAscii(byte[] data, string text) =>
        data.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes(text)) >= 0;

    private static void AddNoise(SKBitmap bitmap)
    {
        var random = new Random(42);
        var pixels = bitmap.GetPixelSpan();
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = (byte)random.Next(256);
        }
    }
}
