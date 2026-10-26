namespace PixelForge;

/// <summary>The format was recognized, but the image data is truncated or corrupt.</summary>
public class InvalidImageContentException : ImageFormatException
{
    /// <summary>Initializes a new instance.</summary>
    public InvalidImageContentException()
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">The error message.</param>
    public InvalidImageContentException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public InvalidImageContentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
