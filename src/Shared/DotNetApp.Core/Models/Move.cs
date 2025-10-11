namespace DotNetApp.Core.Models
{
    /// <summary>
    /// Represents a move from one position to another.
    /// </summary>
    public class Move
    {
        /// <summary>
        /// Starting position.
        /// </summary>
        public Position From { get; set; }

        /// <summary>
        /// Ending position.
        /// </summary>
        public Position To { get; set; }

        /// <summary>
        /// Whether this move is a capture.
        /// </summary>
        public bool IsCapture { get; set; }

        public Move(Position from, Position to, bool isCapture = false)
        {
            From = from;
            To = to;
            IsCapture = isCapture;
        }

        /// <summary>
        /// String representation of the move.
        /// </summary>
        public override string ToString()
        {
            return $"{From} -> {To}{(IsCapture ? " (capture)" : "")}";
        }
    }
}
