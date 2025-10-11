using DotNetApp.Core.Models;

namespace DotNetApp.Core.Abstractions
{
    /// <summary>
    /// Represents a possible action that a piece can perform.
    /// </summary>
    public interface IAbility
    {
        /// <summary>
        /// The move that this ability represents.
        /// </summary>
        Move Move { get; }

        /// <summary>
        /// Whether this ability is currently allowed based on game state.
        /// </summary>
        bool IsAllowed { get; }

        /// <summary>
        /// Reason why the ability is forbidden (if IsAllowed is false).
        /// </summary>
        string? ForbiddenReason { get; }
    }
}
