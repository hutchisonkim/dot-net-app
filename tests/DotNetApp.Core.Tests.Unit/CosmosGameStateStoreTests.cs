using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DotNetApp.Core.Models;
using DotNetApp.Core.Services;
using Microsoft.Azure.Cosmos;
using Moq;
using Xunit;

namespace DotNetApp.Core.Tests.Unit;

[Trait("Category", "Unit")]
public class CosmosGameStateStoreTests
{
    [Fact]
    public async Task SaveGameStateAsync_UpsertItem()
    {
        // Arrange
        var mockContainer = new Mock<Container>();
        var gameState = new GameState
        {
            GameId = "game-1",
            GameType = "Chess",
            StateData = "{\"board\":\"initial\"}",
            CreatedAt = DateTime.UtcNow
        };

        var mockResponse = new Mock<ItemResponse<GameState>>();
        mockResponse.Setup(r => r.Resource).Returns(gameState);

        mockContainer
            .Setup(c => c.UpsertItemAsync(
                It.IsAny<GameState>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        var store = new CosmosGameStateStore(mockContainer.Object);

        // Act
        var result = await store.SaveGameStateAsync(gameState);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("game-1", result.GameId);
        mockContainer.Verify(c => c.UpsertItemAsync(
            It.IsAny<GameState>(),
            It.IsAny<PartitionKey>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetGameStateAsync_ReturnsExistingItem()
    {
        // Arrange
        var mockContainer = new Mock<Container>();
        var gameState = new GameState
        {
            GameId = "game-1",
            GameType = "Chess",
            StateData = "{\"board\":\"initial\"}",
            CreatedAt = DateTime.UtcNow
        };

        var mockResponse = new Mock<ItemResponse<GameState>>();
        mockResponse.Setup(r => r.Resource).Returns(gameState);

        mockContainer
            .Setup(c => c.ReadItemAsync<GameState>(
                "game-1",
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        var store = new CosmosGameStateStore(mockContainer.Object);

        // Act
        var result = await store.GetGameStateAsync("game-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("game-1", result.GameId);
    }

    [Fact]
    public async Task GetGameStateAsync_ReturnsNullWhenNotFound()
    {
        // Arrange
        var mockContainer = new Mock<Container>();
        
        mockContainer
            .Setup(c => c.ReadItemAsync<GameState>(
                "non-existent",
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, "", 0));

        var store = new CosmosGameStateStore(mockContainer.Object);

        // Act
        var result = await store.GetGameStateAsync("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteGameStateAsync_DeletesExistingItem()
    {
        // Arrange
        var mockContainer = new Mock<Container>();
        var mockResponse = new Mock<ItemResponse<GameState>>();

        mockContainer
            .Setup(c => c.DeleteItemAsync<GameState>(
                "game-1",
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        var store = new CosmosGameStateStore(mockContainer.Object);

        // Act
        var result = await store.DeleteGameStateAsync("game-1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteGameStateAsync_ReturnsFalseWhenNotFound()
    {
        // Arrange
        var mockContainer = new Mock<Container>();
        
        mockContainer
            .Setup(c => c.DeleteItemAsync<GameState>(
                "non-existent",
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, "", 0));

        var store = new CosmosGameStateStore(mockContainer.Object);

        // Act
        var result = await store.DeleteGameStateAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Constructor_ThrowsWhenContainerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CosmosGameStateStore(null!));
    }
}
