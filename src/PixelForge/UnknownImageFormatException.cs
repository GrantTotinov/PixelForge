namespace PixelForge;

/// <summary>The input is not an image in a supported format.</summary>
public class UnknownImageFormatException : ImageFormatException
{
    /// <summary>Initializes a new instance.</summary>
    public UnknownImageFormatException()
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">The error message.</param>
    public UnknownImageFormatException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public UnknownImageFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
