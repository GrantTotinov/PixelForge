using SkiaSharp;

namespace PixelForge;

public sealed partial class Image
{
    /// <summary>Decodes an image file.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Limits and hints; <see cref="DecoderOptions.Default"/> when null.</param>
    /// <returns>The decoded image.</returns>
    /// <exception cref="UnknownImageFormatException">The file is not a supported image.</exception>
    /// <exception cref="InvalidImageContentException">The image data is truncated or corrupt.</exception>
    /// <exception cref="ImageTooLargeException">The file exceeds the configured limits.</exception>
    public static Image Load(string path, DecoderOptions? options = null)
    {
        options ??= DecoderOptions.Default;
        return Decode(OpenFile(path, options), options);
    }

    /// <summary>Decodes an image from a stream. The stream is read to the end and left open.</summary>
    /// <param name="stream">The stream; it does not need to be seekable.</param>
    /// <param name="options">Limits and hints; <see cref="DecoderOptions.Default"/> when null.</param>
    /// <returns>The decoded image.</returns>
    /// <exception cref="UnknownImageFormatException">The input is not a supported image.</exception>
    /// <exception cref="InvalidImageContentException">The image data is truncated or corrupt.</exception>
    /// <exception cref="ImageTooLargeException">The input exceeds the configured limits.</exception>
    public static Image Load(Stream stream, DecoderOptions? options = null)
    {
        options ??= DecoderOptions.Default;
        return Decode(ReadAll(stream, options), options);
    }

    /// <summary>Decodes an image from bytes.</summary>
    /// <param name="data">The encoded image.</param>
    /// <param name="options">Limits and hints; <see cref="DecoderOptions.Default"/> when null.</param>
    /// <returns>The decoded image.</returns>
    /// <exception cref="UnknownImageFormatException">The input is not a supported image.</exception>
    /// <exception cref="InvalidImageContentException">The image data is truncated or corrupt.</exception>
    /// <exception cref="ImageTooLargeException">The input exceeds the configured limits.</exception>
    public static Image Load(byte[] data, DecoderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        options ??= DecoderOptions.Default;
        CheckBytes(data.Length, options);
        return Decode(new MemoryStream(data, writable: false), options);
    }

    /// <summary>Decodes an image from bytes.</summary>
    /// <param name="data">The encoded image.</param>
    /// <param name="options">Limits and hints; <see cref="DecoderOptions.Default"/> when null.</param>
    /// <returns>The decoded image.</returns>
    /// <exception cref="UnknownImageFormatException">The input is not a supported image.</exception>
    /// <exception cref="InvalidImageContentException">The image data is truncated or corrupt.</exception>
    /// <exception cref="ImageTooLargeException">The input exceeds the configured limits.</exception>
    public static Image Load(ReadOnlySpan<byte> data, DecoderOptions? options = null)
    {
        options ??= DecoderOptions.Default;
        CheckBytes(data.Length, options);
        return Decode(new MemoryStream(data.ToArray(), writable: false), options);
    }

    /// <summary>Reads a stream asynchronously and decodes the image. Use this for ASP.NET Core request bodies.</summary>
    /// <param name="stream">The stream; it is read to the end and left open.</param>
    /// <param name="options">Limits and hints; <see cref="DecoderOptions.Default"/> when null.</param>
    /// <param name="cancellationToken">Cancels reading the stream.</param>
    /// <returns>The decoded image.</returns>
    /// <exception cref="UnknownImageFormatException">The input is not a supported image.</exception>
    /// <exception cref="InvalidImageContentException">The image data is truncated or corrupt.</exception>
    /// <exception cref="ImageTooLargeException">The input exceeds the configured limits.</exception>
    public static async Task<Image> LoadAsync(Stream stream, DecoderOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= DecoderOptions.Default;
        return Decode(await ReadAllAsync(stream, options, cancellationToken).ConfigureAwait(false), options);
    }

    /// <summary>Reads size and format from the file header without decoding pixels.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>The image info.</returns>
    /// <exception cref="UnknownImageFormatException">The file is not a supported image.</exception>
    public static ImageInfo Identify(string path) => IdentifyCore(OpenFile(path, null));

    /// <summary>Reads size and format without decoding pixels. The stream is read to the end and left open.</summary>
    /// <param name="stream">The stream.</param>
    /// <param name="options">Limits; only <see cref="DecoderOptions.MaxBytes"/> applies.</param>
    /// <returns>The image info.</returns>
    /// <exception cref="UnknownImageFormatException">The input is not a supported image.</exception>
    /// <exception cref="ImageTooLargeException">The input exceeds <see cref="DecoderOptions.MaxBytes"/>.</exception>
    public static ImageInfo Identify(Stream stream, DecoderOptions? options = null) =>
        IdentifyCore(ReadAll(stream, options ?? DecoderOptions.Default));

    /// <summary>Reads size and format without decoding pixels.</summary>
    /// <param name="data">The encoded image.</param>
    /// <returns>The image info.</returns>
    /// <exception cref="UnknownImageFormatException">The input is not a supported image.</exception>
    public static ImageInfo Identify(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return IdentifyCore(new MemoryStream(data, writable: false));
    }

    /// <summary>Reads size and format without decoding pixels.</summary>
    /// <param name="data">The encoded image.</param>
    /// <returns>The image info.</returns>
    /// <exception cref="UnknownImageFormatException">The input is not a supported image.</exception>
    public static ImageInfo Identify(ReadOnlySpan<byte> data) => IdentifyCore(new MemoryStream(data.ToArray(), writable: false));

    private static ImageInfo IdentifyCore(Stream stream)
    {
        using (stream)
        {
            using var codec = CreateCodec(stream);
            return new ImageInfo(codec.Info.Width, codec.Info.Height, ToImageFormat(codec.EncodedFormat));
        }
    }

    private static Image Decode(Stream stream, DecoderOptions options)
    {
        using (stream)
        {
            using var codec = CreateCodec(stream);
            ToImageFormat(codec.EncodedFormat);

            var stored = codec.Info;
            if ((long)stored.Width * stored.Height > options.MaxPixels)
            {
                throw new ImageTooLargeException(
                    $"Image is {stored.Width}x{stored.Height} pixels, above the limit of {options.MaxPixels}. See DecoderOptions.MaxPixels.");
            }

            var size = DecodeSize(codec, options.TargetSize);
            var info = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb());
            var bitmap = new SKBitmap(info);
            try
            {
                var pixels = bitmap.GetPixels();
                if (pixels == IntPtr.Zero)
                {
                    throw new InsufficientMemoryException($"Could not allocate {info.BytesSize64} bytes for a {size.Width}x{size.Height} image.");
                }

                var result = codec.GetPixels(info, pixels);
                if (result != SKCodecResult.Success)
                {
                    throw new InvalidImageContentException($"Failed to decode {codec.EncodedFormat} image: {result}.");
                }

                return new Image(bitmap, codec.EncodedOrigin);
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }
    }

    /// <summary>Picks the smallest native decoder scale that still covers the target size.</summary>
    private static SKSizeI DecodeSize(SKCodec codec, Size? targetSize)
    {
        if (targetSize is not { } target)
        {
            return codec.Info.Size;
        }

        // EXIF orientations 5-8 store the image rotated by 90 degrees.
        var (width, height) = codec.EncodedOrigin >= SKEncodedOrigin.LeftTop ? (target.Height, target.Width) : (target.Width, target.Height);

        // GetScaledDimensions rounds to the nearest supported scale, so ask for exact eighths.
        for (var eighths = 1; eighths < 8; eighths++)
        {
            var scaled = codec.GetScaledDimensions(eighths / 8f);
            if (scaled.Width >= width && scaled.Height >= height)
            {
                return scaled;
            }
        }

        return codec.Info.Size;
    }

    private static SKCodec CreateCodec(Stream stream)
    {
        var codec = SKCodec.Create(stream, out var result);
        return codec ?? throw new UnknownImageFormatException($"Input is not a recognized image format ({result}).");
    }

    private static ImageFormat ToImageFormat(SKEncodedImageFormat format) => format switch
    {
        SKEncodedImageFormat.Png => ImageFormat.Png,
        SKEncodedImageFormat.Jpeg => ImageFormat.Jpeg,
        SKEncodedImageFormat.Webp => ImageFormat.Webp,
        SKEncodedImageFormat.Gif => ImageFormat.Gif,
        SKEncodedImageFormat.Bmp => ImageFormat.Bmp,
        SKEncodedImageFormat.Ico => ImageFormat.Ico,
        SKEncodedImageFormat.Wbmp => ImageFormat.Wbmp,
        _ => throw new UnknownImageFormatException($"Image format {format} is not supported."),
    };

    private static FileStream OpenFile(string path, DecoderOptions? options)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (options is not null)
        {
            CheckBytes(new FileInfo(path).Length, options);
        }

        return File.OpenRead(path);
    }

    private static void CheckBytes(long length, DecoderOptions options)
    {
        if (length > options.MaxBytes)
        {
            throw new ImageTooLargeException($"Input is larger than {options.MaxBytes} bytes. See DecoderOptions.MaxBytes.");
        }
    }

    // Trade-off: buffers the whole encoded input (bounded by MaxBytes). A non-owning, counting stream wrapper would avoid
    // the copy, but SKCodec disposes its stream and ASP.NET bodies forbid sync reads, so buffering keeps both cases simple.
    private static MemoryStream ReadAll(Stream stream, DecoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = stream.Read(chunk)) > 0)
        {
            Append(buffer, chunk, read, options);
        }

        buffer.Position = 0;
        return buffer;
    }

    private static async Task<MemoryStream> ReadAllAsync(Stream stream, DecoderOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
        {
            Append(buffer, chunk, read, options);
        }

        buffer.Position = 0;
        return buffer;
    }

    private static void Append(MemoryStream buffer, byte[] chunk, int count, DecoderOptions options)
    {
        CheckBytes(buffer.Length + count, options);
        buffer.Write(chunk, 0, count);
    }
}
