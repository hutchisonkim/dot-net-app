using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DotNetApp.Core.Abstractions;
using DotNetApp.Core.Models;
using Microsoft.Azure.Cosmos;

namespace DotNetApp.Core.Services
{
    /// <summary>
    /// Cosmos DB implementation of IGameStateService.
    /// </summary>
    public class CosmosGameStateStore : IGameStateService
    {
        private readonly Container _container;

        public CosmosGameStateStore(Container container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public async Task<GameState> SaveGameStateAsync(GameState gameState, CancellationToken cancellationToken = default)
        {
            gameState.UpdatedAt = DateTime.UtcNow;
            var response = await _container.UpsertItemAsync(gameState, new PartitionKey(gameState.GameId), cancellationToken: cancellationToken);
            return response.Resource;
        }

        public async Task<GameState?> GetGameStateAsync(string gameId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _container.ReadItemAsync<GameState>(gameId, new PartitionKey(gameId), cancellationToken: cancellationToken);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<bool> DeleteGameStateAsync(string gameId, CancellationToken cancellationToken = default)
        {
            try
            {
                await _container.DeleteItemAsync<GameState>(gameId, new PartitionKey(gameId), cancellationToken: cancellationToken);
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}
