namespace DotNetApp.Core.Models
{
    /// <summary>
    /// Represents a position on a game board.
    /// </summary>
    public class Position
    {
        /// <summary>
        /// Row coordinate.
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// Column coordinate.
        /// </summary>
        public int Column { get; set; }

        public Position(int row, int column)
        {
            Row = row;
            Column = column;
        }

        /// <summary>
        /// Checks if this position equals another position.
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is Position other)
            {
                return Row == other.Row && Column == other.Column;
            }
            return false;
        }

        /// <summary>
        /// Gets hash code for the position.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }

        /// <summary>
        /// String representation of the position.
        /// </summary>
        public override string ToString()
        {
            return $"({Row},{Column})";
        }
    }
}
