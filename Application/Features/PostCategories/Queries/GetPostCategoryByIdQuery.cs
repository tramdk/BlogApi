using FloraCore.Domain.Entities;
using FloraCore.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace FloraCore.Application.Features.PostCategories.Queries;

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
        var category = await _repository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
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
