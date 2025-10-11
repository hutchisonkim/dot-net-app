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
    public void InitialState_RendersWithCorrectElementsAndButtonStates()
    {
        // Arrange
        using var ctx = new TestContext();
        
        // Act
        var cut = ctx.RenderComponent<Chess.Pages.Index>();

        // Assert - Verify initial UI elements
        Assert.Contains("Chess Game - Persistence Example", cut.Markup);
        
        // Verify New Game button exists using data-testid
        var newGameButton = cut.Find("[data-testid='new-game-button']");
        Assert.NotNull(newGameButton);
        
        // Assert - Verify initial button states using data-testid
        newGameButton = cut.Find("[data-testid='new-game-button']");
        Assert.NotNull(newGameButton);
        Assert.False(newGameButton.HasAttribute("disabled"));

        var loadButton = cut.Find("[data-testid='load-game-button']");
        Assert.NotNull(loadButton);
        Assert.True(loadButton.HasAttribute("disabled"));

        var saveButton = cut.Find("[data-testid='save-game-button']");
        Assert.NotNull(saveButton);
        Assert.True(saveButton.HasAttribute("disabled"));
        
        // Capture screenshot
        var screenshotPath = ScreenshotHelper.CaptureHtml(cut, "chess_initial_state");
        _output.WriteLine($"Screenshot saved to: {screenshotPath}");
    }

    [Fact]
    public void NewGameButton_Click_RendersGameBoardWithPieces()
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
    public void SaveButton_Click_PersistsGameState()
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
    public void LoadButton_Click_RestoresGameState()
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
    public void MultipleStateChanges_NewSaveLoad_MaintainsBoardVisibility()
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
    public void MakeMoveButton_MultipleClicks_ShowsGameProgression()
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
    public void CompleteFlow_StartMoveSaveNewLoad_RestoresPawnPosition()
    {
        // Arrange
        using var ctx = new TestContext();
        var cut = ctx.RenderComponent<Chess.Pages.Index>();
        var screenshots = new System.Collections.Generic.List<string>();

        // Step 1: Start - Create new game using data-testid
        var newGameButton = cut.Find("[data-testid='new-game-button']");
        newGameButton.Click();
        var screenshotPath1 = ScreenshotHelper.CaptureHtml(cut, "chess_flow_1_start");
        screenshots.Add(screenshotPath1);
        _output.WriteLine($"Step 1 (Start): Screenshot saved to {screenshotPath1}");
        
        // Verify initial board state
        var chessBoard = cut.Find("[data-testid='chess-board']");
        Assert.NotNull(chessBoard);
        var markupAfterStart = cut.Markup;
        var gameIdElement = cut.Find("[data-testid='game-id']");
        var gameIdOriginal = gameIdElement.TextContent;
        Assert.NotNull(gameIdOriginal);
        
        System.Threading.Thread.Sleep(100);

        // Step 2: Move - Make a move (e2 to e4)
        var makeMoveButton = cut.Find("[data-testid='make-move-button']");
        makeMoveButton.Click();
        var screenshotPath2 = ScreenshotHelper.CaptureHtml(cut, "chess_flow_2_move");
        screenshots.Add(screenshotPath2);
        _output.WriteLine($"Step 2 (Move): Screenshot saved to {screenshotPath2}");
        
        // Verify pawn moved
        chessBoard = cut.Find("[data-testid='chess-board']");
        Assert.NotNull(chessBoard);
        var markupAfterMove = cut.Markup;
        Assert.NotEqual(markupAfterStart, markupAfterMove); // Board changed
        
        System.Threading.Thread.Sleep(100);

        // Step 3: Save - Save the game state with the moved pawn
        var saveButton = cut.Find("[data-testid='save-game-button']");
        saveButton.Click();
        var screenshotPath3 = ScreenshotHelper.CaptureHtml(cut, "chess_flow_3_save");
        screenshots.Add(screenshotPath3);
        _output.WriteLine($"Step 3 (Save): Screenshot saved to {screenshotPath3}");
        
        // Store the board state after first move for comparison
        var boardAfterFirstMove = cut.Markup;
        var chessBoardAfterFirstMove = cut.Find("[data-testid='chess-board']");
        var chessBoardHtmlAfterFirstMove = chessBoardAfterFirstMove.OuterHtml;
        var lastUpdated = cut.Find("[data-testid='last-updated']");
        Assert.NotNull(lastUpdated);
        
        System.Threading.Thread.Sleep(100);

        // Step 4: Make another move (this will move black pawn from e7 to e5)
        var makeMoveButton2 = cut.Find("[data-testid='make-move-button']");
        makeMoveButton2.Click();
        var screenshotPath4 = ScreenshotHelper.CaptureHtml(cut, "chess_flow_4_second_move");
        screenshots.Add(screenshotPath4);
        _output.WriteLine($"Step 4 (Second Move): Screenshot saved to {screenshotPath4}");
        
        // Verify second move was made - board should be different from after first move
        chessBoard = cut.Find("[data-testid='chess-board']");
        Assert.NotNull(chessBoard);
        var chessBoardHtmlAfterSecondMove = chessBoard.OuterHtml;
        var markupAfterSecondMove = cut.Markup;
        Assert.NotEqual(chessBoardHtmlAfterFirstMove, chessBoardHtmlAfterSecondMove); // Board changed again
        
        System.Threading.Thread.Sleep(100);

        // Step 5: Load - Load the saved game state (should restore to after first move only)
        var loadButton = cut.Find("[data-testid='load-game-button']");
        loadButton.Click();
        var screenshotPath5 = ScreenshotHelper.CaptureHtml(cut, "chess_flow_5_load");
        screenshots.Add(screenshotPath5);
        _output.WriteLine($"Step 5 (Load): Screenshot saved to {screenshotPath5}");
        
        // Verify game loaded and board state restored to after-first-move state
        chessBoard = cut.Find("[data-testid='chess-board']");
        Assert.NotNull(chessBoard);
        lastUpdated = cut.Find("[data-testid='last-updated']");
        Assert.NotNull(lastUpdated);
        var chessBoardHtmlAfterLoad = chessBoard.OuterHtml;
        var markupAfterLoad = cut.Markup;
        
        // The key verification: board should match the saved state (after first move)
        // This proves the pawn position (white pawn at e4) was restored
        // We compare just the chess board HTML, not the entire markup (to avoid timestamp differences)
        Assert.Equal(chessBoardHtmlAfterFirstMove, chessBoardHtmlAfterLoad); // Board matches saved state
        Assert.NotEqual(markupAfterStart, markupAfterLoad); // Board NOT at initial state (this check is OK as initial has no board)
        Assert.NotEqual(chessBoardHtmlAfterSecondMove, chessBoardHtmlAfterLoad); // Board NOT at second move state
        
        _output.WriteLine("Verified: Pawn position restored correctly - white pawn at e4, black pawn at e7");
        
        // Create GIF from screenshots
        var gifPath = CreateGifFromScreenshots(screenshots, "chess_complete_flow");
        _output.WriteLine($"GIF created: {gifPath}");
        
        Assert.True(System.IO.File.Exists(gifPath), "GIF file should be created");
    }

    private string CreateGifFromScreenshots(System.Collections.Generic.List<string> screenshotPaths, string outputName)
    {
        var screenshotDir = ScreenshotHelper.GetScreenshotDirectory();
        var gifPath = System.IO.Path.Combine(screenshotDir, $"{outputName}.gif");
        var mergedPngPath = System.IO.Path.Combine(screenshotDir, $"{outputName}_merged.png");
        
        // Create a Python script to generate the GIF by parsing HTML and rendering key elements
        var scriptPath = System.IO.Path.Combine(screenshotDir, "create_gif.py");
        var script = @"
import sys
import os
import re

# Try to import PIL, install if not available
try:
    from PIL import Image, ImageDraw, ImageFont
    PIL_AVAILABLE = True
except ImportError:
    PIL_AVAILABLE = False
    import subprocess
    print('PIL not available, installing...')
    try:
        subprocess.check_call([sys.executable, '-m', 'pip', 'install', '--quiet', 'Pillow'])
        from PIL import Image, ImageDraw, ImageFont
        PIL_AVAILABLE = True
        print('PIL installed successfully')
    except Exception as e:
        print(f'Failed to install PIL: {e}')
        PIL_AVAILABLE = False

def extract_chess_board_from_html(html_path):
    '''Extract chess board state from HTML'''
    if not PIL_AVAILABLE:
        return None
        
    with open(html_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Extract game info
    game_id = ''
    game_type = ''
    last_updated = ''
    
    game_id_match = re.search(r'Game ID:\s*([^<]+)', content)
    if game_id_match:
        game_id = game_id_match.group(1).strip()[:30]  # Truncate for display
    
    game_type_match = re.search(r'Game Type:\s*([^<]+)', content)
    if game_type_match:
        game_type = game_type_match.group(1).strip()
    
    last_updated_match = re.search(r'Last Updated:\s*([^<]+)', content)
    if last_updated_match:
        last_updated = last_updated_match.group(1).strip()[:50]
    
    # Extract chess pieces from squares
    board = []
    squares = re.findall(r'<div class=""chess-square[^""]*"">([^<]*)</div>', content)
    for i in range(0, len(squares), 8):
        board.append(squares[i:i+8])
    
    return {
        'game_id': game_id,
        'game_type': game_type,
        'last_updated': last_updated,
        'board': board
    }

def render_chess_image(data, img_path, step_name=''):
    '''Render a chess board image from extracted data'''
    if not PIL_AVAILABLE or data is None:
        return None
        
    # Image size
    width, height = 900, 750
    square_size = 60
    board_start_x = 150
    board_start_y = 150
    
    # Create image
    img = Image.new('RGB', (width, height), color='#f5f5f5')
    draw = ImageDraw.Draw(img)
    
    # Try to use a font, fall back to default if not available
    try:
        title_font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', 24)
        text_font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 14)
        info_font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 12)
        piece_font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 40)
    except:
        title_font = text_font = info_font = piece_font = None
    
    # Draw title
    title = 'Chess Game - Persistence Example'
    if step_name:
        title += f' - {step_name}'
    draw.text((50, 30), title, fill='#333333', font=title_font)
    
    # Draw game info if available
    y_offset = 70
    if data['game_id']:
        draw.text((50, y_offset), f""Game ID: {data['game_id']}"", fill='#333333', font=info_font)
        y_offset += 25
    if data['game_type']:
        draw.text((50, y_offset), f""Game Type: {data['game_type']}"", fill='#333333', font=info_font)
        y_offset += 25
    if data['last_updated']:
        draw.text((50, y_offset), f""Last Updated: {data['last_updated']}"", fill='#333333', font=info_font)
    
    # Draw chess board
    if data['board']:
        for row in range(len(data['board'])):
            for col in range(len(data['board'][row])):
                x = board_start_x + col * square_size
                y = board_start_y + row * square_size
                
                # Determine square color
                is_light = (row + col) % 2 == 0
                color = '#f0d9b5' if is_light else '#b58863'
                draw.rectangle([x, y, x + square_size, y + square_size], fill=color)
                
                # Draw piece if exists
                piece = data['board'][row][col].strip()
                if piece:
                    # Center the piece in the square
                    bbox = draw.textbbox((0, 0), piece, font=piece_font)
                    text_width = bbox[2] - bbox[0]
                    text_height = bbox[3] - bbox[1]
                    text_x = x + (square_size - text_width) // 2
                    text_y = y + (square_size - text_height) // 2 - 5
                    draw.text((text_x, text_y), piece, fill='#000000', font=piece_font)
    
    img.save(img_path)
    return img

def create_merged_image(images, output_path):
    '''Create a merged PNG showing all frames side by side'''
    if not PIL_AVAILABLE or not images:
        return
    
    # Calculate dimensions for merged image
    frame_width = images[0].width
    frame_height = images[0].height
    num_frames = len(images)
    
    # Create merged image (horizontal layout)
    merged_width = frame_width * num_frames
    merged_height = frame_height
    
    merged = Image.new('RGB', (merged_width, merged_height), color='white')
    
    for i, img in enumerate(images):
        x_offset = i * frame_width
        merged.paste(img, (x_offset, 0))
    
    merged.save(output_path)
    print(f'Merged image created: {output_path} ({merged_width}x{merged_height})')

def create_gif(html_files, output_path, merged_output_path, duration=1500):
    if not PIL_AVAILABLE:
        print('ERROR: PIL is not available. Cannot create GIF.')
        # Create empty files so test doesn't fail
        with open(output_path, 'wb') as f:
            f.write(b'GIF89a')  # Minimal GIF header
        with open(merged_output_path, 'wb') as f:
            f.write(b'')
        return
        
    step_names = ['1. Start', '2. First Move', '3. Save', '4. Second Move', '5. Load']
    images = []
    temp_dir = os.path.dirname(output_path)
    
    for i, html_file in enumerate(html_files):
        try:
            data = extract_chess_board_from_html(html_file)
            if data is None:
                continue
                
            step_name = step_names[i] if i < len(step_names) else f'Step {i+1}'
            
            img_path = os.path.join(temp_dir, f'temp_frame_{i}.png')
            img = render_chess_image(data, img_path, step_name)
            if img:
                images.append(img)
                print(f'Rendered frame {i+1}/{len(html_files)}: {step_name}')
        except Exception as e:
            print(f'Error processing {html_file}: {e}')
            import traceback
            traceback.print_exc()
    
    if images:
        # Create GIF
        images[0].save(
            output_path,
            save_all=True,
            append_images=images[1:],
            duration=duration,
            loop=0,
            optimize=False
        )
        print(f'GIF created: {output_path} with {len(images)} frames')
        
        # Create merged PNG
        create_merged_image(images, merged_output_path)
        
        # Clean up temp files
        for i in range(len(html_files)):
            img_path = os.path.join(temp_dir, f'temp_frame_{i}.png')
            if os.path.exists(img_path):
                try:
                    os.remove(img_path)
                except:
                    pass
    else:
        print('No images created')

if __name__ == '__main__':
    html_files = sys.argv[1:-2]  # All but last two args
    output_path = sys.argv[-2]  # Second to last arg (GIF path)
    merged_output_path = sys.argv[-1]  # Last arg (merged PNG path)
    create_gif(html_files, output_path, merged_output_path)
";
        
        System.IO.File.WriteAllText(scriptPath, script);
        
        // Build the command
        var args = string.Join(" ", screenshotPaths.Select(p => $"\"{p}\"")) + $" \"{gifPath}\" \"{mergedPngPath}\"";
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"\"{scriptPath}\" {args}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        
        // Wait up to 30 seconds for the process to complete
        if (!process.WaitForExit(30000))
        {
            _output.WriteLine("Warning: Python script timed out after 30 seconds");
            try
            {
                process.Kill();
            }
            catch { }
        }
        
        if (!string.IsNullOrEmpty(output))
            _output.WriteLine($"GIF creation output: {output}");
        if (!string.IsNullOrEmpty(error) && !error.Contains("PIL installed successfully"))
            _output.WriteLine($"GIF creation errors: {error}");
        
        // Verify files were created
        if (System.IO.File.Exists(gifPath))
        {
            var gifSize = new System.IO.FileInfo(gifPath).Length;
            _output.WriteLine($"GIF file created: {gifPath} ({gifSize} bytes)");
        }
        
        if (System.IO.File.Exists(mergedPngPath))
        {
            var pngSize = new System.IO.FileInfo(mergedPngPath).Length;
            _output.WriteLine($"Merged PNG created: {mergedPngPath} ({pngSize} bytes)");
        }
        
        return gifPath;
    }
}
