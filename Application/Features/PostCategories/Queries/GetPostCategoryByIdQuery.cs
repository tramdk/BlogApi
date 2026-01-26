using BlogApi.Domain.Entities;
using BlogApi.Application.Common.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.PostCategories.Queries;

public record GetPostCategoryByIdQuery(string Id) : IRequest<PostCategoryDto?>;

public class GetPostCategoryByIdQueryHandler : IRequestHandler<GetPostCategoryByIdQuery, PostCategoryDto?>
{
    private readonly IGenericRepository<PostCategory, string> _repository;

    public GetPostCategoryByIdQueryHandler(IGenericRepository<PostCategory, string> repository)
    {
        _repository = repository;
    }

    public async Task<PostCategoryDto?> Handle(GetPostCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(request.Id);
        if (category == null) return null;

        return new PostCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}
