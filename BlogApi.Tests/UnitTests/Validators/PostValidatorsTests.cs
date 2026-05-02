using BlogApi.Application.Features.Posts.Commands;
using FluentValidation.TestHelper;
using System;
using Xunit;

namespace BlogApi.Tests.UnitTests.Validators;

public class PostValidatorsTests
{
    [Fact]
    public void CreatePostCommandValidator_ShouldHaveError_WhenTitleIsEmpty()
    {
        // Arrange
        var validator = new CreatePostCommandValidator();
        var command = new CreatePostCommand(null, "", "Valid Content", "category-1");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void CreatePostCommandValidator_ShouldHaveError_WhenContentIsEmpty()
    {
        // Arrange
        var validator = new CreatePostCommandValidator();
        var command = new CreatePostCommand(null, "Valid Title", "", "category-1");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void UpdatePostCommandValidator_ShouldHaveError_WhenTitleIsTooLong()
    {
        // Arrange
        var validator = new UpdatePostCommandValidator();
        var longTitle = new string('a', 201);
        var command = new UpdatePostCommand(Guid.NewGuid(), longTitle, "Valid Content", "category-1");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void UpdatePostCommandValidator_ShouldNotHaveError_WhenValid()
    {
        // Arrange
        var validator = new UpdatePostCommandValidator();
        var command = new UpdatePostCommand(Guid.NewGuid(), "Valid Title", "Valid Content", "category-1");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
        result.ShouldNotHaveValidationErrorFor(x => x.Content);
    }
}
