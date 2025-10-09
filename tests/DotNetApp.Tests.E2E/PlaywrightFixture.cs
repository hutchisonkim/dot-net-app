using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace DotNetApp.Tests.E2E;

public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright PlaywrightInstance { get; private set; } = default!;
    public IBrowser Browser { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        PlaywrightInstance = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
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
