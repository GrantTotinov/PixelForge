using SkiaSharp;

namespace PixelForge;

// Encoders never write metadata: EXIF (including GPS), XMP and comments from the source are dropped.
public sealed partial class Image
{
    private const int DefaultQuality = 75;

    /// <summary>Encodes as JPEG. Transparent pixels become black.</summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="quality">Quality from 1 to 100.</param>
    public void SaveAsJpeg(Stream stream, int quality = DefaultQuality) => Write(stream, Encode(ImageFormat.Jpeg, quality, false));

    /// <summary>Encodes as JPEG to a file. Transparent pixels become black.</summary>
    /// <param name="path">The file path; an existing file is overwritten.</param>
    /// <param name="quality">Quality from 1 to 100.</param>
    public void SaveAsJpeg(string path, int quality = DefaultQuality) => Write(path, Encode(ImageFormat.Jpeg, quality, false));

    /// <summary>Encodes as JPEG and writes asynchronously. Transparent pixels become black.</summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="quality">Quality from 1 to 100.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the data is written.</returns>
    public Task SaveAsJpegAsync(Stream stream, int quality = DefaultQuality, CancellationToken cancellationToken = default) =>
        WriteAsync(stream, Encode(ImageFormat.Jpeg, quality, false), cancellationToken);

    /// <summary>Encodes as JPEG and writes a file asynchronously. Transparent pixels become black.</summary>
    /// <param name="path">The file path; an existing file is overwritten.</param>
    /// <param name="quality">Quality from 1 to 100.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the file is written.</returns>
    public Task SaveAsJpegAsync(string path, int quality = DefaultQuality, CancellationToken cancellationToken = default) =>
        WriteAsync(path, Encode(ImageFormat.Jpeg, quality, false), cancellationToken);

    /// <summary>Encodes as PNG.</summary>
    /// <param name="stream">The destination stream.</param>
    public void SaveAsPng(Stream stream) => Write(stream, Encode(ImageFormat.Png, DefaultQuality, false));

    /// <summary>Encodes as PNG to a file.</summary>
    /// <param name="path">The file path; an existing file is overwritten.</param>
    public void SaveAsPng(string path) => Write(path, Encode(ImageFormat.Png, DefaultQuality, false));

    /// <summary>Encodes as PNG and writes asynchronously.</summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the data is written.</returns>
    public Task SaveAsPngAsync(Stream stream, CancellationToken cancellationToken = default) =>
        WriteAsync(stream, Encode(ImageFormat.Png, DefaultQuality, false), cancellationToken);

    /// <summary>Encodes as PNG and writes a file asynchronously.</summary>
    /// <param name="path">The file path; an existing file is overwritten.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the file is written.</returns>
    public Task SaveAsPngAsync(string path, CancellationToken cancellationToken = default) =>
        WriteAsync(path, Encode(ImageFormat.Png, DefaultQuality, false), cancellationToken);

    /// <summary>Encodes as WebP.</summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="quality">Quality from 1 to 100; with <paramref name="lossless"/> it sets compression effort.</param>
    /// <param name="lossless">Encode losslessly.</param>
    public void SaveAsWebp(Stream stream, int quality = DefaultQuality, bool lossless = false) =>
        Write(stream, Encode(ImageFormat.Webp, quality, lossless));

    /// <summary>Encodes as WebP to a file.</summary>
    /// <param name="path">The file path; an existing file is overwritten.</param>
    /// <param name="quality">Quality from 1 to 100; with <paramref name="lossless"/> it sets compression effort.</param>
    /// <param name="lossless">Encode losslessly.</param>
    public void SaveAsWebp(string path, int quality = DefaultQuality, bool lossless = false) =>
        Write(path, Encode(ImageFormat.Webp, quality, lossless));

    /// <summary>Encodes as WebP and writes asynchronously.</summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="quality">Quality from 1 to 100; with <paramref name="lossless"/> it sets compression effort.</param>
    /// <param name="lossless">Encode losslessly.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the data is written.</returns>
    public Task SaveAsWebpAsync(Stream stream, int quality = DefaultQuality, bool lossless = false, CancellationToken cancellationToken = default) =>
        WriteAsync(stream, Encode(ImageFormat.Webp, quality, lossless), cancellationToken);

    /// <summary>Encodes as WebP and writes a file asynchronously.</summary>
    /// <param name="path">The file path; an existing file is overwritten.</param>
    /// <param name="quality">Quality from 1 to 100; with <paramref name="lossless"/> it sets compression effort.</param>
    /// <param name="lossless">Encode losslessly.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the file is written.</returns>
    public Task SaveAsWebpAsync(string path, int quality = DefaultQuality, bool lossless = false, CancellationToken cancellationToken = default) =>
        WriteAsync(path, Encode(ImageFormat.Webp, quality, lossless), cancellationToken);

    /// <summary>Encodes to a file in the format given by its extension (.jpg, .jpeg, .png, .webp) with default settings.</summary>
    /// <param name="path">The file path; an existing file is overwritten.</param>
    /// <exception cref="NotSupportedException">The extension is not an encodable format.</exception>
    public void Save(string path) => Write(path, Encode(FormatFromPath(path), DefaultQuality, false));

    /// <summary>Encodes to a file in the format given by its extension (.jpg, .jpeg, .png, .webp) with default settings.</summary>
    /// <param name="path">The file path; an existing file is overwritten.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the file is written.</returns>
    /// <exception cref="NotSupportedException">The extension is not an encodable format.</exception>
    public Task SaveAsync(string path, CancellationToken cancellationToken = default) =>
        WriteAsync(path, Encode(FormatFromPath(path), DefaultQuality, false), cancellationToken);

    /// <summary>Encodes in the given format with default settings.</summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="format"><see cref="ImageFormat.Jpeg"/>, <see cref="ImageFormat.Png"/> or <see cref="ImageFormat.Webp"/>.</param>
    /// <exception cref="NotSupportedException">The format cannot be encoded.</exception>
    public void Save(Stream stream, ImageFormat format) => Write(stream, Encode(format, DefaultQuality, false));

    /// <summary>Encodes in the given format with default settings and writes asynchronously.</summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="format"><see cref="ImageFormat.Jpeg"/>, <see cref="ImageFormat.Png"/> or <see cref="ImageFormat.Webp"/>.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the data is written.</returns>
    /// <exception cref="NotSupportedException">The format cannot be encoded.</exception>
    public Task SaveAsync(Stream stream, ImageFormat format, CancellationToken cancellationToken = default) =>
        WriteAsync(stream, Encode(format, DefaultQuality, false), cancellationToken);

    private SKData Encode(ImageFormat format, int quality, bool lossless)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quality, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quality, 100);

        // Pixels are already sRGB. Untagged output means sRGB, so drop the color space to avoid embedding an ICC profile.
        using var tagged = Bitmap.PeekPixels();
        using var pixmap = tagged.WithColorSpace(null!);
        var data = format switch
        {
            ImageFormat.Jpeg => pixmap.Encode(new SKJpegEncoderOptions(quality, SKJpegEncoderDownsample.Downsample420, SKJpegEncoderAlphaOption.Ignore)),
            ImageFormat.Png => pixmap.Encode(SKPngEncoderOptions.Default),
            ImageFormat.Webp => pixmap.Encode(new SKWebpEncoderOptions(lossless ? SKWebpEncoderCompression.Lossless : SKWebpEncoderCompression.Lossy, quality)),
            _ => throw new NotSupportedException($"Encoding to {format} is not supported. Use Jpeg, Png or Webp."),
        };
        return data ?? throw new InvalidOperationException($"SkiaSharp failed to encode the image as {format}.");
    }

    private static ImageFormat FormatFromPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return Path.GetExtension(path).ToUpperInvariant() switch
        {
            ".JPG" or ".JPEG" => ImageFormat.Jpeg,
            ".PNG" => ImageFormat.Png,
            ".WEBP" => ImageFormat.Webp,
            var extension => throw new NotSupportedException($"Cannot pick an encoder for extension '{extension}'. Use .jpg, .jpeg, .png or .webp."),
        };
    }

    private static void Write(Stream stream, SKData data)
    {
        using (data)
        {
            ArgumentNullException.ThrowIfNull(stream);
            stream.Write(data.AsSpan());
        }
    }

    private static void Write(string path, SKData data)
    {
        using (data)
        {
            using var file = File.Create(path);
            file.Write(data.AsSpan());
        }
    }

    private static async Task WriteAsync(Stream stream, SKData data, CancellationToken cancellationToken)
    {
        using (data)
        {
            ArgumentNullException.ThrowIfNull(stream);
            using var source = data.AsStream();
            await source.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task WriteAsync(string path, SKData data, CancellationToken cancellationToken)
    {
        var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await using (file.ConfigureAwait(false))
        {
            await WriteAsync(file, data, cancellationToken).ConfigureAwait(false);
        }
    }
}
