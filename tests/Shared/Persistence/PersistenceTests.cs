using System;
using System.Threading.Tasks;
using DotNetApp.Core.Models;
using DotNetApp.Core.Services;
using Xunit;

namespace Examples.Tests.Integration;

/// <summary>
/// Integration tests for Chess game persistence functionality.
/// Tests the InMemoryGameStateStore with chess-specific scenarios.
/// Follows MSDN and xUnit testing guidelines.
/// </summary>
[Trait("Category", "Integration")]
public class ChessPersistenceTests
{
    [Fact]
    public async Task SaveGameStateAsync_NewChessGame_StoresAndRetrieves()
    {
        // Arrange
        var store = new InMemoryGameStateStore();
        var gameId = Guid.NewGuid().ToString();
        var chessState = new GameState
        {
            GameId = gameId,
            GameType = "Chess",
            StateData = "{\"board\":\"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR\",\"turn\":\"white\"}",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var savedState = await store.SaveGameStateAsync(chessState);
        var retrievedState = await store.GetGameStateAsync(gameId);

        // Assert
        Assert.NotNull(savedState);
        Assert.NotNull(retrievedState);
        Assert.Equal(gameId, retrievedState.GameId);
        Assert.Equal("Chess", retrievedState.GameType);
        Assert.Contains("rnbqkbnr", retrievedState.StateData);
    }

    [Fact]
    public async Task SaveGameStateAsync_UpdatedChessGame_PreservesGameId()
    {
        // Arrange
        var store = new InMemoryGameStateStore();
        var gameId = Guid.NewGuid().ToString();
        var initialState = new GameState
        {
            GameId = gameId,
            GameType = "Chess",
            StateData = "{\"board\":\"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR\",\"turn\":\"white\"}",
            CreatedAt = DateTime.UtcNow
        };

        await store.SaveGameStateAsync(initialState);

        // Act - Simulate a move
        var updatedState = new GameState
        {
            GameId = gameId,
            GameType = "Chess",
            StateData = "{\"board\":\"rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR\",\"turn\":\"black\"}",
            CreatedAt = initialState.CreatedAt
        };

        await store.SaveGameStateAsync(updatedState);
        var retrievedState = await store.GetGameStateAsync(gameId);

        // Assert
        Assert.NotNull(retrievedState);
        Assert.Equal(gameId, retrievedState.GameId);
        Assert.Contains("4P3", retrievedState.StateData);
        Assert.Contains("black", retrievedState.StateData);
    }

    [Fact]
    public async Task DeleteGameStateAsync_ExistingChessGame_RemovesFromStore()
    {
        // Arrange
        var store = new InMemoryGameStateStore();
        var gameId = Guid.NewGuid().ToString();
        var gameState = new GameState
        {
            GameId = gameId,
            GameType = "Chess",
            StateData = "{\"board\":\"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR\"}",
            CreatedAt = DateTime.UtcNow
        };

        await store.SaveGameStateAsync(gameState);

        // Act
        var deleted = await store.DeleteGameStateAsync(gameId);
        var retrievedState = await store.GetGameStateAsync(gameId);

        // Assert
        Assert.True(deleted);
        Assert.Null(retrievedState);
    }

    [Fact]
    public async Task SaveGameStateAsync_MultipleChessGames_StoresIndependently()
    {
        // Arrange
        var store = new InMemoryGameStateStore();
        var game1Id = Guid.NewGuid().ToString();
        var game2Id = Guid.NewGuid().ToString();

        var game1 = new GameState
        {
            GameId = game1Id,
            GameType = "Chess",
            StateData = "{\"board\":\"game1\"}",
            CreatedAt = DateTime.UtcNow
        };

        var game2 = new GameState
        {
            GameId = game2Id,
            GameType = "Chess",
            StateData = "{\"board\":\"game2\"}",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await store.SaveGameStateAsync(game1);
        await store.SaveGameStateAsync(game2);

        var retrieved1 = await store.GetGameStateAsync(game1Id);
        var retrieved2 = await store.GetGameStateAsync(game2Id);

        // Assert
        Assert.NotNull(retrieved1);
        Assert.NotNull(retrieved2);
        Assert.Contains("game1", retrieved1.StateData);
        Assert.Contains("game2", retrieved2.StateData);
    }
}
