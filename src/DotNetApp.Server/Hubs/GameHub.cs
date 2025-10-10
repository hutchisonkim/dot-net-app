using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using DotNetApp.Core.Messages;

namespace DotNetApp.Server.Hubs
{
    /// <summary>
    /// SignalR hub for real-time game communication.
    /// </summary>
    public class GameHub : Hub
    {
        /// <summary>
        /// Join a game room.
        /// </summary>
        public async Task JoinGame(string gameId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("PlayerJoined", Context.ConnectionId, DateTime.UtcNow);
        }

        /// <summary>
        /// Leave a game room.
        /// </summary>
        public async Task LeaveGame(string gameId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("PlayerLeft", Context.ConnectionId, DateTime.UtcNow);
        }

        /// <summary>
        /// Send a game message to all players in a game room.
        /// </summary>
        public async Task SendGameMessage(GameMessage message)
        {
            await Clients.Group(message.GameId).SendAsync("ReceiveGameMessage", message);
        }

        /// <summary>
        /// Called when a connection is established.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a connection is terminated.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
