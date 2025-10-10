using Bunit;
using Xunit;
using Xunit.Abstractions;

namespace Examples.Tests.UI;

/// <summary>
/// UI tests for Pong game that capture screenshots of different UI states.
/// Follows MSDN and xUnit testing guidelines.
/// </summary>
[Trait("Category", "UI")]
public class PongUITests
{
    private readonly ITestOutputHelper _output;

    public PongUITests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Pong_InitialState_RendersCorrectly()
    {
        // Arrange
        using var ctx = new TestContext();
        
        // Act
        var cut = ctx.RenderComponent<Pong.Pages.Index>();

        // Assert - Verify initial UI elements
        Assert.Contains("Pong Game - SignalR Real-time Example", cut.Markup);
        Assert.Contains("<button", cut.Markup);
        Assert.Contains("Connect", cut.Markup);
        Assert.Contains("Connection Status:", cut.Markup);
        Assert.Contains("Disconnected", cut.Markup);
        
        // Capture screenshot
        var screenshotPath = ScreenshotHelper.CaptureHtml(cut, "pong_initial_state");
        _output.WriteLine($"Screenshot saved to: {screenshotPath}");
    }

    [Fact]
    public void Pong_InitialState_ButtonsHaveCorrectDisabledState()
    {
        // Arrange
        using var ctx = new TestContext();
        
        // Act
        var cut = ctx.RenderComponent<Pong.Pages.Index>();

        // Assert - Verify initial button states
        var connectButton = cut.Find("button:contains('Connect')");
        Assert.NotNull(connectButton);
        Assert.False(connectButton.HasAttribute("disabled"));

        var joinButton = cut.Find("button:contains('Join Game')");
        Assert.NotNull(joinButton);
        Assert.True(joinButton.HasAttribute("disabled"));

        var leaveButton = cut.Find("button:contains('Leave Game')");
        Assert.NotNull(leaveButton);
        Assert.True(leaveButton.HasAttribute("disabled"));

        // Capture screenshot
        var screenshotPath = ScreenshotHelper.CaptureHtml(cut, "pong_initial_button_states");
        _output.WriteLine($"Screenshot saved to: {screenshotPath}");
    }

    [Fact]
    public void Pong_InitialState_ShowsGameCanvas()
    {
        // Arrange
        using var ctx = new TestContext();
        
        // Act
        var cut = ctx.RenderComponent<Pong.Pages.Index>();

        // Assert - Verify game canvas is rendered
        Assert.Contains("pong-canvas", cut.Markup);
        
        // Verify paddles and ball are rendered
        var markup = cut.Markup;
        Assert.Contains("position: absolute", markup); // Paddle/ball positioning
        Assert.Contains("background: white", markup); // Paddle/ball color
        
        // Capture screenshot
        var screenshotPath = ScreenshotHelper.CaptureHtml(cut, "pong_initial_canvas");
        _output.WriteLine($"Screenshot saved to: {screenshotPath}");
    }

    [Fact]
    public void Pong_InitialState_ShowsEventsLog()
    {
        // Arrange
        using var ctx = new TestContext();
        
        // Act
        var cut = ctx.RenderComponent<Pong.Pages.Index>();

        // Assert - Verify events log section exists
        Assert.Contains("Events Log:", cut.Markup);
        
        // Capture screenshot
        var screenshotPath = ScreenshotHelper.CaptureHtml(cut, "pong_initial_events_log");
        _output.WriteLine($"Screenshot saved to: {screenshotPath}");
    }

    [Fact]
    public void Pong_InitialState_HasGameIdInput()
    {
        // Arrange
        using var ctx = new TestContext();
        
        // Act
        var cut = ctx.RenderComponent<Pong.Pages.Index>();

        // Assert - Verify Game ID input exists
        Assert.Contains("Game ID:", cut.Markup);
        Assert.Contains("<input", cut.Markup);
        Assert.Contains("pong-room-1", cut.Markup); // Default game ID
        
        // Capture screenshot
        var screenshotPath = ScreenshotHelper.CaptureHtml(cut, "pong_initial_game_id_input");
        _output.WriteLine($"Screenshot saved to: {screenshotPath}");
    }

    [Fact]
    public void Pong_GameContainer_HasCorrectLayout()
    {
        // Arrange
        using var ctx = new TestContext();
        
        // Act
        var cut = ctx.RenderComponent<Pong.Pages.Index>();

        // Assert - Verify layout structure
        Assert.Contains("game-container", cut.Markup);
        
        // Capture screenshot
        var screenshotPath = ScreenshotHelper.CaptureHtml(cut, "pong_layout_structure");
        _output.WriteLine($"Screenshot saved to: {screenshotPath}");
    }
}
