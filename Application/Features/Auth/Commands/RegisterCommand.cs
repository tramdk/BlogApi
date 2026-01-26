using BlogApi.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading;
using System.Threading.Tasks;
using UUIDNext;

namespace BlogApi.Application.Features.Auth.Commands;

public record RegisterCommand(string Email, string Password, string FullName, string Role = "User") : IRequest<bool>;

public class RegisterHandler : IRequestHandler<RegisterCommand, bool>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public RegisterHandler(UserManager<AppUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<bool> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = new AppUser 
        { 
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            Email = request.Email, 
            UserName = request.Email, 
            FullName = request.FullName 
        };
        var result = await _userManager.CreateAsync(user, request.Password);
        
        if (result.Succeeded)
        {
            if (await _roleManager.RoleExistsAsync(request.Role))
            {
                await _userManager.AddToRoleAsync(user, request.Role);
            }
        }
        
        return result.Succeeded;
    }
}
