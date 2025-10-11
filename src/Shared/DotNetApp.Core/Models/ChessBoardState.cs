using DotNetApp.Core.Models;

namespace DotNetApp.Core.Models
{
    /// <summary>
    /// Represents a chess board state for policy evaluation.
    /// </summary>
    public class ChessBoardState
    {
        private readonly Dictionary<(int row, int col), ChessPiece> _pieces = new();

        /// <summary>
        /// Gets or sets a piece at a specific position.
        /// </summary>
        public ChessPiece? this[int row, int col]
        {
            get => _pieces.TryGetValue((row, col), out var piece) ? piece : null;
            set
            {
                if (value == null)
                {
                    _pieces.Remove((row, col));
                }
                else
                {
                    _pieces[(row, col)] = value;
                }
            }
        }

        /// <summary>
        /// Gets a piece at a specific position.
        /// </summary>
        public ChessPiece? GetPiece(Position position)
        {
            return this[position.Row, position.Column];
        }

        /// <summary>
        /// Checks if a position is within board boundaries.
        /// </summary>
        public bool IsInBounds(Position position)
        {
            return position.Row >= 0 && position.Row < 8 && 
                   position.Column >= 0 && position.Column < 8;
        }

        /// <summary>
        /// Checks if a position is occupied.
        /// </summary>
        public bool IsOccupied(Position position)
        {
            return GetPiece(position) != null;
        }

        /// <summary>
        /// Gets all pieces on the board.
        /// </summary>
        public IEnumerable<(Position position, ChessPiece piece)> GetAllPieces()
        {
            foreach (var kvp in _pieces)
            {
                yield return (new Position(kvp.Key.row, kvp.Key.col), kvp.Value);
            }
        }
    }

    /// <summary>
    /// Represents a chess piece with type and color.
    /// </summary>
    public class ChessPiece
    {
        /// <summary>
        /// Type of the piece.
        /// </summary>
        public ChessPieceType Type { get; set; }

        /// <summary>
        /// Color of the piece.
        /// </summary>
        public ChessColor Color { get; set; }

        /// <summary>
        /// Whether this piece has moved (for special moves like castling, en passant).
        /// </summary>
        public bool HasMoved { get; set; }

        public ChessPiece(ChessPieceType type, ChessColor color, bool hasMoved = false)
        {
            Type = type;
            Color = color;
            HasMoved = hasMoved;
        }
    }

    /// <summary>
    /// Types of chess pieces.
    /// </summary>
    public enum ChessPieceType
    {
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King
    }

    /// <summary>
    /// Chess piece colors.
    /// </summary>
    public enum ChessColor
    {
        White,
        Black
    }
}
