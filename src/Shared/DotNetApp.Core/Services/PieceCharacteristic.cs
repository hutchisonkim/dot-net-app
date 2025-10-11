using DotNetApp.Core.Abstractions;

namespace DotNetApp.Core.Services
{
    /// <summary>
    /// Base implementation of a piece characteristic.
    /// </summary>
    public class PieceCharacteristic : IPieceCharacteristic
    {
        /// <summary>
        /// Name of the characteristic.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Maximum range of movement (-1 for unlimited).
        /// </summary>
        public int MaxRange { get; }

        /// <summary>
        /// Whether this characteristic requires a straight line path.
        /// </summary>
        public bool RequiresClearPath { get; }

        private readonly List<(int deltaRow, int deltaColumn)> _relativePositions;

        public PieceCharacteristic(
            string name,
            IEnumerable<(int deltaRow, int deltaColumn)> relativePositions,
            int maxRange = -1,
            bool requiresClearPath = false)
        {
            Name = name;
            MaxRange = maxRange;
            RequiresClearPath = requiresClearPath;
            _relativePositions = new List<(int, int)>(relativePositions);
        }

        /// <summary>
        /// Gets the relative positions this characteristic allows.
        /// </summary>
        public IEnumerable<(int deltaRow, int deltaColumn)> GetRelativePositions()
        {
            return _relativePositions;
        }
    }
}
