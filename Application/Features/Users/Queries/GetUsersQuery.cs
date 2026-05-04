using FloraCore.Application.Common.Attributes;
using FloraCore.Application.Common.Models;
using FloraCore.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FloraCore.Application.Features.Users.Queries;

[Cacheable(ExpirationMinutes = 2)]
public record GetUsersQuery(int PageNumber = 1, int PageSize = 10) : IRequest<PagedResult<UserDto>>;

public class GetUsersHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly UserManager<AppUser> _userManager;

    public GetUsersHandler(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
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

        return new PagedResult<UserDto>(items, count, request.PageNumber, request.PageSize);
    }
}
