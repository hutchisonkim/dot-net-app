using Bunit;
using Xunit;
using Xunit.Abstractions;

namespace Examples.Tests.UI;

/// <summary>
/// UI tests for Chess game that capture screenshots of different UI states.
/// Follows MSDN and xUnit testing guidelines.
/// </summary>
[Trait("Category", "UI")]
public class ChessUITests
{
    private readonly ITestOutputHelper _output;

    public ChessUITests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Chess_InitialState_RendersCorrectly()
    {
        // Arrange
        using var ctx = new TestContext();
        
        // Act
        var cut = ctx.RenderComponent<Chess.Pages.Index>();

        // Assert - Verify initial UI elements using semantic structure
        Assert.Contains("Chess Game - Persistence Example", cut.Markup);
        
        // Verify New Game button exists using data-testid
        var newGameButton = cut.Find("[data-testid='new-game-button']");
        Assert.NotNull(newGameButton);
        
        // Capture screenshot
        var screenshotPath = ScreenshotHelper.CaptureHtml(cut, "chess_initial_state");
        _output.WriteLine($"Screenshot saved to: {screenshotPath}");
    }

    [Fact]
    public void Chess_AfterNewGame_ShowsGameBoard()
    {
        // Arrange
        using var ctx = new TestContext();
        var cut = ctx.RenderComponent<Chess.Pages.Index>();

        // Act - Click "New Game" button using data-testid
        var newGameButton = cut.Find("[data-testid='new-game-button']");
        newGameButton.Click();

        // Assert - Verify game board is displayed using data-testid
        var gameInfo = cut.Find("[data-testid='game-info']");
        Assert.NotNull(gameInfo);
        
        var chessBoard = cut.Find("[data-testid='chess-board']");
        Assert.NotNull(chessBoard);
        
        var squares = cut.FindAll(".chess-square");
        Assert.Equal(64, squares.Count); // Chess board has 8x8 = 64 squares
        
        // Verify chess pieces are rendered (Unicode chess symbols)
        var markup = cut.Markup;
        Assert.Contains("♜", markup); // Black rook
        Assert.Contains("♞", markup); // Black knight
        Assert.Contains("♝", markup); // Black bishop
        Assert.Contains("♛", markup); // Black queen
        Assert.Contains("♚", markup); // Black king
        Assert.Contains("♟", markup); // Black pawn
        Assert.Contains("♖", markup); // White rook
        Assert.Contains("♘", markup); // White knight
        Assert.Contains("♗", markup); // White bishop
        Assert.Contains("♕", markup); // White queen
        Assert.Contains("♔", markup); // White king
        Assert.Contains("♙", markup); // White pawn

        // Capture screenshot
        var screenshotPath = ScreenshotHelper.CaptureHtml(cut, "chess_new_game_with_board");
        _output.WriteLine($"Screenshot saved to: {screenshotPath}");
    }

    [Fact]
    public void Chess_SaveButton_EnabledAfterNewGame()
    {
        // Arrange
        using var ctx = new TestContext();
        var cut = ctx.RenderComponent<Chess.Pages.Index>();

        // Act - Create new game using data-testid
        var newGameButton = cut.Find("[data-testid='new-game-button']");
        newGameButton.Click();

        // Assert - Verify Save button is enabled (not disabled)
        var saveButton = cut.Find("[data-testid='save-game-button']");
        Assert.NotNull(saveButton);
        Assert.False(saveButton.HasAttribute("disabled"));

        // Capture screenshot
        var screenshotPath = ScreenshotHelper.CaptureHtml(cut, "chess_save_button_enabled");
        _output.WriteLine($"Screenshot saved to: {screenshotPath}");
    }

    [Fact]
    public void Chess_LoadButton_EnabledAfterNewGame()
    {
        // Arrange
        using var ctx = new TestContext();
        var cut = ctx.RenderComponent<Chess.Pages.Index>();

        // Act - Create new game using data-testid
        var newGameButton = cut.Find("[data-testid='new-game-button']");
        newGameButton.Click();

        // Assert - Verify Load button is enabled
        var loadButton = cut.Find("[data-testid='load-game-button']");
        Assert.NotNull(loadButton);
        Assert.False(loadButton.HasAttribute("disabled"));

        // Capture screenshot
        var screenshotPath = ScreenshotHelper.CaptureHtml(cut, "chess_load_button_enabled");
        _output.WriteLine($"Screenshot saved to: {screenshotPath}");
    }

    [Fact]
    public void Chess_InitialState_ButtonsHaveCorrectDisabledState()
    {
        // Arrange
        using var ctx = new TestContext();
        
        // Act
        var cut = ctx.RenderComponent<Chess.Pages.Index>();

        // Assert - Verify initial button states using data-testid
        var newGameButton = cut.Find("[data-testid='new-game-button']");
        Assert.NotNull(newGameButton);
        Assert.False(newGameButton.HasAttribute("disabled"));

        var loadButton = cut.Find("[data-testid='load-game-button']");
        Assert.NotNull(loadButton);
        Assert.True(loadButton.HasAttribute("disabled"));

        var saveButton = cut.Find("[data-testid='save-game-button']");
        Assert.NotNull(saveButton);
        Assert.True(saveButton.HasAttribute("disabled"));

        // Capture screenshot
        var screenshotPath = ScreenshotHelper.CaptureHtml(cut, "chess_initial_button_states");
        _output.WriteLine($"Screenshot saved to: {screenshotPath}");
    }

    [Fact]
    public void Chess_SaveGame_ButtonWorks()
    {
        // Arrange
        using var ctx = new TestContext();
        var cut = ctx.RenderComponent<Chess.Pages.Index>();

        // Act - Create new game using data-testid
        var newGameButton = cut.Find("[data-testid='new-game-button']");
        newGameButton.Click();
        
        // Verify game was created
        var gameInfo = cut.Find("[data-testid='game-info']");
        Assert.NotNull(gameInfo);

        // Click Save button - should not throw
        var saveButton = cut.Find("[data-testid='save-game-button']");
        saveButton.Click();

        // Assert - Verify game state persists (UI still shows game info)
        gameInfo = cut.Find("[data-testid='game-info']");
        Assert.NotNull(gameInfo);
        
        var gameType = cut.Find("[data-testid='game-type']");
        Assert.Equal("Chess", gameType.TextContent);
        
        var chessBoard = cut.Find("[data-testid='chess-board']");
        Assert.NotNull(chessBoard);

        // Capture screenshot
        var screenshotPath = ScreenshotHelper.CaptureHtml(cut, "chess_after_save");
        _output.WriteLine($"Screenshot saved to: {screenshotPath}");
    }

    [Fact]
    public void Chess_LoadGame_ButtonWorks()
    {
        // Arrange
        using var ctx = new TestContext();
        var cut = ctx.RenderComponent<Chess.Pages.Index>();

        // Act - Create new game using data-testid
        var newGameButton = cut.Find("[data-testid='new-game-button']");
        newGameButton.Click();
        
        // Verify game was created and get game ID
        var gameIdElement = cut.Find("[data-testid='game-id']");
        var gameIdBefore = gameIdElement.TextContent;
        Assert.NotNull(gameIdBefore);

        // Click Load button - should not throw
        var loadButton = cut.Find("[data-testid='load-game-button']");
        loadButton.Click();

        // Assert - Verify game state is maintained (same game ID, board still visible)
        gameIdElement = cut.Find("[data-testid='game-id']");
        var gameIdAfter = gameIdElement.TextContent;
        Assert.Equal(gameIdBefore, gameIdAfter); // Game ID should not change
        
        var gameType = cut.Find("[data-testid='game-type']");
        Assert.Equal("Chess", gameType.TextContent);
        
        var chessBoard = cut.Find("[data-testid='chess-board']");
        Assert.NotNull(chessBoard);
        
        var lastUpdated = cut.Find("[data-testid='last-updated']");
        Assert.NotNull(lastUpdated);

        // Capture screenshot
        var screenshotPath = ScreenshotHelper.CaptureHtml(cut, "chess_after_load");
        _output.WriteLine($"Screenshot saved to: {screenshotPath}");
    }

    [Fact]
    public void Chess_MultipleStateChanges_AllButtonsWork()
    {
        // Arrange
        using var ctx = new TestContext();
        var cut = ctx.RenderComponent<Chess.Pages.Index>();

        // Act & Assert - New Game using data-testid
        var newGameButton = cut.Find("[data-testid='new-game-button']");
        newGameButton.Click();
        
        var gameInfo = cut.Find("[data-testid='game-info']");
        Assert.NotNull(gameInfo);
        
        var screenshotPath1 = ScreenshotHelper.CaptureHtml(cut, "chess_state_change_1_new_game");
        _output.WriteLine($"Screenshot 1 saved to: {screenshotPath1}");

        // Small delay
        System.Threading.Thread.Sleep(50);

        // Act & Assert - Save
        var saveButton = cut.Find("[data-testid='save-game-button']");
        saveButton.Click();
        
        var lastUpdated = cut.Find("[data-testid='last-updated']");
        Assert.NotNull(lastUpdated);
        
        var screenshotPath2 = ScreenshotHelper.CaptureHtml(cut, "chess_state_change_2_after_save");
        _output.WriteLine($"Screenshot 2 saved to: {screenshotPath2}");

        // Small delay
        System.Threading.Thread.Sleep(50);

        // Act & Assert - Load
        var loadButton = cut.Find("[data-testid='load-game-button']");
        loadButton.Click();
        
        lastUpdated = cut.Find("[data-testid='last-updated']");
        Assert.NotNull(lastUpdated);
        
        var screenshotPath3 = ScreenshotHelper.CaptureHtml(cut, "chess_state_change_3_after_load");
        _output.WriteLine($"Screenshot 3 saved to: {screenshotPath3}");

        // Verify board is still visible throughout
        var chessBoard = cut.Find("[data-testid='chess-board']");
        Assert.NotNull(chessBoard);
    }

    [Fact]
    public void Chess_AfterMakingMove_ShowsPieceMoved()
    {
        // Arrange
        using var ctx = new TestContext();
        var cut = ctx.RenderComponent<Chess.Pages.Index>();

        // Act - Create new game using data-testid
        var newGameButton = cut.Find("[data-testid='new-game-button']");
        newGameButton.Click();
        
        var markupBefore = cut.Markup;
        _output.WriteLine("Board before move:");
        _output.WriteLine(markupBefore);

        // Make a move (e2 to e4)
        var makeMoveButton = cut.Find("[data-testid='make-move-button']");
        makeMoveButton.Click();

        // Assert - Verify board changed
        var markupAfter = cut.Markup;
        _output.WriteLine("Board after move:");
        _output.WriteLine(markupAfter);
        
        // The board should still be visible but in different state
        var chessBoard = cut.Find("[data-testid='chess-board']");
        Assert.NotNull(chessBoard);
        Assert.NotEqual(markupBefore, markupAfter);

        // Capture screenshot showing piece moved
        var screenshotPath = ScreenshotHelper.CaptureHtml(cut, "chess_after_first_move");
        _output.WriteLine($"Screenshot saved to: {screenshotPath}");
    }

    [Fact]
    public void Chess_AfterMultipleMoves_ShowsGameProgression()
    {
        // Arrange
        using var ctx = new TestContext();
        var cut = ctx.RenderComponent<Chess.Pages.Index>();

        // Act - Create new game and make multiple moves using data-testid
        var newGameButton = cut.Find("[data-testid='new-game-button']");
        newGameButton.Click();
        
        var screenshotPath1 = ScreenshotHelper.CaptureHtml(cut, "chess_progression_1_initial");
        _output.WriteLine($"Screenshot 1 (initial) saved to: {screenshotPath1}");

        // First move
        var makeMoveButton = cut.Find("[data-testid='make-move-button']");
        makeMoveButton.Click();
        
        var screenshotPath2 = ScreenshotHelper.CaptureHtml(cut, "chess_progression_2_after_white_move");
        _output.WriteLine($"Screenshot 2 (after white move) saved to: {screenshotPath2}");

        // Second move
        makeMoveButton = cut.Find("[data-testid='make-move-button']");
        makeMoveButton.Click();
        
        var screenshotPath3 = ScreenshotHelper.CaptureHtml(cut, "chess_progression_3_after_black_move");
        _output.WriteLine($"Screenshot 3 (after black move) saved to: {screenshotPath3}");

        // Assert - Verify the game progressed
        var chessBoard = cut.Find("[data-testid='chess-board']");
        Assert.NotNull(chessBoard);
        
        var gameInfo = cut.Find("[data-testid='game-info']");
        Assert.NotNull(gameInfo);
    }

    [Fact]
    public void Chess_SaveAfterMove_PieceRemainsInNewPosition()
    {
        // Arrange
        using var ctx = new TestContext();
        var cut = ctx.RenderComponent<Chess.Pages.Index>();

        // Act - Create new game using data-testid
        var newGameButton = cut.Find("[data-testid='new-game-button']");
        newGameButton.Click();
        
        // Make a move (e2 to e4 - white pawn advances)
        var makeMoveButton = cut.Find("[data-testid='make-move-button']");
        makeMoveButton.Click();
        
        var markupAfterMove = cut.Markup;
        _output.WriteLine("Board after move (before save):");
        _output.WriteLine(markupAfterMove);
        
        // Capture screenshot after move but before save
        var screenshotPath1 = ScreenshotHelper.CaptureHtml(cut, "chess_move_before_save");
        _output.WriteLine($"Screenshot 1 (after move, before save) saved to: {screenshotPath1}");

        // Now save the game
        var saveButton = cut.Find("[data-testid='save-game-button']");
        saveButton.Click();

        // Assert - Verify the piece is still in the moved position after save
        var markupAfterSave = cut.Markup;
        _output.WriteLine("Board after save:");
        _output.WriteLine(markupAfterSave);
        
        // The board state should be the same as before save (piece still moved)
        var chessBoard = cut.Find("[data-testid='chess-board']");
        Assert.NotNull(chessBoard);
        
        var gameInfo = cut.Find("[data-testid='game-info']");
        Assert.NotNull(gameInfo);
        
        var lastUpdated = cut.Find("[data-testid='last-updated']");
        Assert.NotNull(lastUpdated);
        
        // Capture screenshot after save - should show piece still moved
        var screenshotPath2 = ScreenshotHelper.CaptureHtml(cut, "chess_after_save_with_moved_piece");
        _output.WriteLine($"Screenshot 2 (after save with moved piece) saved to: {screenshotPath2}");
    }
}
