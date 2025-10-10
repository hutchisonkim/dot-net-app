using System.Threading;
using System.Threading.Tasks;
using DotNetApp.Core.Models;

namespace DotNetApp.Core.Abstractions
{
    /// <summary>
    /// Service for persisting and retrieving game state.
    /// </summary>
    public interface IGameStateService
    {
        /// <summary>
        /// Creates or updates a game state.
        /// </summary>
        Task<GameState> SaveGameStateAsync(GameState gameState, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a game state by its ID.
        /// </summary>
        Task<GameState?> GetGameStateAsync(string gameId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a game state by its ID.
        /// </summary>
        Task<bool> DeleteGameStateAsync(string gameId, CancellationToken cancellationToken = default);
    }
}
