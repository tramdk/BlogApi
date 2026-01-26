using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using BlogApi.Application.Features.PostCategories.Queries;
using BlogApi.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BlogApi.Tests.IntegrationTests;

public class PostCategoriesControllerTests : BaseIntegrationTest
{
    public PostCategoriesControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<string> GetAdminTokenAsync()
    {
        // Search for existing admin or register one
        var loginCommand = new LoginCommand("admin@blogapi.com", "Admin123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return result!.AccessToken;
        }

        // Fallback or seed logic if needed, but Program.cs seeds admin
        return string.Empty;
    }

    private record LoginResponse(string AccessToken, string RefreshToken);

    [Fact]
    public async Task GetAll_ReturnsDefaultCategories()
    {
        // Act
        var response = await _client.GetAsync("/api/postcategories");

        // Assert
        response.EnsureSuccessStatusCode();
        var categories = await response.Content.ReadFromJsonAsync<List<PostCategoryDto>>();
        
        Assert.NotNull(categories);
        Assert.Contains(categories, c => c.Id == "blog");
        Assert.Contains(categories, c => c.Id == "feedback");
        Assert.Contains(categories, c => c.Id == "intro");
    }

    [Fact]
    public async Task GetById_ReturnsCategory()
    {
        // Act
        var response = await _client.GetAsync("/api/postcategories/blog");

        // Assert
        response.EnsureSuccessStatusCode();
        var category = await response.Content.ReadFromJsonAsync<PostCategoryDto>();
        Assert.NotNull(category);
        Assert.Equal("blog", category.Id);
    }
}
