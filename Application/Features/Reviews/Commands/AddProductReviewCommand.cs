using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UUIDNext;

namespace BlogApi.Application.Features.Reviews.Commands;

public record AddProductReviewCommand(Guid ProductId, int Rating, string Comment) : IRequest<Unit>;

public class AddProductReviewCommandHandler : IRequestHandler<AddProductReviewCommand, Unit>
{
    private readonly IGenericRepository<Product, Guid> _productRepository;
    private readonly IGenericRepository<ProductReview, Guid> _reviewRepository;
    private readonly ICurrentUserService _currentUserService;

    public AddProductReviewCommandHandler(
        IGenericRepository<Product, Guid> productRepository,
        IGenericRepository<ProductReview, Guid> reviewRepository,
        ICurrentUserService currentUserService)
    {
        _productRepository = productRepository;
        _reviewRepository = reviewRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(AddProductReviewCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new Exception("User not authenticated");

        var product = await _productRepository.GetQueryable()
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null) throw new Exception("Product not found");

        var review = new ProductReview
        {
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            ProductId = request.ProductId,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        // We use the repositories to manage entities
        await _reviewRepository.AddAsync(review);
        
        // Update average rating
        product.AverageRating = product.Reviews.Count > 0 
            ? product.Reviews.Average(r => r.Rating) 
            : request.Rating;

        await _productRepository.UpdateAsync(product);
        
        return Unit.Value;
    }
}
