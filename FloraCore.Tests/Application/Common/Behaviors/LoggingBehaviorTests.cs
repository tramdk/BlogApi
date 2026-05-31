using FluentAssertions;
using Moq;
using Xunit;
using FloraCore.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FloraCore.Tests.Application.Common.Behaviors;

public class LoggingBehaviorTests
{
    private readonly Mock<ILogger<LoggingBehavior<TestRequest, Unit>>> _mockLogger;
    private readonly RequestHandlerDelegate<Unit> _mockNext;

    public LoggingBehaviorTests()
    {
        _mockLogger = new Mock<ILogger<LoggingBehavior<TestRequest, Unit>>>();
        _mockNext = () => Task.FromResult(Unit.Value);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new LoggingBehavior<TestRequest, Unit>(null);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    // Dummy request for testing purposes
    public class TestRequest : IRequest<Unit> { }
}
