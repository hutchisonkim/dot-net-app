using DotNetApp.Core.Models;
using Xunit;

namespace DotNetApp.Core.Tests.Unit;

/// <summary>
/// Unit tests for Move model.
/// Follows MSDN and xUnit testing guidelines.
/// </summary>
[Trait("Category", "Unit")]
public class MoveTests
{
    [Fact]
    public void Move_Constructor_SetsFromAndTo()
    {
        // Arrange
        var from = new Position(1, 2);
        var to = new Position(3, 4);

        // Act
        var move = new Move(from, to);

        // Assert
        Assert.Equal(from, move.From);
        Assert.Equal(to, move.To);
        Assert.False(move.IsCapture);
    }

    [Fact]
    public void Move_Constructor_SetsIsCaptureWhenSpecified()
    {
        // Arrange
        var from = new Position(1, 2);
        var to = new Position(3, 4);

        // Act
        var move = new Move(from, to, true);

        // Assert
        Assert.True(move.IsCapture);
    }

    [Fact]
    public void Move_ToString_ReturnsFormattedString()
    {
        // Arrange
        var from = new Position(1, 2);
        var to = new Position(3, 4);
        var move = new Move(from, to);

        // Act
        var result = move.ToString();

        // Assert
        Assert.Equal("(1,2) -> (3,4)", result);
    }

    [Fact]
    public void Move_ToString_IncludesCaptureIndicator()
    {
        // Arrange
        var from = new Position(1, 2);
        var to = new Position(3, 4);
        var move = new Move(from, to, true);

        // Act
        var result = move.ToString();

        // Assert
        Assert.Contains("capture", result);
    }
}
