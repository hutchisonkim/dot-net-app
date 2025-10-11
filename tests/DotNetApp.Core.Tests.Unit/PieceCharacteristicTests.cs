using DotNetApp.Core.Models;
using DotNetApp.Core.Services;
using Xunit;

namespace DotNetApp.Core.Tests.Unit;

/// <summary>
/// Unit tests for PieceCharacteristic implementation.
/// Follows MSDN and xUnit testing guidelines.
/// </summary>
[Trait("Category", "Unit")]
public class PieceCharacteristicTests
{
    [Fact]
    public void PieceCharacteristic_Constructor_SetsProperties()
    {
        // Arrange
        var positions = new[] { (1, 0), (-1, 0) };

        // Act
        var characteristic = new PieceCharacteristic(
            "TestCharacteristic",
            positions,
            maxRange: 5,
            requiresClearPath: true);

        // Assert
        Assert.Equal("TestCharacteristic", characteristic.Name);
        Assert.Equal(5, characteristic.MaxRange);
        Assert.True(characteristic.RequiresClearPath);
    }

    [Fact]
    public void PieceCharacteristic_GetRelativePositions_ReturnsAllPositions()
    {
        // Arrange
        var positions = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
        var characteristic = new PieceCharacteristic("Cross", positions);

        // Act
        var result = characteristic.GetRelativePositions().ToList();

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Contains((1, 0), result);
        Assert.Contains((-1, 0), result);
        Assert.Contains((0, 1), result);
        Assert.Contains((0, -1), result);
    }

    [Fact]
    public void PieceCharacteristic_DefaultMaxRange_IsUnlimited()
    {
        // Arrange & Act
        var characteristic = new PieceCharacteristic(
            "Unlimited",
            new[] { (1, 1) });

        // Assert
        Assert.Equal(-1, characteristic.MaxRange);
    }

    [Fact]
    public void PieceCharacteristic_DefaultRequiresClearPath_IsFalse()
    {
        // Arrange & Act
        var characteristic = new PieceCharacteristic(
            "Jump",
            new[] { (2, 1) });

        // Assert
        Assert.False(characteristic.RequiresClearPath);
    }
}
