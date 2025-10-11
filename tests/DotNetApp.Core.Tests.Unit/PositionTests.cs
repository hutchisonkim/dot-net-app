using DotNetApp.Core.Models;
using Xunit;

namespace DotNetApp.Core.Tests.Unit;

/// <summary>
/// Unit tests for Position model.
/// Follows MSDN and xUnit testing guidelines.
/// </summary>
[Trait("Category", "Unit")]
public class PositionTests
{
    [Fact]
    public void Position_Constructor_SetsRowAndColumn()
    {
        // Arrange & Act
        var position = new Position(3, 5);

        // Assert
        Assert.Equal(3, position.Row);
        Assert.Equal(5, position.Column);
    }

    [Fact]
    public void Position_Equals_ReturnsTrueForSameCoordinates()
    {
        // Arrange
        var position1 = new Position(2, 4);
        var position2 = new Position(2, 4);

        // Act & Assert
        Assert.True(position1.Equals(position2));
        Assert.Equal(position1.GetHashCode(), position2.GetHashCode());
    }

    [Fact]
    public void Position_Equals_ReturnsFalseForDifferentCoordinates()
    {
        // Arrange
        var position1 = new Position(2, 4);
        var position2 = new Position(2, 5);

        // Act & Assert
        Assert.False(position1.Equals(position2));
    }

    [Fact]
    public void Position_Equals_ReturnsFalseForNull()
    {
        // Arrange
        var position = new Position(2, 4);

        // Act & Assert
        Assert.False(position.Equals(null));
    }

    [Fact]
    public void Position_ToString_ReturnsFormattedString()
    {
        // Arrange
        var position = new Position(3, 7);

        // Act
        var result = position.ToString();

        // Assert
        Assert.Equal("(3,7)", result);
    }
}
