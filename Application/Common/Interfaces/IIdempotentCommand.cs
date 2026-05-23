namespace FloraCore.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands that require request idempotency.
/// </summary>
public interface IIdempotentCommand
{
    /// <summary>
    /// Gets the unique idempotency key for the request.
    /// </summary>
    string IdempotencyKey { get; }
}
