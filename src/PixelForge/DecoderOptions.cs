namespace PixelForge;

/// <summary>Limits and hints for decoding. The defaults are safe for untrusted uploads.</summary>
public sealed class DecoderOptions
{
    private readonly long _maxPixels = 100_000_000;
    private readonly long _maxBytes = 256L * 1024 * 1024;

    /// <summary>Gets the default options.</summary>
    public static DecoderOptions Default { get; } = new();

    /// <summary>
    /// Gets the maximum width × height allowed. The check uses the file header and runs before any pixel memory is allocated.
    /// Defaults to 100,000,000 (about 10000 × 10000, 400 MB of RGBA pixels).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not positive.</exception>
    public long MaxPixels
    {
        get => _maxPixels;
        init => _maxPixels = Positive(value);
    }

    /// <summary>
    /// Gets the maximum encoded input size in bytes. Defaults to 256 MB. Use <see cref="long.MaxValue"/> to disable.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not positive.</exception>
    public long MaxBytes
    {
        get => _maxBytes;
        init => _maxBytes = Positive(value);
    }

    /// <summary>
    /// Gets an optional hint for the size you will resize to. JPEG and WebP are then decoded directly at a smaller size
    /// that still covers it, which is much faster for thumbnails. The image is never decoded smaller than this size
    /// (unless the original is smaller), so always resize afterwards. A zero width or height means "any".
    /// The size refers to the image after <c>AutoOrient()</c>.
    /// </summary>
    public Size? TargetSize { get; init; }

    private static long Positive(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        return value;
    }
}
