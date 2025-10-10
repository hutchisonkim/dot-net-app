using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using DotNetApp.Core.Messages;

namespace DotNetApp.Client.Services
{
    /// <summary>
    /// Client-side wrapper for SignalR game hub connection.
    /// </summary>
    public class GameHubClient : IAsyncDisposable
    {
        private readonly HubConnection _connection;

        public GameHubClient(string hubUrl)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();
        }

        public event Action<string, DateTime>? PlayerJoined;
        public event Action<string, DateTime>? PlayerLeft;
        public event Action<GameMessage>? GameMessageReceived;

        public async Task StartAsync()
        {
            _connection.On<string, DateTime>("PlayerJoined", (connectionId, timestamp) =>
            {
                PlayerJoined?.Invoke(connectionId, timestamp);
            });

            _connection.On<string, DateTime>("PlayerLeft", (connectionId, timestamp) =>
            {
                PlayerLeft?.Invoke(connectionId, timestamp);
            });

            _connection.On<GameMessage>("ReceiveGameMessage", (message) =>
            {
                GameMessageReceived?.Invoke(message);
            });

            await _connection.StartAsync();
        }

        public async Task JoinGameAsync(string gameId)
        {
            await _connection.InvokeAsync("JoinGame", gameId);
        }

        public async Task LeaveGameAsync(string gameId)
        {
            await _connection.InvokeAsync("LeaveGame", gameId);
        }

        public async Task SendGameMessageAsync(GameMessage message)
        {
            await _connection.InvokeAsync("SendGameMessage", message);
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
