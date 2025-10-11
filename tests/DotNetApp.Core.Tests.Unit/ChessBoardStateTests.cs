using DotNetApp.Core.Models;
using Xunit;

namespace DotNetApp.Core.Tests.Unit;

/// <summary>
/// Unit tests for ChessBoardState model.
/// Follows MSDN and xUnit testing guidelines.
/// </summary>
[Trait("Category", "Unit")]
public class ChessBoardStateTests
{
    [Fact]
    public void ChessBoardState_IndexerSet_StoresPiece()
    {
        // Arrange
        var board = new ChessBoardState();
        var piece = new ChessPiece(ChessPieceType.Pawn, ChessColor.White);

        // Act
        board[3, 4] = piece;

        // Assert
        Assert.Equal(piece, board[3, 4]);
    }

    [Fact]
    public void ChessBoardState_IndexerGet_ReturnsNullForEmptySquare()
    {
        // Arrange
        var board = new ChessBoardState();

        // Act
        var piece = board[3, 4];

        // Assert
        Assert.Null(piece);
    }

    [Fact]
    public void ChessBoardState_IndexerSet_RemovesPieceWhenNull()
    {
        // Arrange
        var board = new ChessBoardState();
        var piece = new ChessPiece(ChessPieceType.Pawn, ChessColor.White);
        board[3, 4] = piece;

        // Act
        board[3, 4] = null;

        // Assert
        Assert.Null(board[3, 4]);
    }

    [Fact]
    public void ChessBoardState_GetPiece_ReturnsPieceAtPosition()
    {
        // Arrange
        var board = new ChessBoardState();
        var piece = new ChessPiece(ChessPieceType.Knight, ChessColor.Black);
        var position = new Position(2, 5);
        board[2, 5] = piece;

        // Act
        var result = board.GetPiece(position);

        // Assert
        Assert.Equal(piece, result);
    }

    [Fact]
    public void ChessBoardState_IsInBounds_ReturnsTrueForValidPosition()
    {
        // Arrange
        var board = new ChessBoardState();

        // Act & Assert
        Assert.True(board.IsInBounds(new Position(0, 0)));
        Assert.True(board.IsInBounds(new Position(7, 7)));
        Assert.True(board.IsInBounds(new Position(3, 4)));
    }

    [Fact]
    public void ChessBoardState_IsInBounds_ReturnsFalseForInvalidPosition()
    {
        // Arrange
        var board = new ChessBoardState();

        // Act & Assert
        Assert.False(board.IsInBounds(new Position(-1, 0)));
        Assert.False(board.IsInBounds(new Position(0, -1)));
        Assert.False(board.IsInBounds(new Position(8, 0)));
        Assert.False(board.IsInBounds(new Position(0, 8)));
    }

    [Fact]
    public void ChessBoardState_IsOccupied_ReturnsTrueWhenPieceExists()
    {
        // Arrange
        var board = new ChessBoardState();
        var position = new Position(3, 4);
        board[3, 4] = new ChessPiece(ChessPieceType.Rook, ChessColor.White);

        // Act
        var result = board.IsOccupied(position);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ChessBoardState_IsOccupied_ReturnsFalseWhenEmpty()
    {
        // Arrange
        var board = new ChessBoardState();
        var position = new Position(3, 4);

        // Act
        var result = board.IsOccupied(position);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ChessBoardState_GetAllPieces_ReturnsAllPieces()
    {
        // Arrange
        var board = new ChessBoardState();
        var piece1 = new ChessPiece(ChessPieceType.Pawn, ChessColor.White);
        var piece2 = new ChessPiece(ChessPieceType.Knight, ChessColor.Black);
        board[0, 0] = piece1;
        board[7, 7] = piece2;

        // Act
        var pieces = board.GetAllPieces().ToList();

        // Assert
        Assert.Equal(2, pieces.Count);
        Assert.Contains(pieces, p => p.piece == piece1 && p.position.Row == 0 && p.position.Column == 0);
        Assert.Contains(pieces, p => p.piece == piece2 && p.position.Row == 7 && p.position.Column == 7);
    }

    [Fact]
    public void ChessPiece_Constructor_SetsProperties()
    {
        // Arrange & Act
        var piece = new ChessPiece(ChessPieceType.Queen, ChessColor.Black, true);

        // Assert
        Assert.Equal(ChessPieceType.Queen, piece.Type);
        Assert.Equal(ChessColor.Black, piece.Color);
        Assert.True(piece.HasMoved);
    }

    [Fact]
    public void ChessPiece_Constructor_HasMovedDefaultsFalse()
    {
        // Arrange & Act
        var piece = new ChessPiece(ChessPieceType.King, ChessColor.White);

        // Assert
        Assert.False(piece.HasMoved);
    }
}
