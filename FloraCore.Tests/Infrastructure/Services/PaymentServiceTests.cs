using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Common.Models;
using FloraCore.Domain.Constants;
using FloraCore.Domain.Entities;
using FloraCore.Domain.ValueObjects;
using FloraCore.Infrastructure.Data;
using FloraCore.Infrastructure.Services.Payments;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FloraCore.Tests.Infrastructure.Services;

public class PaymentServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public PaymentServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _mockConfiguration = new Mock<IConfiguration>();
    }

    [Fact]
    public async Task VnPayService_ShouldGenerateCorrectUrl_AndVerifySuccessfully()
    {
        // Arrange
        var hashSecret = "9U8WJNJNLUPU8V4M8Z8W8JNJNLUPU8V4";
        _mockConfiguration.Setup(x => x["PaymentGateways:VnPay:Url"]).Returns("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html");
        _mockConfiguration.Setup(x => x["PaymentGateways:VnPay:TmnCode"]).Returns("TEST_TMN");
        _mockConfiguration.Setup(x => x["PaymentGateways:VnPay:HashSecret"]).Returns(hashSecret);

        var vnPayService = new VnPayService(_mockConfiguration.Object);
        var orderId = Guid.NewGuid();
        var orderDto = new OrderPaymentDto
        {
            OrderId = orderId,
            Amount = 100000m,
            Description = "Thanh toan don hang",
            ReturnUrl = "http://localhost:5000/api/payments/callback"
        };

        // Act - Create payment Url
        var createResult = await vnPayService.CreatePaymentUrlAsync(orderDto);

        // Assert creation
        createResult.Success.Should().BeTrue();
        createResult.PaymentUrl.Should().Contain("vnp_SecureHash=");

        // Construct mock callback params (simulating VNPAY response)
        var callbackParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            { "vnp_Amount", "10000000" },
            { "vnp_BankCode", "NCB" },
            { "vnp_BankTranNo", "VNP13579246" },
            { "vnp_CardType", "ATM" },
            { "vnp_OrderInfo", "Thanh toan don hang" },
            { "vnp_PayDate", "20260531120000" },
            { "vnp_ResponseCode", "00" },
            { "vnp_TmnCode", "TEST_TMN" },
            { "vnp_TransactionNo", "123456" },
            { "vnp_TransactionStatus", "00" },
            { "vnp_TxnRef", orderId.ToString() }
        };

        // Recreate signature inside test simulating VNPAY signing
        var rawData = string.Join("&", callbackParams.Select(kv => $"{kv.Key}={kv.Value}"));
        var secureHash = HmacSha512(hashSecret, rawData);

        var callbackDict = callbackParams.ToDictionary(kv => kv.Key, kv => kv.Value);
        callbackDict["vnp_SecureHash"] = secureHash;

        var callbackDto = new PaymentCallbackDto { QueryParameters = callbackDict };

        // Act - Verify callback
        var isVerified = await vnPayService.VerifyCallbackAsync(callbackDto);

        // Assert verification
        isVerified.Should().BeTrue();
    }

    [Fact]
    public async Task IdempotentPaymentHandler_ShouldProcessSuccessfully_AndBeIdempotent()
    {
        // Arrange
        var gateway = "VNPAY";
        var transactionId = Guid.NewGuid().ToString();

        // Register user and order
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            UserName = "test",
            Email = "test@example.com",
            FullName = "Test User"
        };
        _context.Users.Add(user);

        var order = new Order
        {
            Id = Guid.Parse(transactionId),
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            ShippingAddress = new Address { Street = "123 Street", City = "HN" },
            OrderStatus = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            PaymentMethod = gateway,
            TotalAmount = 50000m
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var mockPaymentService = new Mock<IPaymentService>();
        mockPaymentService.Setup(x => x.GatewayName).Returns(gateway);
        mockPaymentService.Setup(x => x.VerifyCallbackAsync(It.IsAny<PaymentCallbackDto>())).ReturnsAsync(true);

        var mockFactory = new Mock<FloraCore.Application.Common.Interfaces.IPaymentServiceFactory>();
        mockFactory.Setup(x => x.GetPaymentService(gateway)).Returns(mockPaymentService.Object);

        var mockLogger = new Mock<ILogger<IdempotentPaymentHandler>>();

        var handler = new IdempotentPaymentHandler(_context, mockFactory.Object, mockLogger.Object);
        var callbackDto = new PaymentCallbackDto();

        // Act - First process
        var result1 = await handler.ProcessPaymentCallbackAsync(gateway, transactionId, callbackDto);

        // Assert first run
        result1.Should().BeTrue();
        
        var dbOrder1 = await _context.Orders.FindAsync(order.Id);
        dbOrder1!.PaymentStatus.Should().Be(PaymentStatus.Paid);
        dbOrder1.OrderStatus.Should().Be(OrderStatus.Processing);

        var inboxCount1 = await _context.InboxMessages.CountAsync();
        inboxCount1.Should().Be(1);

        var txCount1 = await _context.PaymentTransactions.CountAsync();
        txCount1.Should().Be(1);

        // Act - Second process (simulated duplicate request)
        var result2 = await handler.ProcessPaymentCallbackAsync(gateway, transactionId, callbackDto);

        // Assert second run (idempotent - returns true but does not duplicate database entries)
        result2.Should().BeTrue();

        var inboxCount2 = await _context.InboxMessages.CountAsync();
        inboxCount2.Should().Be(1); // Still 1 record, no duplicate in Inbox

        var txCount2 = await _context.PaymentTransactions.CountAsync();
        txCount2.Should().Be(1); // Still 1 transaction record
    }

    private static string HmacSha512(string key, string inputData)
    {
        var hash = new StringBuilder();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        using (var hmac = new HMACSHA512(keyBytes))
        {
            var hashValue = hmac.ComputeHash(inputBytes);
            foreach (var theByte in hashValue)
            {
                hash.Append(theByte.ToString("x2"));
            }
        }
        return hash.ToString();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
