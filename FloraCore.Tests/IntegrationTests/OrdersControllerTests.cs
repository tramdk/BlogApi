
using FloraCore.Application.Common.Models;
using FloraCore.Application.Features.Orders.Queries;
using FloraCore.Domain.Entities;
using FloraCore.Domain.ValueObjects;
using FloraCore.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace FloraCore.Tests.IntegrationTests;

public class OrdersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OrdersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOrderStatistics_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrderStatistics_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync("user@floracore.com", "User123!");

        // Act
        var response = await client.GetAsync("/api/orders/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOrderStatistics_ShouldReturnOkAndStatistics_WhenAdminAuthenticated()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync("admin@floracore.com", "Admin123!");

        // Act
        var response = await client.GetAsync("/api/orders/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<OrderStatisticsDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrderStatistics_ShouldReturnFilteredStatistics_WhenDateRangeProvided()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync("admin@floracore.com", "Admin123!");
        var baseDate = DateTime.Now.Date;
        await SeedOrderDataAsync(baseDate);

        // Act
        var startDate = baseDate.AddDays(-1);
        var endDate = baseDate.AddDays(1);
        const string dateFormat = "yyyy-MM-dd HH:mm:ss.fffffff";
        var response = await client.GetAsync($"/api/orders/statistics?startDate={Uri.EscapeDataString(startDate.ToString(dateFormat))}&endDate={Uri.EscapeDataString(endDate.ToString(dateFormat))}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<OrderStatisticsDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.TotalOrders.Should().Be(3);
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync(string email, string password)
    {
        var client = _factory.CreateClient();

        // Register user if not admin (admin is pre-seeded)
        if (email != "admin@floracore.com")
        {
            var registerPayload = new { email, password, fullName = email.Split('@')[0] };
            await client.PostAsync("/api/v1/auth/register", JsonContent.Create(registerPayload));
        }

        var loginPayload = new { email, password };
        var loginResponse = await client.PostAsync("/api/v1/auth/login", JsonContent.Create(loginPayload));
        loginResponse.EnsureSuccessStatusCode();

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginData = JsonDocument.Parse(loginContent).RootElement;

        // Response is wrapped in ApiResponse envelope: { success, data: { accessToken, refreshToken } }
        var token = loginData.GetProperty("data").GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task SeedOrderDataAsync(DateTime seedBaseDate)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure the database is created
        await dbContext.Database.EnsureCreatedAsync();

        // Clear existing data
        await dbContext.Orders.ExecuteDeleteAsync();

        // Get actual user IDs from the database to satisfy FK constraint
        var userIds = await dbContext.Users.Select(u => u.Id).ToListAsync();
        if (userIds.Count == 0)
        {
            // Fallback: use a deterministic GUID if no users exist yet
            userIds = [Guid.NewGuid()];
        }

        // Seed new data using fixed dates to avoid time drift issues
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), UserId = userIds[0], OrderDate = seedBaseDate.AddDays(-2), OrderStatus = "Delivered", TotalAmount = 50, ShippingAddress = new Address { Street = "123 Main St", City = "HCMC", Country = "VN" } },
            new Order { Id = Guid.NewGuid(), UserId = userIds[0], OrderDate = seedBaseDate.AddDays(-1), OrderStatus = "Pending", TotalAmount = 100, ShippingAddress = new Address { Street = "456 Elm St", City = "HCMC", Country = "VN" } },
            new Order { Id = Guid.NewGuid(), UserId = userIds[0], OrderDate = seedBaseDate, OrderStatus = "Shipped", TotalAmount = 150, ShippingAddress = new Address { Street = "789 Oak St", City = "HCMC", Country = "VN" } },
            new Order { Id = Guid.NewGuid(), UserId = userIds[0], OrderDate = seedBaseDate.AddDays(1), OrderStatus = "Delivered", TotalAmount = 200, ShippingAddress = new Address { Street = "321 Pine St", City = "HCMC", Country = "VN" } }
        };

        dbContext.Orders.AddRange(orders);
        await dbContext.SaveChangesAsync();
    }
}
