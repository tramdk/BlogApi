using FloraCore.Application.Common.Interfaces;
using FloraCore.Domain.Entities;
using FloraCore.Domain.Exceptions;
using MediatR;

namespace FloraCore.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that enforces ownership authorization for requests
/// implementing <see cref="IOwnershipRequest"/>.
/// Currently supports Post ownership checks.
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IGenericRepository<Post, Guid> _postRepository;

    public AuthorizationBehavior(ICurrentUserService currentUserService, IGenericRepository<Post, Guid> postRepository)
    {
        _currentUserService = currentUserService;
        _postRepository = postRepository;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IOwnershipRequest ownershipRequest)
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedAccessException("User is not authenticated.");

            // Check Post ownership. Extend this block when other entities need ownership checks.
            if (request is IPostOwnershipRequest)
            {
                var post = await _postRepository.GetByIdAsync(ownershipRequest.Id)
                    ?? throw new EntityNotFoundException("Post", ownershipRequest.Id);

                if (post.AuthorId != userId)
                    throw new AccessDeniedException($"post '{ownershipRequest.Id}'");
            }
        }

        return await next();
    }
}
