using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.Cart.Commands;

public record RemoveFromCartCommand(Guid ProductId) : IRequest<Unit>;

public class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, Unit>
{
    private readonly IGenericRepository<BlogApi.Domain.Entities.Cart, Guid> _cartRepository;
    private readonly IGenericRepository<CartItem, Guid> _cartItemRepository;
    private readonly ICurrentUserService _currentUserService;

    public RemoveFromCartCommandHandler(
        IGenericRepository<BlogApi.Domain.Entities.Cart, Guid> cartRepository, 
        IGenericRepository<CartItem, Guid> cartItemRepository,
        ICurrentUserService currentUserService)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new Exception("User not authenticated");

        var cart = await _cartRepository.GetQueryable()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart != null)
        {
            var item = await _cartItemRepository.GetQueryable()
                .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == request.ProductId, cancellationToken);

            if (item != null)
            {
                await _cartItemRepository.DeleteAsync(item);
            }
        }

        return Unit.Value;
    }
}
