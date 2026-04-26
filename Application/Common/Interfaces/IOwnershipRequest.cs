namespace BlogApi.Application.Common.Interfaces;

/// <summary>
/// Marks a request as requiring ownership verification for a resource.
/// </summary>
public interface IOwnershipRequest
{
    Guid Id { get; }
}

/// <summary>
/// Marker interface specifically for Post ownership checks.
/// Commands implementing this will trigger Post.AuthorId verification in AuthorizationBehavior.
/// </summary>
public interface IPostOwnershipRequest : IOwnershipRequest { }
