using DotNetApp.Core.Models;

namespace DotNetApp.Core.Abstractions
{
    /// <summary>
    /// Represents a policy engine for managing game rules.
    /// </summary>
    /// <typeparam name="TBoardState">The type representing the board state.</typeparam>
    public interface IGamePolicy<TBoardState>
    {
        /// <summary>
        /// Gets all available abilities (possible moves) for a given position.
        /// </summary>
        /// <param name="position">The position to get abilities for.</param>
        /// <param name="boardState">Current state of the board.</param>
        /// <returns>Collection of abilities with their allowed status.</returns>
        IEnumerable<IAbility> GetAbilities(Position position, TBoardState boardState);

        /// <summary>
        /// Gets characteristics for a piece at a given position.
        /// </summary>
        /// <param name="position">The position to get characteristics for.</param>
        /// <param name="boardState">Current state of the board.</param>
        /// <returns>Collection of characteristics that define piece behavior.</returns>
        IEnumerable<IPieceCharacteristic> GetCharacteristics(Position position, TBoardState boardState);

        /// <summary>
        /// Validates if a move is legal according to game rules.
        /// </summary>
        /// <param name="move">The move to validate.</param>
        /// <param name="boardState">Current state of the board.</param>
        /// <returns>True if the move is legal, false otherwise.</returns>
        bool IsValidMove(Move move, TBoardState boardState);
    }
}
