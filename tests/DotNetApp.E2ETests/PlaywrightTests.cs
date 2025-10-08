#if RUN_E2E
using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace DotNetApp.Tests.E2E;

[Collection("Playwright E2E")] // Leverage shared browser instance
public class PlaywrightTests
{
    private readonly PlaywrightSharedFixture _fx;
    public PlaywrightTests(PlaywrightSharedFixture fx) => _fx = fx;

    [Fact]
    [Trait("Category", "E2E")]
    public async Task Client_Index_Loads_BlazorRuntime()
    {
        // Ensure the test as a whole cannot hang forever. Enforce a 10 second timeout
        // for the async operations inside this test. This is independent of any
        // Playwright timeouts passed to its API calls.
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://client:80";

        Func<Task> testBody = async () =>
        {
            await using var context = await _fx.Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(frontendUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

            var title = await page.TitleAsync();
            Assert.False(string.IsNullOrWhiteSpace(title));

            var content = await page.ContentAsync();
            Assert.True(content?.IndexOf("_framework/blazor.webassembly.js", StringComparison.OrdinalIgnoreCase) >= 0);
        };

        var testTask = testBody();
        var completed = await Task.WhenAny(testTask, Task.Delay(TimeSpan.FromSeconds(10)));
        if (completed != testTask)
        {
            // If the test body didn't complete within 10s, fail with a clear message.
            throw new TimeoutException("E2E test 'Client_Index_Loads_BlazorRuntime' timed out after 10 seconds.");
        }
        // Propagate any exception from the test body
        await testTask;
    }
}
#endif
