using BlogApi.Domain.Entities;
using Xunit;

namespace BlogApi.Tests.UnitTests.Domain;

public class PostTests
{
    [Fact]
    public void AddRating_ShouldUpdateAverageRating()
    {
        // Arrange
        var post = new Post { Title = "Test", Content = "Test" };

        // Act
        post.AddRating(4);
        post.AddRating(5);

        // Assert
        Assert.Equal(4.5, post.AverageRating);
        Assert.Equal(2, post.TotalRatings);
    }
}
