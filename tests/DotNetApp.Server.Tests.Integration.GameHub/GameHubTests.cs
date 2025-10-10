using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using DotNetApp.Core.Messages;
using Xunit;

namespace DotNetApp.Server.Tests.Integration.GameHub;

[Trait("Category", "Integration")]
public class GameHubTests : IClassFixture<WebApplicationFactory<DotNetApp.Server.Program>>
{
    private readonly WebApplicationFactory<DotNetApp.Server.Program> _factory;

    public GameHubTests(WebApplicationFactory<DotNetApp.Server.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task JoinGame_NotifiesOtherPlayers()
    {
        // Arrange
        var client = _factory.Server.CreateClient();
        var hubConnection1 = new HubConnectionBuilder()
            .WithUrl($"{client.BaseAddress}gamehub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var hubConnection2 = new HubConnectionBuilder()
            .WithUrl($"{client.BaseAddress}gamehub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var playerJoinedTcs = new TaskCompletionSource<(string ConnectionId, DateTime Timestamp)>();
        hubConnection2.On<string, DateTime>("PlayerJoined", (connectionId, timestamp) =>
        {
            playerJoinedTcs.SetResult((connectionId, timestamp));
        });

        // Act
        await hubConnection1.StartAsync();
        await hubConnection2.StartAsync();
        await hubConnection2.InvokeAsync("JoinGame", "game-123");
        await hubConnection1.InvokeAsync("JoinGame", "game-123");
        var result = await Task.WhenAny(playerJoinedTcs.Task, Task.Delay(5000));

        // Assert
        Assert.Equal(playerJoinedTcs.Task, result);
        var (connectionId, timestamp) = await playerJoinedTcs.Task;
        Assert.NotNull(connectionId);
        Assert.NotEqual(default, timestamp);

        // Cleanup
        await hubConnection1.StopAsync();
        await hubConnection2.StopAsync();
        await hubConnection1.DisposeAsync();
        await hubConnection2.DisposeAsync();
    }

    [Fact]
    public async Task LeaveGame_NotifiesOtherPlayers()
    {
        // Arrange
        var client = _factory.Server.CreateClient();
        var hubConnection1 = new HubConnectionBuilder()
            .WithUrl($"{client.BaseAddress}gamehub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var hubConnection2 = new HubConnectionBuilder()
            .WithUrl($"{client.BaseAddress}gamehub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var playerLeftTcs = new TaskCompletionSource<(string ConnectionId, DateTime Timestamp)>();
        hubConnection2.On<string, DateTime>("PlayerLeft", (connectionId, timestamp) =>
        {
            playerLeftTcs.SetResult((connectionId, timestamp));
        });

        // Act
        await hubConnection1.StartAsync();
        await hubConnection2.StartAsync();
        await hubConnection1.InvokeAsync("JoinGame", "game-123");
        await hubConnection2.InvokeAsync("JoinGame", "game-123");
        await hubConnection1.InvokeAsync("LeaveGame", "game-123");
        var result = await Task.WhenAny(playerLeftTcs.Task, Task.Delay(5000));

        // Assert
        Assert.Equal(playerLeftTcs.Task, result);
        var (connectionId, timestamp) = await playerLeftTcs.Task;
        Assert.NotNull(connectionId);
        Assert.NotEqual(default, timestamp);

        // Cleanup
        await hubConnection1.StopAsync();
        await hubConnection2.StopAsync();
        await hubConnection1.DisposeAsync();
        await hubConnection2.DisposeAsync();
    }

    [Fact]
    public async Task SendGameMessage_BroadcastsToGameRoom()
    {
        // Arrange
        var client1 = _factory.Server.CreateClient();
        var client2 = _factory.Server.CreateClient();

        var connection1 = new HubConnectionBuilder()
            .WithUrl($"{client1.BaseAddress}gamehub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var connection2 = new HubConnectionBuilder()
            .WithUrl($"{client2.BaseAddress}gamehub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var messageTcs = new TaskCompletionSource<GameMessage>();
        connection2.On<GameMessage>("ReceiveGameMessage", (message) =>
        {
            messageTcs.SetResult(message);
        });

        // Act
        await connection1.StartAsync();
        await connection2.StartAsync();
        await connection1.InvokeAsync("JoinGame", "game-123");
        await connection2.InvokeAsync("JoinGame", "game-123");

        var testMessage = new GameMessage
        {
            GameId = "game-123",
            MessageType = "move",
            Payload = "{\"from\":\"e2\",\"to\":\"e4\"}",
            Timestamp = DateTime.UtcNow
        };

        await connection1.InvokeAsync("SendGameMessage", testMessage);
        var result = await Task.WhenAny(messageTcs.Task, Task.Delay(5000));

        // Assert
        Assert.Equal(messageTcs.Task, result);
        var receivedMessage = await messageTcs.Task;
        Assert.NotNull(receivedMessage);
        Assert.Equal("game-123", receivedMessage.GameId);
        Assert.Equal("move", receivedMessage.MessageType);
        Assert.Contains("e2", receivedMessage.Payload);

        // Cleanup
        await connection1.StopAsync();
        await connection2.StopAsync();
        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
    }
}
