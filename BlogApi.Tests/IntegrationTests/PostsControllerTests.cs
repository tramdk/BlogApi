using System.Net;
using System.Net.Http.Json;
using BlogApi.Application.Features.Posts.Commands;
using BlogApi.Application.Features.Posts.DTOs;
using BlogApi.Application.Common.Models;
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
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerCommand);

        var loginCommand = new LoginCommand(email, password);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginCommand);
        
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Login failed in GetTokenAsync. Status: {response.StatusCode}, Content: {content}");
        }

        var result = await GetResponseDataAsync<LoginResponse>(response);
        return result!.AccessToken;
    }

    private record LoginResponse(string AccessToken, string RefreshToken);

    [Fact]
    public async Task CreatePost_ReturnsId()
    {
        // Arrange
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var command = new CreatePostCommand(null, "Test Post", "Content here");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/posts", command);
        var id = await GetResponseDataAsync<Guid>(response);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task GetPost_ReturnsPost()
    {
        // Arrange
        var token = await GetTokenAsync("reader@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createCmd = new CreatePostCommand(null, "Get Test", "Content");
        var createResp = await _client.PostAsJsonAsync("/api/v1/posts", createCmd);
        var id = await GetResponseDataAsync<Guid>(createResp);

        // Act
        var response = await _client.GetAsync($"/api/v1/posts/{id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var post = await GetResponseDataAsync<PostDetailDto>(response);
        Assert.NotNull(post);
        Assert.Equal("Get Test", post.Title);
    }

    [Fact]
    public async Task RatePost_UpdatesAverageRating()
    {
        // Arrange
        var token = await GetTokenAsync("rater@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createCmd = new CreatePostCommand(null, "Rating Test", "Content");
        var createResp = await _client.PostAsJsonAsync("/api/v1/posts", createCmd);
        var id = await GetResponseDataAsync<Guid>(createResp);

        // Act
        var rateResp = await _client.PostAsJsonAsync($"/api/v1/posts/{id}/rate", 5);
        Assert.Equal(HttpStatusCode.OK, rateResp.StatusCode);

        var getResp = await _client.GetAsync($"/api/v1/posts/{id}");
        var post = await GetResponseDataAsync<PostDetailDto>(getResp);

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

        var command = new CreatePostCommand(null, "", "Content"); // Empty title

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/posts", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeletePost_ByNonOwner_ReturnsForbidden()
    {
        // 1. Author creates a post
        var authorToken = await GetTokenAsync("author1@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        var createCmd = new CreatePostCommand(null, "Author's Post", "Content");
        var createResp = await _client.PostAsJsonAsync("/api/v1/posts", createCmd);
        var postId = await GetResponseDataAsync<Guid>(createResp);

        // 2. Another user tries to delete it
        var otherToken = await GetTokenAsync("otheruser@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        
        // Act
        var deleteResp = await _client.DeleteAsync($"/api/v1/posts/{postId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, deleteResp.StatusCode);
    }

    [Fact]
    public async Task SearchPosts_ReturnsPagedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/posts/search?searchTerm=test");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await GetResponseDataAsync<PagedResult<PostDto>>(response);
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
    }

    [Fact]
    public async Task SearchPosts_WithFilterModel_ReturnsPagedResult()
    {
        // Arrange
        var request = new 
        {
            Filters = new Dictionary<string, object>
            {
                { "title", new { filterType = "text", type = "contains", filter = "test" } }
            },
            Page = 0,
            PageSize = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/posts/search", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await GetResponseDataAsync<PagedResult<PostDto>>(response);
        Assert.NotNull(result);
    }
}
