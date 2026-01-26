using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using MediatR;

namespace BlogApi.Application.Common.Behaviors;

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
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException();
            }

            // Since our IOwnershipRequest is currently only for Posts, we check IGenericRepository<Post, Guid>
            // In a more complex app, we might use a factory or switch based on request type
            var post = await _postRepository.GetByIdAsync(ownershipRequest.Id);
            
            if (post != null && post.AuthorId != userId)
            {
                throw new UnauthorizedAccessException("You are not the owner of this post.");
            }
        }

        return await next();
    }
}
