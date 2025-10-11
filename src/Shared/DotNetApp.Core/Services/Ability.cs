using DotNetApp.Core.Abstractions;
using DotNetApp.Core.Models;

namespace DotNetApp.Core.Services
{
    /// <summary>
    /// Concrete implementation of an ability.
    /// </summary>
    public class Ability : IAbility
    {
        /// <summary>
        /// The move that this ability represents.
        /// </summary>
        public Move Move { get; }

        /// <summary>
        /// Whether this ability is currently allowed based on game state.
        /// </summary>
        public bool IsAllowed { get; }

        /// <summary>
        /// Reason why the ability is forbidden (if IsAllowed is false).
        /// </summary>
        public string? ForbiddenReason { get; }

        public Ability(Move move, bool isAllowed, string? forbiddenReason = null)
        {
            Move = move;
            IsAllowed = isAllowed;
            ForbiddenReason = forbiddenReason;
        }
    }
}
