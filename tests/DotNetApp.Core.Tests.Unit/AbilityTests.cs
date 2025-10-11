using DotNetApp.Core.Models;
using DotNetApp.Core.Services;
using Xunit;

namespace DotNetApp.Core.Tests.Unit;

/// <summary>
/// Unit tests for Ability implementation.
/// Follows MSDN and xUnit testing guidelines.
/// </summary>
[Trait("Category", "Unit")]
public class AbilityTests
{
    [Fact]
    public void Ability_Constructor_SetsProperties()
    {
        // Arrange
        var move = new Move(new Position(1, 2), new Position(3, 4));

        // Act
        var ability = new Ability(move, true);

        // Assert
        Assert.Equal(move, ability.Move);
        Assert.True(ability.IsAllowed);
        Assert.Null(ability.ForbiddenReason);
    }

    [Fact]
    public void Ability_Constructor_SetsForbiddenReason()
    {
        // Arrange
        var move = new Move(new Position(1, 2), new Position(3, 4));

        // Act
        var ability = new Ability(move, false, "Test reason");

        // Assert
        Assert.False(ability.IsAllowed);
        Assert.Equal("Test reason", ability.ForbiddenReason);
    }

    [Fact]
    public void Ability_ForbiddenReason_CanBeNullWhenAllowed()
    {
        // Arrange
        var move = new Move(new Position(1, 2), new Position(3, 4));

        // Act
        var ability = new Ability(move, true, null);

        // Assert
        Assert.True(ability.IsAllowed);
        Assert.Null(ability.ForbiddenReason);
    }
}
