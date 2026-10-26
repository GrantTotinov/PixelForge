namespace PixelForge;

/// <summary>
/// The input exceeds <see cref="DecoderOptions.MaxPixels"/> or <see cref="DecoderOptions.MaxBytes"/>.
/// Thrown before pixel memory is allocated.
/// </summary>
public class ImageTooLargeException : ImageFormatException
{
    /// <summary>Initializes a new instance.</summary>
    public ImageTooLargeException()
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">The error message.</param>
    public ImageTooLargeException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public ImageTooLargeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
