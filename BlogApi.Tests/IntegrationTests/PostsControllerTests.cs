using System.Net;
using System.Net.Http.Json;
using BlogApi.Application.Features.Posts.Commands;
using BlogApi.Application.Features.Posts.DTOs;
using BlogApi.Application.Features.Auth.Commands;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BlogApi.Tests.IntegrationTests;

public class PostsControllerTests : BaseIntegrationTest
{
    public PostsControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<string> GetTokenAsync(string email = "author@example.com", string password = "Password123!")
    {
        var registerCommand = new RegisterCommand(email, password, "Author User");
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        var loginCommand = new LoginCommand(email, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return result!.AccessToken;
    }

    private record LoginResponse(string AccessToken, string RefreshToken);

    [Fact]
    public async Task CreatePost_ReturnsId()
    {
        // Arrange
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var command = new CreatePostCommand("Test Post", "Content here");

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", command);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task GetPost_ReturnsPost()
    {
        // Arrange
        var token = await GetTokenAsync("reader@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createCmd = new CreatePostCommand("Get Test", "Content");
        var createResp = await _client.PostAsJsonAsync("/api/posts", createCmd);
        var id = await createResp.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await _client.GetAsync($"/api/posts/{id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var post = await response.Content.ReadFromJsonAsync<PostDetailDto>();
        Assert.NotNull(post);
        Assert.Equal("Get Test", post.Title);
    }

    [Fact]
    public async Task RatePost_UpdatesAverageRating()
    {
        // Arrange
        var token = await GetTokenAsync("rater@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createCmd = new CreatePostCommand("Rating Test", "Content");
        var createResp = await _client.PostAsJsonAsync("/api/posts", createCmd);
        var id = await createResp.Content.ReadFromJsonAsync<Guid>();

        // Act
        var rateResp = await _client.PostAsJsonAsync($"/api/posts/{id}/rate", 5);
        Assert.Equal(HttpStatusCode.OK, rateResp.StatusCode);

        var getResp = await _client.GetAsync($"/api/posts/{id}");
        var post = await getResp.Content.ReadFromJsonAsync<PostDetailDto>();

        // Assert
        Assert.Equal(5, post!.AverageRating);
        Assert.Equal(1, post.TotalRatings);
    }

    [Fact]
    public async Task CreatePost_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetTokenAsync("validator@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var command = new CreatePostCommand("", "Content"); // Empty title

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeletePost_ByNonOwner_ReturnsForbidden()
    {
        // 1. Author creates a post
        var authorToken = await GetTokenAsync("author1@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        var createCmd = new CreatePostCommand("Author's Post", "Content");
        var createResp = await _client.PostAsJsonAsync("/api/posts", createCmd);
        var postId = await createResp.Content.ReadFromJsonAsync<Guid>();

        // 2. Another user tries to delete it
        var otherToken = await GetTokenAsync("otheruser@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        
        // Act
        var deleteResp = await _client.DeleteAsync($"/api/posts/{postId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, deleteResp.StatusCode);
    }
}
