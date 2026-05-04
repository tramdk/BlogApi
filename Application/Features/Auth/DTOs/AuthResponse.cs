using FloraCore.Application.Features.Users.Queries;

namespace FloraCore.Application.Features.Auth.DTOs;

public record AuthResponse(string AccessToken, string RefreshToken, UserDto User);
