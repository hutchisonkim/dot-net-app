using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace DotNetApp.Tests.E2E;

public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright? PlaywrightInstance { get; private set; }
    public IBrowser? Browser { get; private set; }
    public string? SkipReason { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            PlaywrightInstance = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist"))
        {
            SkipReason = "Playwright browsers not installed. Run: playwright install chromium";
            PlaywrightInstance?.Dispose();
            PlaywrightInstance = null;
        }
        catch (Exception ex)
        {
            SkipReason = $"Failed to initialize Playwright: {ex.Message}";
            PlaywrightInstance?.Dispose();
            PlaywrightInstance = null;
        }
    }

    public async Task DisposeAsync()
    {
        if (Browser != null) await Browser.CloseAsync();
        PlaywrightInstance?.Dispose();
    }
}

[CollectionDefinition("Playwright E2E")]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
    // Intentionally empty - serves only as a marker for test collection
}
