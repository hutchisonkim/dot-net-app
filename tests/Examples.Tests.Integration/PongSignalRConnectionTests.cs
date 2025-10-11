using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Examples.Tests.Integration;

/// <summary>
/// Integration tests for Pong game SignalR connection.
/// Tests actual connection to a running SignalR server following MSDN and xUnit guidelines.
/// </summary>
[Trait("Category", "Integration")]
public class PongSignalRConnectionTests : IClassFixture<WebApplicationFactory<DotNetApp.Server.Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<DotNetApp.Server.Program> _factory;
    private HubConnection? _hubConnection;

    public PongSignalRConnectionTests(WebApplicationFactory<DotNetApp.Server.Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// xUnit lifecycle method - called before each test.
    /// </summary>
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// xUnit lifecycle method - called after each test to clean up resources.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests successful connection to SignalR GameHub.
    /// Verifies that a client can connect when server is running.
    /// </summary>
    [Fact]
    public async Task Pong_ConnectToGameHub_SuccessfulConnection()
    {
        // Arrange - Create a test server and SignalR client
        var client = _factory.Server.CreateClient();
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{client.BaseAddress}gamehub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        // Act - Connect to the SignalR hub
        await _hubConnection.StartAsync();

        // Assert - Verify connection is established
        Assert.Equal(HubConnectionState.Connected, _hubConnection.State);
    }

    /// <summary>
    /// Tests full connection lifecycle: connect, verify state, and disconnect.
    /// Ensures proper cleanup and state transitions.
    /// </summary>
    [Fact]
    public async Task Pong_ConnectToGameHub_FullLifecycle()
    {
        // Arrange
        var client = _factory.Server.CreateClient();
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{client.BaseAddress}gamehub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        // Assert initial state
        Assert.Equal(HubConnectionState.Disconnected, _hubConnection.State);

        // Act - Connect
        await _hubConnection.StartAsync();

        // Assert connected state
        Assert.Equal(HubConnectionState.Connected, _hubConnection.State);

        // Act - Disconnect
        await _hubConnection.StopAsync();

        // Assert disconnected state
        Assert.Equal(HubConnectionState.Disconnected, _hubConnection.State);
    }

    /// <summary>
    /// Tests that connection receives "Connected to SignalR hub" equivalent by verifying connection.
    /// Simulates what the Pong UI component does when connecting successfully.
    /// </summary>
    [Fact]
    public async Task Pong_ConnectToGameHub_ReceivesConnectionConfirmation()
    {
        // Arrange
        var client = _factory.Server.CreateClient();
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{client.BaseAddress}gamehub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var connectionEstablished = false;
        _hubConnection.Closed += error =>
        {
            connectionEstablished = false;
            return Task.CompletedTask;
        };

        // Act - Establish connection
        await _hubConnection.StartAsync();
        connectionEstablished = _hubConnection.State == HubConnectionState.Connected;

        // Assert - Connection is established
        Assert.True(connectionEstablished, "Connection should be established");
        Assert.Equal(HubConnectionState.Connected, _hubConnection.State);
        
        // Verify the connection ID is assigned
        Assert.NotNull(_hubConnection.ConnectionId);
        Assert.NotEmpty(_hubConnection.ConnectionId);
    }

    /// <summary>
    /// Tests that Pong can join a game after establishing connection.
    /// Verifies the complete flow: connect -> join game -> receive confirmation.
    /// </summary>
    [Fact]
    public async Task Pong_ConnectAndJoinGame_SuccessfulJoin()
    {
        // Arrange
        var client = _factory.Server.CreateClient();
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{client.BaseAddress}gamehub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var playerJoinedReceived = new TaskCompletionSource<bool>();

        _hubConnection.On<string, DateTime>("PlayerJoined", (connectionId, timestamp) =>
        {
            playerJoinedReceived.TrySetResult(true);
        });

        // Act - Connect and join game
        await _hubConnection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, _hubConnection.State);

        await _hubConnection.InvokeAsync("JoinGame", "pong-room-1");
        var joinConfirmed = await Task.WhenAny(playerJoinedReceived.Task, Task.Delay(5000)) == playerJoinedReceived.Task;

        // Assert - Join was successful
        Assert.True(joinConfirmed, "Should receive PlayerJoined event after joining game");
        Assert.Equal(HubConnectionState.Connected, _hubConnection.State);
    }
}
