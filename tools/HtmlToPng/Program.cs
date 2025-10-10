using Microsoft.Playwright;
static string GetArg(string[] args, string name, string defaultValue)
{
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 < args.Length) return args[i + 1];
            break;
        }
        if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
        {
            return args[i].Substring(name.Length + 1);
        }
    }
    return defaultValue;
}

static int GetIntArg(string[] args, string name, int defaultValue)
{
    var val = GetArg(args, name, defaultValue.ToString());
    if (int.TryParse(val, out var result)) return result;
    return defaultValue;
}

var input = GetArg(args, "--input", "tests/Examples.Tests.UI/bin/Debug/net8.0/screenshots");
var output = GetArg(args, "--output", "coverage-report/screenshots-png");
var width = GetIntArg(args, "--width", 900);
var height = GetIntArg(args, "--height", 700);

input = Path.GetFullPath(input);
output = Path.GetFullPath(output);

if (!Directory.Exists(input))
{
    Console.WriteLine($"Input directory not found: {input}");
    return 0;
}

Directory.CreateDirectory(output);

using var pw = await Playwright.CreateAsync();
await using var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = true
});

var htmlFiles = Directory.GetFiles(input, "*.html");
Console.WriteLine($"Found {htmlFiles.Length} HTML files in {input}");

foreach (var htmlFile in htmlFiles)
{
    var fileName = Path.GetFileNameWithoutExtension(htmlFile);
    var pngPath = Path.Combine(output, fileName + ".png");
    var page = await browser.NewPageAsync(new BrowserNewPageOptions
    {
        ViewportSize = new ViewportSize { Width = width, Height = height }
    });

    var uri = new Uri(htmlFile);
    await page.GotoAsync(uri.AbsoluteUri, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    await page.WaitForTimeoutAsync(150);

    await page.ScreenshotAsync(new PageScreenshotOptions
    {
        Path = pngPath,
        FullPage = true
    });

    await page.CloseAsync();
    Console.WriteLine($"Wrote {pngPath}");
}

Console.WriteLine("Done.");
return 0;
