namespace PixelForge;

/// <summary>
/// The input could not be decoded. Catch this to reject a bad upload; the derived types say why.
/// </summary>
public class ImageFormatException : Exception
{
    /// <summary>Initializes a new instance.</summary>
    public ImageFormatException()
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">The error message.</param>
    public ImageFormatException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public ImageFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
