namespace FloraCore.Domain.Exceptions;

/// <summary>
/// Exception thrown when a JWT token is invalid.
/// </summary>
public class InvalidJwtTokenException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidJwtTokenException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidJwtTokenException(string message) : base("INVALID_JWT_TOKEN", message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidJwtTokenException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public InvalidJwtTokenException(string message, Exception innerException) : base("INVALID_JWT_TOKEN", message, innerException)
    {
    }
}
