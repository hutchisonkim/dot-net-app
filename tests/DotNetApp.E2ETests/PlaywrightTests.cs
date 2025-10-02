using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace DotNetApp.E2ETests;

public class PlaywrightTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task Client_Has_Title_And_BlazorLoader()
    {
        // Ensure the test as a whole cannot hang forever. Enforce a 10 second timeout
        // for the async operations inside this test. This is independent of any
        // Playwright timeouts passed to its API calls.
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://client:80";

        Func<Task> testBody = async () =>
        {
            await using var context = await _browser!.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(frontendUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

            var title = await page.TitleAsync();
            title.Should().NotBeNullOrWhiteSpace();

            var content = await page.ContentAsync();
            content.Contains("_framework/blazor.webassembly.js", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        };

        var testTask = testBody();
        var completed = await Task.WhenAny(testTask, Task.Delay(TimeSpan.FromSeconds(10)));
        if (completed != testTask)
        {
            // If the test body didn't complete within 10s, fail with a clear message.
            throw new TimeoutException("E2E test 'Client_Has_Title_And_BlazorLoader' timed out after 10 seconds.");
        }
        // Propagate any exception from the test body
        await testTask;
    }
}
