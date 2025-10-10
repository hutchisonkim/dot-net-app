using System;

namespace DotNetApp.Core.Messages
{
    /// <summary>
    /// Base message for real-time game communication.
    /// </summary>
    public class GameMessage
    {
        /// <summary>
        /// Unique identifier for the game room.
        /// </summary>
        public string GameId { get; set; } = string.Empty;

        /// <summary>
        /// Type of message (e.g., "move", "join", "leave").
        /// </summary>
        public string MessageType { get; set; } = string.Empty;

        /// <summary>
        /// JSON-serialized message payload.
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the message was sent.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
