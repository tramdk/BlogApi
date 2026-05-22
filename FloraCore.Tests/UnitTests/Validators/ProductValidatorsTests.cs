using FloraCore.Application.Features.Products.Commands;
using FluentValidation.TestHelper;
using System;
using Xunit;

namespace FloraCore.Tests.UnitTests.Validators;

public class ProductValidatorsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(15.5)]
    [InlineData(100)]
    public void CreateProductCommandValidator_ShouldNotHaveError_WhenPromotionRateIsValid(decimal rate)
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            Id: Guid.NewGuid(),
            Name: "Valid Product",
            Description: "Valid Description",
            Price: 100m,
            PromotionRate: rate,
            Stock: 5,
            ImageUrl: null,
            CategoryId: null
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PromotionRate);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    [InlineData(-5)]
    [InlineData(150)]
    public void CreateProductCommandValidator_ShouldHaveError_WhenPromotionRateIsInvalid(decimal rate)
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            Id: Guid.NewGuid(),
            Name: "Valid Product",
            Description: "Valid Description",
            Price: 100m,
            PromotionRate: rate,
            Stock: 5,
            ImageUrl: null,
            CategoryId: null
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PromotionRate);
    }
}
