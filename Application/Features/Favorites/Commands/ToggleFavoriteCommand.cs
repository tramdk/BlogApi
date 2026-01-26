using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UUIDNext;

namespace BlogApi.Application.Features.Favorites.Commands;

public record ToggleFavoriteCommand(Guid ProductId) : IRequest<bool>;

public class ToggleFavoriteCommandHandler : IRequestHandler<ToggleFavoriteCommand, bool>
{
    private readonly IGenericRepository<Favorite, Guid> _favoriteRepository;
    private readonly ICurrentUserService _currentUserService;

    public ToggleFavoriteCommandHandler(IGenericRepository<Favorite, Guid> favoriteRepository, ICurrentUserService currentUserService)
    {
        _favoriteRepository = favoriteRepository;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(ToggleFavoriteCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new Exception("User not authenticated");

        var favorite = await _favoriteRepository.GetQueryable()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == request.ProductId, cancellationToken);

        if (favorite != null)
        {
            await _favoriteRepository.DeleteAsync(favorite);
            return false; // Removed
        }
        else
        {
            await _favoriteRepository.AddAsync(new Favorite
            {
                Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
                UserId = userId,
                ProductId = request.ProductId
            });
            return true; // Added
        }
    }
}
