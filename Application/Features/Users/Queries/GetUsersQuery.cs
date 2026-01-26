using BlogApi.Application.Common.Attributes;
using BlogApi.Application.Common.Models;
using BlogApi.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Application.Features.Users.Queries;

[Cacheable(ExpirationMinutes = 2)]
public record GetUsersQuery(int PageNumber = 1, int PageSize = 10) : IRequest<PaginatedList<UserDto>>;

public class GetUsersHandler : IRequestHandler<GetUsersQuery, PaginatedList<UserDto>>
{
    private readonly UserManager<AppUser> _userManager;

    public GetUsersHandler(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<PaginatedList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _userManager.Users.AsNoTracking();
        
        var count = await query.CountAsync(cancellationToken);
        var users = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                UserName = user.UserName ?? string.Empty,
                Roles = roles
            });
        }

        return new PaginatedList<UserDto>(items, count, request.PageNumber, request.PageSize);
    }
}
