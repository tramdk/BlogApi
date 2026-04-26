using System.Net;
using System.Net.Http.Json;
using BlogApi.Application.Features.Users.Commands;
using BlogApi.Application.Features.Users.Queries;
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
        var registerCommand = new RegisterCommand(email, password, "Test User");
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        var loginCommand = new LoginCommand(email, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return result!.AccessToken;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        // Register with Admin role (seeded roles allow this in test env)
        var registerCommand = new RegisterCommand("admin-users-test@example.com", "AdminPass123!", "Admin User", "Admin");
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        var loginCommand = new LoginCommand("admin-users-test@example.com", "AdminPass123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return result!.AccessToken;
    }

    private record LoginResponse(string AccessToken, string RefreshToken);

    [Fact]
    public async Task CreateUser_AsAdmin_ReturnCreated()
    {
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var command = new CreateUserCommand("newuser-admin@example.com", "Password123!", "New User");
        var response = await _client.PostAsJsonAsync("/api/users", command);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_AsRegularUser_ReturnsForbidden()
    {
        var token = await GetTokenAsync("regular-create@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var command = new CreateUserCommand("blocked@example.com", "Password123!", "Blocked User");
        var response = await _client.PostAsJsonAsync("/api/users", command);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_AsAdmin_ReturnsList()
    {
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/users");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetUsers_AsRegularUser_ReturnsForbidden()
    {
        var token = await GetTokenAsync("regular-list@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_AsAdmin_ReturnsNoContent()
    {
        var adminToken = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Admin creates a user first
        var createCmd = new CreateUserCommand("todelete-admin@example.com", "Password123!", "To Delete");
        var createResponse = await _client.PostAsJsonAsync("/api/users", createCmd);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var userId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Then deletes it
        var deleteResponse = await _client.DeleteAsync($"/api/users/{userId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}
