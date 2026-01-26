using System.Net;
using System.Net.Http.Json;
using BlogApi.Application.Features.Users.Commands;
using BlogApi.Application.Features.Users.Queries;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using BlogApi.Application.Features.Auth.Commands;
using BlogApi.Application.Common.Models;
using System.Net.Http.Headers;

namespace BlogApi.Tests.IntegrationTests;

public class UsersControllerTests : BaseIntegrationTest
{
    public UsersControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<string> GetTokenAsync(string email = "test@example.com", string password = "Password123!")
    {
        // First Register
        var registerCommand = new RegisterCommand(email, password, "Test User");
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        // Then Login to get token
        var loginCommand = new LoginCommand(email, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return result!.AccessToken;
    }

    private record LoginResponse(string AccessToken, string RefreshToken);

    [Fact]
    public async Task CreateUser_ReturnCreated()
    {
        // Arrange
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var command = new CreateUserCommand("newuser@example.com", "Password123!", "New User");

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", command);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_ReturnsList()
    {
        // Arrange
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<UserDto>>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task DeleteUser_ReturnsNoContent()
    {
        // Arrange
        var token = await GetTokenAsync("admin@test.com", "AdminPassword123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // Create a user to delete
        var createCmd = new CreateUserCommand("todelete@example.com", "Password123!", "To Delete");
        var createResponse = await _client.PostAsJsonAsync("/api/users", createCmd);
        var userId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/users/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}
