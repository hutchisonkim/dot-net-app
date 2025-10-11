using System;
using System.IO;
using System.Text;
using Bunit;

namespace Examples.Tests.UI;

/// <summary>
/// Helper class for capturing HTML screenshots from bUnit rendered components.
/// Follows MSDN guidelines for test utilities.
/// </summary>
public static class ScreenshotHelper
{
    private static readonly string ScreenshotDirectory = Path.Combine(
        Directory.GetCurrentDirectory(),
        "screenshots"
    );

    static ScreenshotHelper()
    {
        // Ensure screenshot directory exists
        if (!Directory.Exists(ScreenshotDirectory))
        {
            Directory.CreateDirectory(ScreenshotDirectory);
        }
    }

    /// <summary>
    /// Captures the rendered HTML markup and saves it as an HTML file.
    /// </summary>
    /// <param name="component">The rendered component to capture</param>
    /// <param name="fileName">Base name for the screenshot file (without extension)</param>
    public static string CaptureHtml(IRenderedFragment component, string fileName)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or whitespace.", nameof(fileName));

        var sanitizedFileName = SanitizeFileName(fileName);
        var filePath = Path.Combine(ScreenshotDirectory, $"{sanitizedFileName}.html");

        // Create a complete HTML document with styles
        var htmlContent = CreateCompleteHtmlDocument(component.Markup);

        File.WriteAllText(filePath, htmlContent, Encoding.UTF8);

        return filePath;
    }

    /// <summary>
    /// Captures raw HTML content and saves it as an HTML file.
    /// Useful for creating mock states for screenshots.
    /// </summary>
    /// <param name="htmlContent">The HTML content to save</param>
    /// <param name="fileName">Base name for the screenshot file (without extension)</param>
    public static string CaptureHtmlContent(string htmlContent, string fileName)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            throw new ArgumentException("HTML content cannot be null or whitespace.", nameof(htmlContent));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or whitespace.", nameof(fileName));

        var sanitizedFileName = SanitizeFileName(fileName);
        var filePath = Path.Combine(ScreenshotDirectory, $"{sanitizedFileName}.html");

        // Create a complete HTML document with styles
        var completeHtmlContent = CreateCompleteHtmlDocument(htmlContent);

        File.WriteAllText(filePath, completeHtmlContent, Encoding.UTF8);

        return filePath;
    }

    /// <summary>
    /// Creates a complete HTML document with the component markup embedded.
    /// </summary>
    private static string CreateCompleteHtmlDocument(string componentMarkup)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>Component Screenshot</title>
    <style>
        body {{ 
            font-family: Arial, sans-serif; 
            margin: 20px; 
            background-color: #f5f5f5;
        }}
        .chess-board {{ 
            display: grid; 
            grid-template-columns: repeat(8, 50px); 
            gap: 0; 
            margin: 20px 0; 
        }}
        .chess-square {{ 
            width: 50px; 
            height: 50px; 
            display: flex; 
            align-items: center; 
            justify-content: center; 
            font-size: 30px; 
        }}
        .chess-square.light {{ background-color: #f0d9b5; }}
        .chess-square.dark {{ background-color: #b58863; }}
        .game-container {{ 
            display: flex; 
            flex-direction: column; 
            align-items: center; 
        }}
        .pong-canvas {{ 
            border: 2px solid #fff; 
            background-color: #000; 
            margin: 20px 0; 
        }}
        button {{
            margin: 5px;
            padding: 10px 20px;
            font-size: 14px;
            cursor: pointer;
        }}
        input {{
            padding: 5px;
            margin: 5px;
        }}
    </style>
</head>
<body>
    {componentMarkup}
</body>
</html>";
    }

    /// <summary>
    /// Sanitizes a file name by removing invalid characters.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = fileName;
        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }
        return sanitized;
    }

    /// <summary>
    /// Gets the path to the screenshot directory.
    /// </summary>
    public static string GetScreenshotDirectory() => ScreenshotDirectory;
}
