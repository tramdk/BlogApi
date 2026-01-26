using BlogApi.Domain.Entities;
using BlogApi.Application.Common.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using UUIDNext;

namespace BlogApi.Application.Features.ProductCategories.Commands;

public record CreateProductCategoryCommand(string Name, string Description, string? ImageUrl) : IRequest<Guid>;

public class CreateProductCategoryCommandHandler : IRequestHandler<CreateProductCategoryCommand, Guid>
{
    private readonly IGenericRepository<ProductCategory, Guid> _repository;

    public CreateProductCategoryCommandHandler(IGenericRepository<ProductCategory, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new ProductCategory
        {
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            Name = request.Name,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(category);
        return category.Id;
    }
}
