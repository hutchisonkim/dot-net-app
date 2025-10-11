using DotNetApp.Core.Models;
using DotNetApp.Core.Services;
using Xunit;

namespace DotNetApp.Core.Tests.Unit;

/// <summary>
/// Unit tests for ChessPolicy engine.
/// Tests characteristic-based ability system for chess pieces.
/// Follows MSDN and xUnit testing guidelines.
/// </summary>
[Trait("Category", "Unit")]
public class ChessPolicyTests
{
    private ChessBoardState CreateInitialBoardState()
    {
        var board = new ChessBoardState();
        
        // Black pieces (row 0 and 1)
        board[0, 0] = new ChessPiece(ChessPieceType.Rook, ChessColor.Black);
        board[0, 1] = new ChessPiece(ChessPieceType.Knight, ChessColor.Black);
        board[0, 2] = new ChessPiece(ChessPieceType.Bishop, ChessColor.Black);
        board[0, 3] = new ChessPiece(ChessPieceType.Queen, ChessColor.Black);
        board[0, 4] = new ChessPiece(ChessPieceType.King, ChessColor.Black);
        board[0, 5] = new ChessPiece(ChessPieceType.Bishop, ChessColor.Black);
        board[0, 6] = new ChessPiece(ChessPieceType.Knight, ChessColor.Black);
        board[0, 7] = new ChessPiece(ChessPieceType.Rook, ChessColor.Black);
        
        for (int col = 0; col < 8; col++)
        {
            board[1, col] = new ChessPiece(ChessPieceType.Pawn, ChessColor.Black);
        }
        
        // White pieces (row 6 and 7)
        for (int col = 0; col < 8; col++)
        {
            board[6, col] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White);
        }
        
        board[7, 0] = new ChessPiece(ChessPieceType.Rook, ChessColor.White);
        board[7, 1] = new ChessPiece(ChessPieceType.Knight, ChessColor.White);
        board[7, 2] = new ChessPiece(ChessPieceType.Bishop, ChessColor.White);
        board[7, 3] = new ChessPiece(ChessPieceType.Queen, ChessColor.White);
        board[7, 4] = new ChessPiece(ChessPieceType.King, ChessColor.White);
        board[7, 5] = new ChessPiece(ChessPieceType.Bishop, ChessColor.White);
        board[7, 6] = new ChessPiece(ChessPieceType.Knight, ChessColor.White);
        board[7, 7] = new ChessPiece(ChessPieceType.Rook, ChessColor.White);
        
        return board;
    }

    [Fact]
    public void GetCharacteristics_Pawn_ReturnsForwardAndCapture()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(6, 4);
        board[6, 4] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White);

        // Act
        var characteristics = policy.GetCharacteristics(position, board).ToList();

        // Assert
        Assert.NotEmpty(characteristics);
        Assert.Contains(characteristics, c => c.Name == "PawnForward");
        Assert.Contains(characteristics, c => c.Name == "PawnCapture");
        Assert.Contains(characteristics, c => c.Name == "PawnDoubleForward");
    }

    [Fact]
    public void GetCharacteristics_MovedPawn_DoesNotIncludeDoubleForward()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(5, 4);
        board[5, 4] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White, hasMoved: true);

        // Act
        var characteristics = policy.GetCharacteristics(position, board).ToList();

        // Assert
        Assert.DoesNotContain(characteristics, c => c.Name == "PawnDoubleForward");
    }

    [Fact]
    public void GetCharacteristics_Knight_ReturnsLShapeCharacteristic()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(7, 1);
        board[7, 1] = new ChessPiece(ChessPieceType.Knight, ChessColor.White);

        // Act
        var characteristics = policy.GetCharacteristics(position, board).ToList();

        // Assert
        Assert.Single(characteristics);
        Assert.Equal("LShape", characteristics[0].Name);
    }

    [Fact]
    public void GetAbilities_InitialWhitePawn_CanMoveOneOrTwoSquares()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = CreateInitialBoardState();
        var position = new Position(6, 4); // White pawn at e2

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();
        var allowedAbilities = abilities.Where(a => a.IsAllowed).ToList();

        // Assert
        Assert.NotEmpty(allowedAbilities);
        Assert.Contains(allowedAbilities, a => 
            a.Move.To.Row == 5 && a.Move.To.Column == 4 && !a.Move.IsCapture);
        Assert.Contains(allowedAbilities, a => 
            a.Move.To.Row == 4 && a.Move.To.Column == 4 && !a.Move.IsCapture);
    }

    [Fact]
    public void GetAbilities_InitialWhiteKnight_HasTwoMoves()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = CreateInitialBoardState();
        var position = new Position(7, 1); // White knight at b1

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();
        var allowedAbilities = abilities.Where(a => a.IsAllowed).ToList();

        // Assert
        Assert.Equal(2, allowedAbilities.Count);
        // Knight can jump to a3 (row 5, col 0) or c3 (row 5, col 2)
        Assert.Contains(allowedAbilities, a => 
            a.Move.To.Row == 5 && a.Move.To.Column == 0);
        Assert.Contains(allowedAbilities, a => 
            a.Move.To.Row == 5 && a.Move.To.Column == 2);
    }

    [Fact]
    public void GetAbilities_InitialBlackPawn_CanMoveOneOrTwoSquares()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = CreateInitialBoardState();
        var position = new Position(1, 4); // Black pawn at e7

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();
        var allowedAbilities = abilities.Where(a => a.IsAllowed).ToList();

        // Assert
        Assert.NotEmpty(allowedAbilities);
        Assert.Contains(allowedAbilities, a => 
            a.Move.To.Row == 2 && a.Move.To.Column == 4 && !a.Move.IsCapture);
        Assert.Contains(allowedAbilities, a => 
            a.Move.To.Row == 3 && a.Move.To.Column == 4 && !a.Move.IsCapture);
    }

    [Fact]
    public void GetAbilities_InitialBlackKnight_HasTwoMoves()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = CreateInitialBoardState();
        var position = new Position(0, 1); // Black knight at b8

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();
        var allowedAbilities = abilities.Where(a => a.IsAllowed).ToList();

        // Assert
        Assert.Equal(2, allowedAbilities.Count);
        // Knight can jump to a6 (row 2, col 0) or c6 (row 2, col 2)
        Assert.Contains(allowedAbilities, a => 
            a.Move.To.Row == 2 && a.Move.To.Column == 0);
        Assert.Contains(allowedAbilities, a => 
            a.Move.To.Row == 2 && a.Move.To.Column == 2);
    }

    [Fact]
    public void GetAbilities_PawnCapture_ForbiddenWhenNoPieceToCapture()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(4, 4);
        board[4, 4] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White, hasMoved: true);

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();
        var captureAbilities = abilities.Where(a => a.Move.IsCapture).ToList();

        // Assert
        Assert.NotEmpty(captureAbilities);
        Assert.All(captureAbilities, a => Assert.False(a.IsAllowed));
        Assert.All(captureAbilities, a => Assert.Equal("No piece to capture", a.ForbiddenReason));
    }

    [Fact]
    public void GetAbilities_PawnCapture_AllowedWhenEnemyPiecePresent()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(4, 4);
        board[4, 4] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White, hasMoved: true);
        board[3, 3] = new ChessPiece(ChessPieceType.Pawn, ChessColor.Black);
        board[3, 5] = new ChessPiece(ChessPieceType.Pawn, ChessColor.Black);

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();
        var allowedCaptures = abilities.Where(a => a.IsAllowed && a.Move.IsCapture).ToList();

        // Assert
        Assert.Equal(2, allowedCaptures.Count);
        Assert.Contains(allowedCaptures, a => 
            a.Move.To.Row == 3 && a.Move.To.Column == 3);
        Assert.Contains(allowedCaptures, a => 
            a.Move.To.Row == 3 && a.Move.To.Column == 5);
    }

    [Fact]
    public void GetAbilities_PawnCapture_ForbiddenWhenFriendlyPiecePresent()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(4, 4);
        board[4, 4] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White, hasMoved: true);
        board[3, 3] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White);

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();
        var captureLeft = abilities.FirstOrDefault(a => 
            a.Move.IsCapture && a.Move.To.Row == 3 && a.Move.To.Column == 3);

        // Assert
        Assert.NotNull(captureLeft);
        Assert.False(captureLeft.IsAllowed);
        Assert.Equal("Cannot capture friendly piece", captureLeft.ForbiddenReason);
    }

    [Fact]
    public void GetAbilities_PawnForward_ForbiddenWhenBlocked()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(6, 4);
        board[6, 4] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White);
        board[5, 4] = new ChessPiece(ChessPieceType.Pawn, ChessColor.Black);

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();
        var forwardMoves = abilities.Where(a => !a.Move.IsCapture).ToList();

        // Assert
        Assert.All(forwardMoves, a => Assert.False(a.IsAllowed));
    }

    [Fact]
    public void GetAbilities_PawnDoubleForward_ForbiddenWhenPathBlocked()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(6, 4);
        board[6, 4] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White);
        board[5, 4] = new ChessPiece(ChessPieceType.Pawn, ChessColor.Black);

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();
        var doubleForward = abilities.FirstOrDefault(a => 
            !a.Move.IsCapture && a.Move.To.Row == 4);

        // Assert
        Assert.NotNull(doubleForward);
        Assert.False(doubleForward.IsAllowed);
        Assert.Equal("Path is blocked", doubleForward.ForbiddenReason);
    }

    [Fact]
    public void GetAbilities_Rook_CanSlideUntilBlocked()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(4, 4);
        board[4, 4] = new ChessPiece(ChessPieceType.Rook, ChessColor.White);
        board[4, 7] = new ChessPiece(ChessPieceType.Pawn, ChessColor.Black);

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();
        var rightMoves = abilities.Where(a => 
            a.Move.To.Row == 4 && a.Move.To.Column > 4 && a.IsAllowed).ToList();

        // Assert
        Assert.Equal(3, rightMoves.Count); // Can move to columns 5, 6, and 7 (capture)
        Assert.Contains(rightMoves, a => a.Move.To.Column == 5 && !a.Move.IsCapture);
        Assert.Contains(rightMoves, a => a.Move.To.Column == 6 && !a.Move.IsCapture);
        Assert.Contains(rightMoves, a => a.Move.To.Column == 7 && a.Move.IsCapture);
    }

    [Fact]
    public void GetAbilities_Bishop_CanSlideDiagonally()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(4, 4);
        board[4, 4] = new ChessPiece(ChessPieceType.Bishop, ChessColor.White);

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();
        var allowedAbilities = abilities.Where(a => a.IsAllowed).ToList();

        // Assert
        Assert.NotEmpty(allowedAbilities);
        // Bishop on empty board should have moves in all 4 diagonal directions
        Assert.Contains(allowedAbilities, a => a.Move.To.Row == 3 && a.Move.To.Column == 3); // up-left
        Assert.Contains(allowedAbilities, a => a.Move.To.Row == 3 && a.Move.To.Column == 5); // up-right
        Assert.Contains(allowedAbilities, a => a.Move.To.Row == 5 && a.Move.To.Column == 3); // down-left
        Assert.Contains(allowedAbilities, a => a.Move.To.Row == 5 && a.Move.To.Column == 5); // down-right
    }

    [Fact]
    public void GetAbilities_Knight_CanJumpOverPieces()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(4, 4);
        board[4, 4] = new ChessPiece(ChessPieceType.Knight, ChessColor.White);
        // Surround knight with pieces
        board[3, 4] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White);
        board[5, 4] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White);
        board[4, 3] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White);
        board[4, 5] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White);

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();
        var allowedAbilities = abilities.Where(a => a.IsAllowed).ToList();

        // Assert
        Assert.Equal(8, allowedAbilities.Count); // All 8 L-shape moves are available
    }

    [Fact]
    public void GetAbilities_Queen_CombinesRookAndBishop()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(4, 4);
        board[4, 4] = new ChessPiece(ChessPieceType.Queen, ChessColor.White);

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();
        var allowedAbilities = abilities.Where(a => a.IsAllowed).ToList();

        // Assert
        Assert.NotEmpty(allowedAbilities);
        // Queen should be able to move in all 8 directions
        Assert.Contains(allowedAbilities, a => a.Move.To.Row == 3 && a.Move.To.Column == 4); // up
        Assert.Contains(allowedAbilities, a => a.Move.To.Row == 5 && a.Move.To.Column == 4); // down
        Assert.Contains(allowedAbilities, a => a.Move.To.Row == 4 && a.Move.To.Column == 3); // left
        Assert.Contains(allowedAbilities, a => a.Move.To.Row == 4 && a.Move.To.Column == 5); // right
        Assert.Contains(allowedAbilities, a => a.Move.To.Row == 3 && a.Move.To.Column == 3); // up-left
        Assert.Contains(allowedAbilities, a => a.Move.To.Row == 3 && a.Move.To.Column == 5); // up-right
        Assert.Contains(allowedAbilities, a => a.Move.To.Row == 5 && a.Move.To.Column == 3); // down-left
        Assert.Contains(allowedAbilities, a => a.Move.To.Row == 5 && a.Move.To.Column == 5); // down-right
    }

    [Fact]
    public void GetAbilities_King_CanMoveOneSquareInAnyDirection()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(4, 4);
        board[4, 4] = new ChessPiece(ChessPieceType.King, ChessColor.White);

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();
        var allowedAbilities = abilities.Where(a => a.IsAllowed).ToList();

        // Assert
        Assert.Equal(8, allowedAbilities.Count);
        // King should be able to move to all 8 adjacent squares
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                Assert.Contains(allowedAbilities, a => 
                    a.Move.To.Row == 4 + dr && a.Move.To.Column == 4 + dc);
            }
        }
    }

    [Fact]
    public void GetAbilities_EmptySquare_ReturnsNoAbilities()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(4, 4);

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();

        // Assert
        Assert.Empty(abilities);
    }

    [Fact]
    public void IsValidMove_ValidMove_ReturnsTrue()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(6, 4);
        board[6, 4] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White);
        var move = new Move(position, new Position(5, 4));

        // Act
        var result = policy.IsValidMove(move, board);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidMove_InvalidMove_ReturnsFalse()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(6, 4);
        board[6, 4] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White);
        var move = new Move(position, new Position(4, 5)); // Invalid diagonal without capture

        // Act
        var result = policy.IsValidMove(move, board);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetAbilities_EdgeOfBoard_DoesNotIncludeOutOfBoundsMoves()
    {
        // Arrange
        var policy = new ChessPolicy();
        var board = new ChessBoardState();
        var position = new Position(0, 0);
        board[0, 0] = new ChessPiece(ChessPieceType.King, ChessColor.White);

        // Act
        var abilities = policy.GetAbilities(position, board).ToList();

        // Assert
        Assert.All(abilities, a => 
        {
            Assert.True(a.Move.To.Row >= 0 && a.Move.To.Row < 8);
            Assert.True(a.Move.To.Column >= 0 && a.Move.To.Column < 8);
        });
    }
}
