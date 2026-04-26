using System;

namespace BlogApi.Application.Common.Interfaces;

/// <summary>
/// Provides information about the currently authenticated user.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Gets the ID of the currently authenticated user, or null if unauthenticated.</summary>
    Guid? UserId { get; }
}
