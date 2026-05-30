using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Common.Models;
using FloraCore.Application.Features.Orders.Commands;
using FloraCore.Application.Interfaces;
using FloraCore.Domain.Constants;
using FloraCore.Domain.Entities;
using FloraCore.Domain.ValueObjects;
using FloraCore.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FloraCore.Tests.Application.Orders;

public class OrderCombinedFlowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;

    public OrderCombinedFlowTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _mockUnitOfWork = new Mock<IUnitOfWork>();
    }

    [Fact]
    public async Task UpdateOrderDetailsCommandHandler_ShouldUpdateDetails_AndClearPaymentUrlOnMethodChange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            UserName = "test1",
            Email = "test1@example.com",
            FullName = "Test User"
        };
        _context.Users.Add(user);

        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            ShippingAddress = new Address { Street = "Old Street", City = "Old City" },
            OrderStatus = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            PaymentMethod = "MOMO",
            PaymentUrl = "http://momo.vn/pay/123",
            TotalAmount = 150000m
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var mockRepository = new Mock<IOrderRepository>();
        mockRepository.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);

        var handler = new UpdateOrderDetailsCommandHandler(mockRepository.Object, _mockUnitOfWork.Object);
        var newAddress = new Address { Street = "New Street", City = "New City" };
        var command = new UpdateOrderDetailsCommand(orderId, newAddress, "VNPAY");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        order.ShippingAddress.Street.Should().Be("New Street");
        order.ShippingAddress.City.Should().Be("New City");
        order.PaymentMethod.Should().Be("VNPAY");
        order.PaymentUrl.Should().BeNull(); // Payment URL must be cleared

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokePaymentCommandHandler_ShouldGenerateFreshPaymentUrl_WhenInvokedLazy()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            UserName = "test2",
            Email = "test2@example.com",
            FullName = "Test User"
        };
        _context.Users.Add(user);

        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            ShippingAddress = new Address { Street = "Street", City = "City" },
            OrderStatus = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            PaymentMethod = "VNPAY",
            PaymentUrl = null,
            TotalAmount = 250000m
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var mockRepository = new Mock<IOrderRepository>();
        mockRepository.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);

        var mockPaymentService = new Mock<IPaymentService>();
        mockPaymentService.Setup(x => x.GatewayName).Returns("VNPAY");
        mockPaymentService.Setup(x => x.CreatePaymentUrlAsync(It.IsAny<OrderPaymentDto>()))
            .ReturnsAsync(new CreatePaymentResult { Success = true, PaymentUrl = "https://sandbox.vnpayment.vn/pay/456" });

        var mockFactory = new Mock<IPaymentServiceFactory>();
        mockFactory.Setup(x => x.GetPaymentService("VNPAY")).Returns(mockPaymentService.Object);

        var handler = new InvokePaymentCommandHandler(mockRepository.Object, mockFactory.Object, _mockUnitOfWork.Object);
        var command = new InvokePaymentCommand(orderId, "http://localhost:5000");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.PaymentUrl.Should().Be("https://sandbox.vnpayment.vn/pay/456");
        order.PaymentUrl.Should().Be("https://sandbox.vnpayment.vn/pay/456");

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
