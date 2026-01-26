using BlogApi.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.Users.Commands;

public record UpdateUserCommand(Guid Id, string FullName, string? Email = null) : IRequest<bool>;

public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly UserManager<AppUser> _userManager;

    public UpdateUserHandler(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user == null) return false;

        user.FullName = request.FullName;
        if (!string.IsNullOrEmpty(request.Email))
        {
            user.Email = request.Email;
            user.UserName = request.Email;
        }

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }
}
