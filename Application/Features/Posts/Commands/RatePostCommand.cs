using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using MediatR;

namespace BlogApi.Application.Features.Posts.Commands;

public record RatePostCommand(Guid PostId, int Score) : IRequest<bool>;

public class RatePostHandler : IRequestHandler<RatePostCommand, bool>
{
    private readonly IGenericRepository<Post, Guid> _postRepository;
    public RatePostHandler(IGenericRepository<Post, Guid> postRepository) => _postRepository = postRepository;

    public async Task<bool> Handle(RatePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.PostId);
        if (post == null) return false;

        post.AddRating(request.Score);
        await _postRepository.UpdateAsync(post);
        return true;
    }
}
