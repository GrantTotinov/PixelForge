using SkiaSharp;

namespace PixelForge.Tests;

/// <summary>
/// Pins how SkiaSharp 4.153.1 actually behaves. PixelForge relies on each of these.
/// If a SkiaSharp upgrade breaks one, re-check the code that depends on it.
/// </summary>
public class SkiaSharpBehaviorTests
{
    [Fact]
    public void Codec_reads_size_and_format_from_header()
    {
        using var codec = SKCodec.Create(new MemoryStream(TestImageFactory.Png(64, 32)));

        Assert.Equal(64, codec.Info.Width);
        Assert.Equal(32, codec.Info.Height);
        Assert.Equal(SKEncodedImageFormat.Png, codec.EncodedFormat);
    }

    [Theory]
    [InlineData(23, SKCodecResult.IncompleteInput)]
    [InlineData(200, SKCodecResult.Unimplemented)]
    public void Codec_create_on_garbage_returns_null_and_a_result_code(int length, SKCodecResult expected)
    {
        var garbage = Enumerable.Repeat((byte)'x', length).ToArray();

        var codec = SKCodec.Create(new MemoryStream(garbage), out var result);

        Assert.Null(codec);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Codec_create_on_empty_stream_returns_null()
    {
        var codec = SKCodec.Create(new MemoryStream(), out var result);

        Assert.Null(codec);
        Assert.Equal(SKCodecResult.IncompleteInput, result);
    }

    [Fact]
    public void Codec_reports_huge_header_size_without_decoding_pixels()
    {
        var before = GC.GetAllocatedBytesForCurrentThread();

        using var codec = SKCodec.Create(new MemoryStream(TestImageFactory.PngHeaderOnly(50_000, 50_000)), out var result);

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(SKCodecResult.Success, result);
        Assert.Equal(50_000, codec.Info.Width);
        Assert.Equal(50_000, codec.Info.Height);
        Assert.True(allocated < 10_000_000, $"Allocated {allocated} managed bytes.");
    }

    [Theory]
    [InlineData(1, SKEncodedOrigin.TopLeft)]
    [InlineData(2, SKEncodedOrigin.TopRight)]
    [InlineData(3, SKEncodedOrigin.BottomRight)]
    [InlineData(4, SKEncodedOrigin.BottomLeft)]
    [InlineData(5, SKEncodedOrigin.LeftTop)]
    [InlineData(6, SKEncodedOrigin.RightTop)]
    [InlineData(7, SKEncodedOrigin.RightBottom)]
    [InlineData(8, SKEncodedOrigin.LeftBottom)]
    public void Codec_reads_exif_orientation(ushort orientation, SKEncodedOrigin expected)
    {
        var jpeg = TestImageFactory.WithExifOrientation(TestImageFactory.Jpeg(40, 20), orientation);

        using var codec = SKCodec.Create(new MemoryStream(jpeg));

        Assert.Equal(expected, codec.EncodedOrigin);
        Assert.Equal((int)expected, orientation);
        // Info stays in stored (unrotated) dimensions.
        Assert.Equal(40, codec.Info.Width);
    }

    [Fact]
    public void Codec_without_exif_reports_top_left()
    {
        using var codec = SKCodec.Create(new MemoryStream(TestImageFactory.Jpeg(40, 20)));

        Assert.Equal(SKEncodedOrigin.TopLeft, codec.EncodedOrigin);
    }

    [Theory]
    [InlineData(0.25f, 200, 150)]
    [InlineData(0.5f, 400, 300)]
    [InlineData(0.3f, 200, 150)]   // rounds to the nearest 1/8 step, so it can go below the requested scale
    [InlineData(0.35f, 300, 225)]
    [InlineData(0.37f, 300, 225)]
    [InlineData(0.1f, 100, 75)]    // never below 1/8
    [InlineData(1f, 800, 600)]
    public void Jpeg_codec_scales_in_eighths(float scale, int width, int height)
    {
        using var codec = SKCodec.Create(new MemoryStream(TestImageFactory.Jpeg(800, 600)));

        var size = codec.GetScaledDimensions(scale);

        Assert.Equal(new SKSizeI(width, height), size);
    }

    [Fact]
    public void Png_codec_does_not_scale()
    {
        using var codec = SKCodec.Create(new MemoryStream(TestImageFactory.Png(800, 600)));

        Assert.Equal(new SKSizeI(800, 600), codec.GetScaledDimensions(0.25f));
    }

    [Fact]
    public void Jpeg_codec_decodes_directly_at_scaled_size_as_rgba8888()
    {
        using var codec = SKCodec.Create(new MemoryStream(TestImageFactory.Jpeg(800, 600)));
        var info = new SKImageInfo(200, 150, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);

        var result = codec.GetPixels(info, bitmap.GetPixels());

        Assert.Equal(SKCodecResult.Success, result);
        AssertClose(SKColors.Red, bitmap.GetPixel(10, 10));
        AssertClose(SKColors.White, bitmap.GetPixel(190, 140));
    }

    [Fact]
    public void Codec_native_color_type_is_platform_specific()
    {
        using var codec = SKCodec.Create(new MemoryStream(TestImageFactory.Png(4, 4)));

        // N32 is BGRA on Windows and RGBA elsewhere, so PixelForge always asks for Rgba8888 explicitly.
        Assert.Equal(SKImageInfo.PlatformColorType, codec.Info.ColorType);
    }

    [Fact]
    public void Codec_disposes_the_managed_stream_it_was_given()
    {
        var stream = new MemoryStream(TestImageFactory.Png(4, 4));

        SKCodec.Create(stream)!.Dispose();

        Assert.False(stream.CanRead);
    }

    [Fact]
    public void Codec_accepts_a_non_seekable_stream()
    {
        using var stream = new NonSeekableStream(TestImageFactory.Png(16, 8));

        using var codec = SKCodec.Create(stream, out var result);

        Assert.Equal(SKCodecResult.Success, result);
        Assert.Equal(16, codec.Info.Width);
    }

    [Fact]
    public void Jpeg_quality_changes_output_size()
    {
        using var bitmap = TestImageFactory.Quadrants(256, 256);
        using var noise = AddNoise(bitmap);

        var low = EncodeJpeg(noise, 30);
        var high = EncodeJpeg(noise, 95);

        Assert.True(low < high, $"q30={low} q95={high}");
    }

    [Fact]
    public void Webp_lossless_round_trip_is_pixel_identical()
    {
        using var source = TestImageFactory.Quadrants(64, 48);
        using var noise = AddNoise(source);
        using var ms = new MemoryStream();

        Assert.True(noise.PeekPixels().Encode(ms, new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 100)));

        ms.Position = 0;
        using var codec = SKCodec.Create(ms);
        Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
        using var decoded = new SKBitmap(noise.Info);
        Assert.Equal(SKCodecResult.Success, codec.GetPixels(noise.Info, decoded.GetPixels()));
        Assert.Equal(noise.Bytes, decoded.Bytes);
    }

    [Fact]
    public void Png_encoder_options_produce_png()
    {
        using var bitmap = TestImageFactory.Quadrants(8, 8);
        using var ms = new MemoryStream();

        Assert.True(bitmap.PeekPixels().Encode(ms, SKPngEncoderOptions.Default));

        Assert.Equal([0x89, (byte)'P', (byte)'N', (byte)'G'], ms.ToArray()[..4]);
    }

    [Fact]
    public void Encode_with_format_enum_returns_null_for_unsupported_encoders()
    {
        using var bitmap = TestImageFactory.Quadrants(8, 8);

        Assert.Null(bitmap.Encode(SKEncodedImageFormat.Gif, 100));
        Assert.Null(bitmap.Encode(SKEncodedImageFormat.Bmp, 100));
    }

    [Theory]
    [MemberData(nameof(CubicResamplers))]
    public void Bitmap_resize_supports_cubic_resamplers(SKCubicResampler resampler)
    {
        using var source = TestImageFactory.Quadrants(400, 200);

        using var resized = source.Resize(new SKSizeI(100, 50), new SKSamplingOptions(resampler));

        Assert.NotNull(resized);
        Assert.Equal(new SKSizeI(100, 50), new SKSizeI(resized.Width, resized.Height));
        Assert.Equal(SKColorType.Rgba8888, resized.ColorType);
        AssertClose(SKColors.Red, resized.GetPixel(5, 5));
        AssertClose(SKColors.White, resized.GetPixel(95, 45));
    }

    public static TheoryData<SKCubicResampler> CubicResamplers => [SKCubicResampler.Mitchell, SKCubicResampler.CatmullRom];

    private static long EncodeJpeg(SKBitmap bitmap, int quality)
    {
        using var data = bitmap.PeekPixels().Encode(new SKJpegEncoderOptions(quality));
        Assert.NotNull(data);
        return data.Size;
    }

    private static SKBitmap AddNoise(SKBitmap source)
    {
        var copy = source.Copy();
        var random = new Random(42);
        var pixels = copy.GetPixelSpan();
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = (byte)random.Next(256);
        }
        return copy;
    }

    private static void AssertClose(SKColor expected, SKColor actual, int tolerance = 8)
    {
        Assert.True(
            Math.Abs(expected.Red - actual.Red) <= tolerance
            && Math.Abs(expected.Green - actual.Green) <= tolerance
            && Math.Abs(expected.Blue - actual.Blue) <= tolerance,
            $"Expected {expected}, got {actual}.");
    }
}
