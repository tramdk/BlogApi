using BlogApi.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;

namespace BlogApi.Controllers;

/// <summary>
/// Controller for authentication operations.
/// </summary>
/// <param name="mediator">The mediator instance for handling commands and queries.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    /// <summary>
    /// Authenticates a user and returns tokens.
    /// </summary>
    /// <param name="command">The login credentials.</param>
    /// <returns>Auth response containing tokens.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="command">The registration details.</param>
    /// <returns>Ok if successful; otherwise, BadRequest.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        return result ? Ok() : BadRequest();
    }

    /// <summary>
    /// Refreshes the access token using a refresh token.
    /// </summary>
    /// <param name="command">The refresh token command.</param>
    /// <returns>Auth response containing new tokens.</returns>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command)
    {
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    /// <summary>
    /// Logs out the current user and invalidates the session.
    /// </summary>
    /// <returns>Ok if successful.</returns>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        await _mediator.Send(new LogoutCommand(accessToken));
        return Ok();
    }

    /// <summary>
    /// Changes the user's password and invalidates all existing sessions (tokens).
    /// </summary>
    /// <param name="command">The change password command.</param>
    /// <returns>Ok if successful, BadRequest if validation fails.</returns>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return result ? Ok() : BadRequest();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
