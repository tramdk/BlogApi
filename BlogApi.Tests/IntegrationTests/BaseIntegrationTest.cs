using BlogApi.Application.Common.Models;
using BlogApi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Xunit;

namespace BlogApi.Tests.IntegrationTests;

public class BaseIntegrationTest : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory _factory;
    protected readonly HttpClient _client;

    public BaseIntegrationTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    protected async Task<T?> GetResponseDataAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"API Error: {response.StatusCode} - {content}");
        }

        try 
        {
            var result = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<T>>(content, new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            return result != null ? result.Data : default;
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new Exception($"Failed to parse JSON. Status: {response.StatusCode}, Content: {content}", ex);
        }
    }

    protected async Task<ApiResponse<T>?> GetApiResponseAsync<T>(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
    }
}

