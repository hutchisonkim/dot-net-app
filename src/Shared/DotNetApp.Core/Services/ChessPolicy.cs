using DotNetApp.Core.Abstractions;
using DotNetApp.Core.Models;

namespace DotNetApp.Core.Services
{
    /// <summary>
    /// Policy engine for chess game rules.
    /// Implements characteristic-based ability system for chess pieces.
    /// </summary>
    public class ChessPolicy : IGamePolicy<ChessBoardState>
    {
        /// <summary>
        /// Gets all available abilities (possible moves) for a piece at a given position.
        /// </summary>
        public IEnumerable<IAbility> GetAbilities(Position position, ChessBoardState boardState)
        {
            var piece = boardState.GetPiece(position);
            if (piece == null)
            {
                yield break;
            }

            var characteristics = GetCharacteristics(position, boardState);
            
            foreach (var characteristic in characteristics)
            {
                foreach (var ability in GenerateAbilitiesFromCharacteristic(position, characteristic, boardState, piece))
                {
                    yield return ability;
                }
            }
        }

        /// <summary>
        /// Gets characteristics for a piece at a given position.
        /// </summary>
        public IEnumerable<IPieceCharacteristic> GetCharacteristics(Position position, ChessBoardState boardState)
        {
            var piece = boardState.GetPiece(position);
            if (piece == null)
            {
                yield break;
            }

            switch (piece.Type)
            {
                case ChessPieceType.Pawn:
                    yield return GetPawnForwardCharacteristic(piece.Color);
                    yield return GetPawnCaptureCharacteristic(piece.Color);
                    if (!piece.HasMoved)
                    {
                        yield return GetPawnDoubleForwardCharacteristic(piece.Color);
                    }
                    break;

                case ChessPieceType.Knight:
                    yield return GetKnightCharacteristic();
                    break;

                case ChessPieceType.Bishop:
                    yield return GetBishopCharacteristic();
                    break;

                case ChessPieceType.Rook:
                    yield return GetRookCharacteristic();
                    break;

                case ChessPieceType.Queen:
                    yield return GetQueenCharacteristic();
                    break;

                case ChessPieceType.King:
                    yield return GetKingCharacteristic();
                    break;
            }
        }

        /// <summary>
        /// Validates if a move is legal according to chess rules.
        /// </summary>
        public bool IsValidMove(Move move, ChessBoardState boardState)
        {
            var abilities = GetAbilities(move.From, boardState);
            return abilities.Any(a => a.IsAllowed && 
                                     a.Move.To.Equals(move.To) && 
                                     a.Move.IsCapture == move.IsCapture);
        }

        private IEnumerable<IAbility> GenerateAbilitiesFromCharacteristic(
            Position position, 
            IPieceCharacteristic characteristic, 
            ChessBoardState boardState,
            ChessPiece piece)
        {
            foreach (var (deltaRow, deltaColumn) in characteristic.GetRelativePositions())
            {
                if (characteristic.MaxRange == 1 || !characteristic.RequiresClearPath)
                {
                    // Single step or jump movement (pawn, knight, king)
                    var targetPos = new Position(position.Row + deltaRow, position.Column + deltaColumn);
                    
                    if (boardState.IsInBounds(targetPos))
                    {
                        var ability = EvaluateMove(position, targetPos, characteristic, boardState, piece);
                        yield return ability;
                    }
                }
                else
                {
                    // Sliding movement (rook, bishop, queen)
                    int distance = 1;
                    while (characteristic.MaxRange == -1 || distance <= characteristic.MaxRange)
                    {
                        var targetPos = new Position(
                            position.Row + (deltaRow * distance), 
                            position.Column + (deltaColumn * distance));

                        if (!boardState.IsInBounds(targetPos))
                        {
                            break;
                        }

                        var targetPiece = boardState.GetPiece(targetPos);
                        
                        if (targetPiece == null)
                        {
                            // Empty square - can move here
                            yield return new Ability(
                                new Move(position, targetPos, false),
                                true);
                        }
                        else if (targetPiece.Color != piece.Color)
                        {
                            // Enemy piece - can capture
                            yield return new Ability(
                                new Move(position, targetPos, true),
                                true);
                            break; // Can't move past this piece
                        }
                        else
                        {
                            // Friendly piece - blocked
                            yield return new Ability(
                                new Move(position, targetPos, false),
                                false,
                                "Blocked by friendly piece");
                            break;
                        }

                        distance++;
                    }
                }
            }
        }

        private IAbility EvaluateMove(
            Position from, 
            Position to, 
            IPieceCharacteristic characteristic,
            ChessBoardState boardState,
            ChessPiece piece)
        {
            var targetPiece = boardState.GetPiece(to);

            // Pawn forward movement
            if (characteristic.Name == "PawnForward" || characteristic.Name == "PawnDoubleForward")
            {
                if (targetPiece != null)
                {
                    return new Ability(
                        new Move(from, to, false),
                        false,
                        "Pawns cannot capture moving forward");
                }
                
                // Check if path is clear for double move
                if (characteristic.Name == "PawnDoubleForward")
                {
                    int direction = piece.Color == ChessColor.White ? -1 : 1;
                    var intermediatePos = new Position(from.Row + direction, from.Column);
                    if (boardState.IsOccupied(intermediatePos))
                    {
                        return new Ability(
                            new Move(from, to, false),
                            false,
                            "Path is blocked");
                    }
                }
                
                return new Ability(new Move(from, to, false), true);
            }

            // Pawn diagonal capture
            if (characteristic.Name == "PawnCapture")
            {
                if (targetPiece == null)
                {
                    return new Ability(
                        new Move(from, to, true),
                        false,
                        "No piece to capture");
                }
                
                if (targetPiece.Color == piece.Color)
                {
                    return new Ability(
                        new Move(from, to, true),
                        false,
                        "Cannot capture friendly piece");
                }
                
                return new Ability(new Move(from, to, true), true);
            }

            // Other pieces (knight, king)
            if (targetPiece == null)
            {
                return new Ability(new Move(from, to, false), true);
            }
            
            if (targetPiece.Color != piece.Color)
            {
                return new Ability(new Move(from, to, true), true);
            }
            
            return new Ability(
                new Move(from, to, false),
                false,
                "Blocked by friendly piece");
        }

        // Characteristic definitions
        private IPieceCharacteristic GetPawnForwardCharacteristic(ChessColor color)
        {
            int direction = color == ChessColor.White ? -1 : 1;
            return new PieceCharacteristic(
                "PawnForward",
                new[] { (direction, 0) },
                maxRange: 1);
        }

        private IPieceCharacteristic GetPawnDoubleForwardCharacteristic(ChessColor color)
        {
            int direction = color == ChessColor.White ? -1 : 1;
            return new PieceCharacteristic(
                "PawnDoubleForward",
                new[] { (direction * 2, 0) },
                maxRange: 1);
        }

        private IPieceCharacteristic GetPawnCaptureCharacteristic(ChessColor color)
        {
            int direction = color == ChessColor.White ? -1 : 1;
            return new PieceCharacteristic(
                "PawnCapture",
                new[] { (direction, -1), (direction, 1) },
                maxRange: 1);
        }

        private IPieceCharacteristic GetKnightCharacteristic()
        {
            return new PieceCharacteristic(
                "LShape",
                new[]
                {
                    (-2, -1), (-2, 1), (-1, -2), (-1, 2),
                    (1, -2), (1, 2), (2, -1), (2, 1)
                },
                maxRange: 1);
        }

        private IPieceCharacteristic GetBishopCharacteristic()
        {
            return new PieceCharacteristic(
                "Diagonal",
                new[] { (-1, -1), (-1, 1), (1, -1), (1, 1) },
                maxRange: -1,
                requiresClearPath: true);
        }

        private IPieceCharacteristic GetRookCharacteristic()
        {
            return new PieceCharacteristic(
                "Orthogonal",
                new[] { (-1, 0), (1, 0), (0, -1), (0, 1) },
                maxRange: -1,
                requiresClearPath: true);
        }

        private IPieceCharacteristic GetQueenCharacteristic()
        {
            return new PieceCharacteristic(
                "OmnidirectionalSlide",
                new[]
                {
                    (-1, -1), (-1, 0), (-1, 1),
                    (0, -1), (0, 1),
                    (1, -1), (1, 0), (1, 1)
                },
                maxRange: -1,
                requiresClearPath: true);
        }

        private IPieceCharacteristic GetKingCharacteristic()
        {
            return new PieceCharacteristic(
                "Adjacent",
                new[]
                {
                    (-1, -1), (-1, 0), (-1, 1),
                    (0, -1), (0, 1),
                    (1, -1), (1, 0), (1, 1)
                },
                maxRange: 1);
        }
    }
}
