using System;
using System.Threading.Tasks;
using DotNetApp.Core.Models;
using DotNetApp.Core.Services;
using Xunit;

namespace DotNetApp.Core.Tests.Unit;

[Trait("Category", "Unit")]
public class InMemoryGameStateStoreTests
{
    [Fact]
    public async Task SaveGameStateAsync_CreatesNewGameState()
    {
        // Arrange
        var store = new InMemoryGameStateStore();
        var gameState = new GameState
        {
            GameId = "game-1",
            GameType = "Chess",
            StateData = "{\"board\":\"initial\"}",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await store.SaveGameStateAsync(gameState);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("game-1", result.GameId);
        Assert.Equal("Chess", result.GameType);
        Assert.True(result.UpdatedAt >= gameState.CreatedAt);
    }

    [Fact]
    public async Task SaveGameStateAsync_UpdatesExistingGameState()
    {
        // Arrange
        var store = new InMemoryGameStateStore();
        var gameState = new GameState
        {
            GameId = "game-1",
            GameType = "Chess",
            StateData = "{\"board\":\"initial\"}",
            CreatedAt = DateTime.UtcNow
        };
        await store.SaveGameStateAsync(gameState);

        // Act
        gameState.StateData = "{\"board\":\"modified\"}";
        var result = await store.SaveGameStateAsync(gameState);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("{\"board\":\"modified\"}", result.StateData);
    }

    [Fact]
    public async Task GetGameStateAsync_ReturnsExistingGameState()
    {
        // Arrange
        var store = new InMemoryGameStateStore();
        var gameState = new GameState
        {
            GameId = "game-1",
            GameType = "Chess",
            StateData = "{\"board\":\"initial\"}",
            CreatedAt = DateTime.UtcNow
        };
        await store.SaveGameStateAsync(gameState);

        // Act
        var result = await store.GetGameStateAsync("game-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("game-1", result.GameId);
        Assert.Equal("Chess", result.GameType);
    }

    [Fact]
    public async Task GetGameStateAsync_ReturnsNullForNonExistentGame()
    {
        // Arrange
        var store = new InMemoryGameStateStore();

        // Act
        var result = await store.GetGameStateAsync("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteGameStateAsync_RemovesExistingGame()
    {
        // Arrange
        var store = new InMemoryGameStateStore();
        var gameState = new GameState
        {
            GameId = "game-1",
            GameType = "Chess",
            StateData = "{\"board\":\"initial\"}",
            CreatedAt = DateTime.UtcNow
        };
        await store.SaveGameStateAsync(gameState);

        // Act
        var deleted = await store.DeleteGameStateAsync("game-1");
        var result = await store.GetGameStateAsync("game-1");

        // Assert
        Assert.True(deleted);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteGameStateAsync_ReturnsFalseForNonExistentGame()
    {
        // Arrange
        var store = new InMemoryGameStateStore();

        // Act
        var deleted = await store.DeleteGameStateAsync("non-existent");

        // Assert
        Assert.False(deleted);
    }
}
