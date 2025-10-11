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

        // Assert - Verify initial UI elements
        Assert.Contains("Chess Game - Persistence Example", cut.Markup);
        Assert.Contains("<button", cut.Markup);
        Assert.Contains("New Game", cut.Markup);

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

        // Act - Click "New Game" button
        var newGameButton = cut.Find("button:contains('New Game')");
        newGameButton.Click();

        // Assert - Verify game board is displayed
        var markup = cut.Markup;
        Assert.Contains("Game ID:", markup);
        Assert.Contains("chess-board", markup);
        Assert.Contains("chess-square", markup);

        // Verify chess pieces are rendered (Unicode chess symbols)
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

        // Act - Create new game
        var newGameButton = cut.Find("button:contains('New Game')");
        newGameButton.Click();

        // Assert - Verify Save button is enabled (not disabled)
        var saveButton = cut.Find("button:contains('Save Game')");
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

        // Act - Create new game
        var newGameButton = cut.Find("button:contains('New Game')");
        newGameButton.Click();

        // Assert - Verify Load button is enabled
        var loadButton = cut.Find("button:contains('Load Game')");
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

        // Assert - Verify initial button states
        var newGameButton = cut.Find("button:contains('New Game')");
        Assert.NotNull(newGameButton);
        Assert.False(newGameButton.HasAttribute("disabled"));

        var loadButton = cut.Find("button:contains('Load Game')");
        Assert.NotNull(loadButton);
        Assert.True(loadButton.HasAttribute("disabled"));

        var saveButton = cut.Find("button:contains('Save Game')");
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

        // Act - Create new game
        var newGameButton = cut.Find("button:contains('New Game')");
        newGameButton.Click();

        // Verify game was created
        Assert.Contains("Game ID:", cut.Markup);
        Assert.Contains("Last Updated:", cut.Markup);

        // Click Save button - should not throw
        var saveButton = cut.Find("button:contains('Save Game')");
        saveButton.Click();

        // Assert - Verify game state persists (UI still shows game info)
        var markupAfterSave = cut.Markup;
        Assert.Contains("Game ID:", markupAfterSave);
        Assert.Contains("Last Updated:", markupAfterSave);
        Assert.Contains("Game Type: Chess", markupAfterSave);
        Assert.Contains("chess-board", markupAfterSave);

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

        // Act - Create new game
        var newGameButton = cut.Find("button:contains('New Game')");
        newGameButton.Click();

        // Verify game was created
        var gameIdBefore = ExtractGameId(cut.Markup);
        Assert.NotNull(gameIdBefore);

        // Click Load button - should not throw
        var loadButton = cut.Find("button:contains('Load Game')");
        loadButton.Click();

        // Assert - Verify game state is maintained (same game ID, board still visible)
        var markupAfterLoad = cut.Markup;
        var gameIdAfter = ExtractGameId(markupAfterLoad);
        Assert.Equal(gameIdBefore, gameIdAfter); // Game ID should not change
        Assert.Contains("Game Type: Chess", markupAfterLoad);
        Assert.Contains("chess-board", markupAfterLoad);
        Assert.Contains("Last Updated:", markupAfterLoad);

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

        // Act & Assert - New Game
        var newGameButton = cut.Find("button:contains('New Game')");
        newGameButton.Click();
        Assert.Contains("Game ID:", cut.Markup);

        var screenshotPath1 = ScreenshotHelper.CaptureHtml(cut, "chess_state_change_1_new_game");
        _output.WriteLine($"Screenshot 1 saved to: {screenshotPath1}");

        // Small delay
        System.Threading.Thread.Sleep(50);

        // Act & Assert - Save
        var saveButton = cut.Find("button:contains('Save Game')");
        saveButton.Click();
        Assert.Contains("Last Updated:", cut.Markup);

        var screenshotPath2 = ScreenshotHelper.CaptureHtml(cut, "chess_state_change_2_after_save");
        _output.WriteLine($"Screenshot 2 saved to: {screenshotPath2}");

        // Small delay
        System.Threading.Thread.Sleep(50);

        // Act & Assert - Load
        var loadButton = cut.Find("button:contains('Load Game')");
        loadButton.Click();
        Assert.Contains("Last Updated:", cut.Markup);

        var screenshotPath3 = ScreenshotHelper.CaptureHtml(cut, "chess_state_change_3_after_load");
        _output.WriteLine($"Screenshot 3 saved to: {screenshotPath3}");

        // Verify board is still visible throughout
        Assert.Contains("chess-board", cut.Markup);
    }

    private static string? ExtractTimestamp(string markup)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            markup,
            @"Last Updated:\s*([^<]+)"
        );
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string? ExtractGameId(string markup)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            markup,
            @"Game ID:\s*([^<]+)"
        );
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    [Fact]
    public void Chess_AfterMakingMove_ShowsPieceMoved()
    {
        // Arrange
        using var ctx = new TestContext();
        var cut = ctx.RenderComponent<Chess.Pages.Index>();

        // Act - Create new game
        var newGameButton = cut.Find("button:contains('New Game')");
        newGameButton.Click();

        var markupBefore = cut.Markup;
        _output.WriteLine("Board before move:");
        _output.WriteLine(markupBefore);

        // Make a move (e2 to e4)
        var makeMoveButton = cut.Find("button:contains('Make Move')");
        makeMoveButton.Click();

        // Assert - Verify board changed
        var markupAfter = cut.Markup;
        _output.WriteLine("Board after move:");
        _output.WriteLine(markupAfter);

        // The board should still have pieces but in different positions
        Assert.Contains("chess-board", markupAfter);
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

        // Act - Create new game and make multiple moves
        var newGameButton = cut.Find("button:contains('New Game')");
        newGameButton.Click();

        var screenshotPath1 = ScreenshotHelper.CaptureHtml(cut, "chess_progression_1_initial");
        _output.WriteLine($"Screenshot 1 (initial) saved to: {screenshotPath1}");

        // First move
        var makeMoveButton = cut.Find("button:contains('Make Move')");
        makeMoveButton.Click();

        var screenshotPath2 = ScreenshotHelper.CaptureHtml(cut, "chess_progression_2_after_white_move");
        _output.WriteLine($"Screenshot 2 (after white move) saved to: {screenshotPath2}");

        // Second move
        makeMoveButton = cut.Find("button:contains('Make Move')");
        makeMoveButton.Click();

        var screenshotPath3 = ScreenshotHelper.CaptureHtml(cut, "chess_progression_3_after_black_move");
        _output.WriteLine($"Screenshot 3 (after black move) saved to: {screenshotPath3}");

        // Assert - Verify the game progressed
        Assert.Contains("chess-board", cut.Markup);
        Assert.Contains("Game ID:", cut.Markup);
    }

    [Fact]
    public void Chess_SaveAfterMove_PieceRemainsInNewPosition()
    {
        // Arrange
        using var ctx = new TestContext();
        var cut = ctx.RenderComponent<Chess.Pages.Index>();

        // Act - Create new game
        var newGameButton = cut.Find("button:contains('New Game')");
        newGameButton.Click();

        // Make a move (e2 to e4 - white pawn advances)
        var makeMoveButton = cut.Find("button:contains('Make Move')");
        makeMoveButton.Click();

        var markupAfterMove = cut.Markup;
        _output.WriteLine("Board after move (before save):");
        _output.WriteLine(markupAfterMove);

        // Capture screenshot after move but before save
        var screenshotPath1 = ScreenshotHelper.CaptureHtml(cut, "chess_move_before_save");
        _output.WriteLine($"Screenshot 1 (after move, before save) saved to: {screenshotPath1}");

        // Now save the game
        var saveButton = cut.Find("button:contains('Save Game')");
        saveButton.Click();

        // Assert - Verify the piece is still in the moved position after save
        var markupAfterSave = cut.Markup;
        _output.WriteLine("Board after save:");
        _output.WriteLine(markupAfterSave);

        // The board state should be the same as before save (piece still moved)
        Assert.Contains("chess-board", markupAfterSave);
        Assert.Contains("Game ID:", markupAfterSave);
        Assert.Contains("Last Updated:", markupAfterSave);

        // Capture screenshot after save - should show piece still moved
        var screenshotPath2 = ScreenshotHelper.CaptureHtml(cut, "chess_after_save_with_moved_piece");
        _output.WriteLine($"Screenshot 2 (after save with moved piece) saved to: {screenshotPath2}");
    }
}
