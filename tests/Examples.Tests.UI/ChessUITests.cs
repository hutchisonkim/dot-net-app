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
}
