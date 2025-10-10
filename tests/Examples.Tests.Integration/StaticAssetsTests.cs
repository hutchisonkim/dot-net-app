using System.IO;
using Xunit;

namespace Examples.Tests.Integration;

[Trait("Category", "Integration")]
public class StaticAssetsTests
{
    [Theory]
    [InlineData("Chess")]
    [InlineData("Pong")]
    public void Example_HasRequiredStaticAssets(string gameName)
    {
        // Arrange
        var projectRoot = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..",
            "examples", gameName, "wwwroot"));

        // Assert - Check index.html exists
        var indexPath = Path.Combine(projectRoot, "index.html");
        Assert.True(File.Exists(indexPath), 
            $"{gameName} must have an index.html in wwwroot folder. Path checked: {indexPath}");

        // Assert - Check favicon.ico exists
        var faviconPath = Path.Combine(projectRoot, "favicon.ico");
        Assert.True(File.Exists(faviconPath), 
            $"{gameName} must have a favicon.ico in wwwroot folder to prevent 404 errors. Path checked: {faviconPath}");
    }

    [Theory]
    [InlineData("Chess")]
    [InlineData("Pong")]
    public void Example_IndexHtml_ReferencesFavicon(string gameName)
    {
        // Arrange
        var projectRoot = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..",
            "examples", gameName, "wwwroot"));

        var indexPath = Path.Combine(projectRoot, "index.html");
        
        // Act
        var indexContent = File.ReadAllText(indexPath);

        // Assert - Index.html should reference the favicon
        Assert.Contains("favicon.ico", indexContent);
    }
}
