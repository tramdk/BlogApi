using System.Net;
using FloraCore.Application.Features.Auth.Commands;
using System.Net.Http.Json;
using Xunit;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FloraCore.Tests.IntegrationTests;

public class AuthControllerTests : BaseIntegrationTest
{
    public AuthControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var command = new LoginCommand("wrong@email.com", "wrongpassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", command);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FullAuthFlow_Works()
    {
        // 1. Register
        var regCmd = new RegisterCommand("unique_flow@test.com", "Password123!", "Flow User");
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", regCmd);
        Assert.Equal(HttpStatusCode.OK, regResp.StatusCode);

        // 2. Login
        var loginCmd = new LoginCommand("unique_flow@test.com", "Password123!");
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", loginCmd);
        var authRes = await GetResponseDataAsync<AuthResponse>(loginResp);
        Assert.NotNull(authRes?.AccessToken);

        // 3. Refresh
        var refreshCmd = new RefreshTokenCommand(authRes!.AccessToken, authRes.RefreshToken);
        var refreshResp = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshCmd);
        Assert.Equal(HttpStatusCode.OK, refreshResp.StatusCode);
        var newAuthRes = await GetResponseDataAsync<AuthResponse>(refreshResp);
        Assert.NotEqual(authRes.AccessToken, newAuthRes!.AccessToken);

        // 4. Logout
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newAuthRes.AccessToken);
        var logoutResp = await _client.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.OK, logoutResp.StatusCode);
    }

    private record AuthResponse(string AccessToken, string RefreshToken);
}
