using DotNetApp.Core.Models;

namespace DotNetApp.Core.Abstractions
{
    /// <summary>
    /// Represents a characteristic or behavior pattern of a game piece.
    /// </summary>
    public interface IPieceCharacteristic
    {
        /// <summary>
        /// Name of the characteristic (e.g., "Diagonal", "Forward", "LShape").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the relative positions this characteristic allows from a given position.
        /// Returns offsets (deltaRow, deltaColumn).
        /// </summary>
        IEnumerable<(int deltaRow, int deltaColumn)> GetRelativePositions();

        /// <summary>
        /// Maximum range of movement (-1 for unlimited).
        /// </summary>
        int MaxRange { get; }

        /// <summary>
        /// Whether this characteristic requires a straight line path (for blocking checks).
        /// </summary>
        bool RequiresClearPath { get; }
    }
}
