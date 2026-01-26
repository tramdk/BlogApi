using BlogApi.Application.Features.Users.Queries;

namespace BlogApi.Application.Features.Auth.DTOs;

public record AuthResponse(string AccessToken, string RefreshToken, UserDto User);
