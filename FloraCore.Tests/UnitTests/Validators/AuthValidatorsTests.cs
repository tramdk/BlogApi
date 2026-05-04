using FloraCore.Application.Features.Auth.Commands;
using FluentValidation.TestHelper;
using Xunit;

namespace FloraCore.Tests.UnitTests.Validators;

public class AuthValidatorsTests
{
    [Fact]
    public void RegisterCommandValidator_ShouldHaveError_WhenEmailIsInvalid()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("invalid-email", "Password123", "John Doe");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void RegisterCommandValidator_ShouldNotHaveError_WhenEmailIsValid()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("valid@email.com", "Password123", "John Doe");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void RegisterCommandValidator_ShouldHaveError_WhenPasswordIsTooShort()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("valid@email.com", "12345", "John Doe");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void LoginCommandValidator_ShouldHaveError_WhenEmailIsEmpty()
    {
        // Arrange
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("", "Password123");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ChangePasswordCommandValidator_ShouldHaveError_WhenNewPasswordIsTooShort()
    {
        // Arrange
        var validator = new ChangePasswordCommandValidator();
        var command = new ChangePasswordCommand("OldPassword123", "12345");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
