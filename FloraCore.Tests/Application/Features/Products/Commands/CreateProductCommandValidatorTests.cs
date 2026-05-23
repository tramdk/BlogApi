using FloraCore.Application.Features.Products.Commands;
using FluentValidation.TestHelper;
using Xunit;

namespace FloraCore.Tests.Application.Features.Products.Commands;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Error_When_Name_Is_Valid()
    {
        var command = new CreateProductCommand(null, "Valid Name", "Description", 10, 0, 10, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var command = new CreateProductCommand(null, "", "Description", 10, 0, 10, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Have_Error_When_Price_Is_Negative()
    {
        var command = new CreateProductCommand(null, "Name", "Description", -10, 0, 10, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Price);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Price_Is_Positive()
    {
        var command = new CreateProductCommand(null, "Name", "Description", 10, 0, 10, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(c => c.Price);
    }

    [Fact]
        public void Should_Have_Error_When_PromotionRate_Is_Out_Of_Range()
        {
            var command1 = new CreateProductCommand(null, "Name", "Description", 10, -1, 10, null, null);
            var result1 = _validator.TestValidate(command1);
            result1.ShouldHaveValidationErrorFor(c => c.PromotionRate);

            var command2 = new CreateProductCommand(null, "Name", "Description", 10, 101, 10, null, null);
            var result2 = _validator.TestValidate(command2);
            result2.ShouldHaveValidationErrorFor(c => c.PromotionRate);
        }

        [Fact]
        public void Should_Not_Have_Error_When_PromotionRate_Is_In_Range()
        {
            var command = new CreateProductCommand(null, "Name", "Description", 10, 50, 10, null, null);
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveValidationErrorFor(c => c.PromotionRate);
        }
}
