using FloraCore.Domain.Entities;
using FloraCore.Application.Common.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace FloraCore.Application.Features.ProductCategories.Commands;

public record CreateProductCategoryCommand(Guid? Id, string Name, string Description, string? ImageUrl) : IRequest<Guid>;

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
            Id = request.Id ?? Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(category);
        return category.Id;
    }
}
