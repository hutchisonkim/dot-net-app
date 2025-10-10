using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DotNetApp.Core.Abstractions;
using DotNetApp.Core.Models;

namespace DotNetApp.Core.Services
{
    /// <summary>
    /// In-memory implementation of IGameStateService.
    /// Suitable for testing and single-instance scenarios.
    /// </summary>
    public class InMemoryGameStateStore : IGameStateService
    {
        private readonly ConcurrentDictionary<string, GameState> _store = new();

        public Task<GameState> SaveGameStateAsync(GameState gameState, CancellationToken cancellationToken = default)
        {
            gameState.UpdatedAt = System.DateTime.UtcNow;
            _store.AddOrUpdate(gameState.GameId, gameState, (_, _) => gameState);
            return Task.FromResult(gameState);
        }

        public Task<GameState?> GetGameStateAsync(string gameId, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue(gameId, out var gameState);
            return Task.FromResult(gameState);
        }

        public Task<bool> DeleteGameStateAsync(string gameId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_store.TryRemove(gameId, out _));
        }
    }
}
