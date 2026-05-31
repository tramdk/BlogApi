
using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Features.Orders.Queries;
using FloraCore.Application.Features.Orders.DTOs;
using FloraCore.Domain.Entities;
using FloraCore.Tests;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace FloraCore.Tests.Application.Features.Orders.Queries;

public class GetOrderStatisticsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmptyStatistics_WhenNoOrdersExist()
    {
        // Arrange
        var mockOrderRepository = new Mock<IGenericRepository<Order, Guid>>();
        mockOrderRepository.Setup(repo => repo.GetQueryable()).Returns(new List<Order>().AsAsyncQueryable());
        var handler = new GetOrderStatisticsQueryHandler(mockOrderRepository.Object);
        var query = new GetOrderStatisticsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalOrders.Should().Be(0);
        result.TotalRevenue.Should().Be(0);
        result.AverageOrderValue.Should().Be(0);
        result.OrdersByStatus.Should().BeEmpty();
        result.RevenueByMonth.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectStatistics_WhenOrdersExist()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), OrderDate = DateTime.Now, OrderStatus = "Pending", TotalAmount = 100 },
            new Order { Id = Guid.NewGuid(), OrderDate = DateTime.Now, OrderStatus = "Delivered", TotalAmount = 200 },
            new Order { Id = Guid.NewGuid(), OrderDate = DateTime.Now, OrderStatus = "Delivered", TotalAmount = 300 }
        };
        var mockOrderRepository = new Mock<IGenericRepository<Order, Guid>>();
        mockOrderRepository.Setup(repo => repo.GetQueryable()).Returns(orders.AsAsyncQueryable());
        var handler = new GetOrderStatisticsQueryHandler(mockOrderRepository.Object);
        var query = new GetOrderStatisticsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalOrders.Should().Be(3);
        result.TotalRevenue.Should().Be(600);
        result.AverageOrderValue.Should().Be(200);
        result.OrdersByStatus.Should().ContainKey("Pending").WhoseValue.Should().Be(1);
        result.OrdersByStatus.Should().ContainKey("Delivered").WhoseValue.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldFilterByStartDate_WhenProvided()
    {
        // Arrange
        var today = DateTime.Today;
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), OrderDate = today.AddDays(-1), OrderStatus = "Pending", TotalAmount = 100 },
            new Order { Id = Guid.NewGuid(), OrderDate = today, OrderStatus = "Delivered", TotalAmount = 200 }
        };
        var mockOrderRepository = new Mock<IGenericRepository<Order, Guid>>();
        mockOrderRepository.Setup(repo => repo.GetQueryable()).Returns(orders.AsAsyncQueryable());
        var handler = new GetOrderStatisticsQueryHandler(mockOrderRepository.Object);
        var query = new GetOrderStatisticsQuery { StartDate = today };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalOrders.Should().Be(1);
        result.TotalRevenue.Should().Be(200);
    }

    [Fact]
    public async Task Handle_ShouldFilterByEndDate_WhenProvided()
    {
        // Arrange
        var today = DateTime.Today;
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), OrderDate = today.AddDays(-1), OrderStatus = "Pending", TotalAmount = 100 },
            new Order { Id = Guid.NewGuid(), OrderDate = today, OrderStatus = "Delivered", TotalAmount = 200 }
        };
        var mockOrderRepository = new Mock<IGenericRepository<Order, Guid>>();
        mockOrderRepository.Setup(repo => repo.GetQueryable()).Returns(orders.AsAsyncQueryable());
        var handler = new GetOrderStatisticsQueryHandler(mockOrderRepository.Object);
        var query = new GetOrderStatisticsQuery { EndDate = today.AddDays(-1) };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalOrders.Should().Be(1);
        result.TotalRevenue.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ShouldFilterByDateRange_WhenBothDatesProvided()
    {
        // Arrange
        var today = DateTime.Today;
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), OrderDate = today.AddDays(-2), OrderStatus = "Pending", TotalAmount = 100 },
            new Order { Id = Guid.NewGuid(), OrderDate = today.AddDays(-1), OrderStatus = "Delivered", TotalAmount = 200 },
            new Order { Id = Guid.NewGuid(), OrderDate = today, OrderStatus = "Delivered", TotalAmount = 300 }
        };
        var mockOrderRepository = new Mock<IGenericRepository<Order, Guid>>();
        mockOrderRepository.Setup(repo => repo.GetQueryable()).Returns(orders.AsAsyncQueryable());
        var handler = new GetOrderStatisticsQueryHandler(mockOrderRepository.Object);
        var query = new GetOrderStatisticsQuery { StartDate = today.AddDays(-1), EndDate = today };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalOrders.Should().Be(2);
        result.TotalRevenue.Should().Be(500);
    }

    [Fact]
    public async Task Handle_ShouldGroupOrdersByStatusCorrectly()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), OrderDate = DateTime.Now, OrderStatus = "Pending", TotalAmount = 100 },
            new Order { Id = Guid.NewGuid(), OrderDate = DateTime.Now, OrderStatus = "Delivered", TotalAmount = 200 },
            new Order { Id = Guid.NewGuid(), OrderDate = DateTime.Now, OrderStatus = "Cancelled", TotalAmount = 300 },
            new Order { Id = Guid.NewGuid(), OrderDate = DateTime.Now, OrderStatus = "Delivered", TotalAmount = 400 }
        };
        var mockOrderRepository = new Mock<IGenericRepository<Order, Guid>>();
        mockOrderRepository.Setup(repo => repo.GetQueryable()).Returns(orders.AsAsyncQueryable());
        var handler = new GetOrderStatisticsQueryHandler(mockOrderRepository.Object);
        var query = new GetOrderStatisticsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.OrdersByStatus.Should().ContainKey("Pending").WhoseValue.Should().Be(1);
        result.OrdersByStatus.Should().ContainKey("Delivered").WhoseValue.Should().Be(2);
        result.OrdersByStatus.Should().ContainKey("Cancelled").WhoseValue.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldCalculateRevenueByMonthCorrectly()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), OrderDate = new DateTime(2024, 01, 15), OrderStatus = "Delivered", TotalAmount = 100 },
            new Order { Id = Guid.NewGuid(), OrderDate = new DateTime(2024, 01, 20), OrderStatus = "Delivered", TotalAmount = 200 },
            new Order { Id = Guid.NewGuid(), OrderDate = new DateTime(2024, 02, 10), OrderStatus = "Delivered", TotalAmount = 300 }
        };
        var mockOrderRepository = new Mock<IGenericRepository<Order, Guid>>();
        mockOrderRepository.Setup(repo => repo.GetQueryable()).Returns(orders.AsAsyncQueryable());
        var handler = new GetOrderStatisticsQueryHandler(mockOrderRepository.Object);
        var query = new GetOrderStatisticsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.RevenueByMonth.Should().ContainKey("2024-01").WhoseValue.Should().Be(300);
        result.RevenueByMonth.Should().ContainKey("2024-02").WhoseValue.Should().Be(300);
    }

    [Fact]
    public async Task Handle_ShouldReturnZeroAverageOrderValue_WhenTotalOrdersIsZero()
    {
        // Arrange
        var mockOrderRepository = new Mock<IGenericRepository<Order, Guid>>();
        mockOrderRepository.Setup(repo => repo.GetQueryable()).Returns(new List<Order>().AsAsyncQueryable());
        var handler = new GetOrderStatisticsQueryHandler(mockOrderRepository.Object);
        var query = new GetOrderStatisticsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.AverageOrderValue.Should().Be(0);
    }
}
