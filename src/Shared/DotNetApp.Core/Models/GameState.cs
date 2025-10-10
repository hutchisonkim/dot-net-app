using System;

namespace DotNetApp.Core.Models
{
    /// <summary>
    /// Base model for game state.
    /// </summary>
    public class GameState
    {
        /// <summary>
        /// Unique identifier for the game.
        /// </summary>
        public string GameId { get; set; } = string.Empty;

        /// <summary>
        /// Type of game (e.g., "Chess", "Pong").
        /// </summary>
        public string GameType { get; set; } = string.Empty;

        /// <summary>
        /// JSON-serialized game-specific state data.
        /// </summary>
        public string StateData { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the game was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the game state was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
