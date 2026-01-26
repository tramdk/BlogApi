using BlogApi.Domain.Entities;
using BlogApi.Application.Common.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.ProductCategories.Commands;

public record UpdateProductCategoryCommand(Guid Id, string Name, string Description, string? ImageUrl) : IRequest<bool>;

public class UpdateProductCategoryCommandHandler : IRequestHandler<UpdateProductCategoryCommand, bool>
{
    private readonly IGenericRepository<ProductCategory, Guid> _repository;

    public UpdateProductCategoryCommandHandler(IGenericRepository<ProductCategory, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(UpdateProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(request.Id);
        if (category == null) return false;

        category.Name = request.Name;
        category.Description = request.Description;
        category.ImageUrl = request.ImageUrl;
        category.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(category);
        return true;
    }
}
