using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using FluentAssertions;
using Xunit;

#nullable enable

namespace DotNetApp.Api.IntegrationTests;

[Trait("Category","Integration")]
[Collection("docker-compose")]
public class ServeClientFromBackendTests
{
    // Allow an override for CI or local runs
    private static readonly string[] CandidateUrls = BuildCandidateUrls();

    private static string[] BuildCandidateUrls()
    {
        var env = Environment.GetEnvironmentVariable("FRONTEND_URL");
        if (!string.IsNullOrWhiteSpace(env)) return new[] { env };
    // Inside docker-compose the backend API service is named 'api' (internal port 8080) and the frontend 'client' listens on port 80
    // Prefer localhost first for local runs; keep api and client as fallbacks
    return new[] { "http://localhost:8080/" };
    }

    [Fact]
    [Trait("Category","Integration")]
    public async Task ClientServed_IsSameAsPublishedIndex()
    {
        // Configurable overall timeout for the test (seconds). Defaults to 20s to avoid flakiness.
        var timeoutSeconds = 20;
        var envTimeout = Environment.GetEnvironmentVariable("INTEGRATION_TEST_TIMEOUT_SECONDS");
        if (!string.IsNullOrWhiteSpace(envTimeout) && int.TryParse(envTimeout, out var parsed))
        {
            timeoutSeconds = parsed;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var http = new HttpClient();

        HttpResponseMessage? res = null;
        Exception? lastEx = null;

        // Try each candidate URL until one responds with success or we hit the overall timeout
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(timeoutSeconds);
        foreach (var baseUrl in CandidateUrls)
        {
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    res = await http.GetAsync(baseUrl, cts.Token);
                    if (res.IsSuccessStatusCode) break;
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    throw new TimeoutException($"Integration test timed out after {timeoutSeconds} seconds while waiting for frontend to respond. Last exception: {lastEx?.Message}");
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                }
                await Task.Delay(1000, cts.Token);
            }
            if (res != null && res.IsSuccessStatusCode) break;
        }

        res.Should().NotBeNull("No response received from frontend service. Last exception: {0}", lastEx?.Message ?? "none");
        res!.IsSuccessStatusCode.Should().BeTrue($"frontend returned non-success status: {(int)res.StatusCode}");

        var served = await res.Content.ReadAsStringAsync();

        var expectedPath = FindExpectedIndex();
        expectedPath.Should().NotBeNull("Expected index file not found. Searched from current/base directories and mounted path");

        string expected;
        if (expectedPath!.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
        {
            var razor = await File.ReadAllTextAsync(expectedPath);
            var h1Match = Regex.Match(razor, "<h1>(.*?)</h1>", RegexOptions.IgnoreCase);
            if (h1Match.Success)
            {
                expected = h1Match.Groups[1].Value.Trim();
            }
            else
            {
                expected = Regex.Replace(razor, @"\r?\n|\s+", " ").Trim();
            }
        }
        else
        {
            expected = await File.ReadAllTextAsync(expectedPath!);
        }

        var nServed = Normalize(served);
        var nExpected = Normalize(expected);

        var titleMatch = Regex.Match(expected, "<title>(.*?)</title>", RegexOptions.IgnoreCase);
        if (titleMatch.Success)
        {
            var expectedTitle = titleMatch.Groups[1].Value.Trim();
            served.Should().Contain(expectedTitle, "served HTML should contain the expected <title> text");
        }

        var baseMatch = Regex.Match(expected, "<base href=\"(.*?)\"", RegexOptions.IgnoreCase);
        if (baseMatch.Success)
        {
            var expectedBase = baseMatch.Groups[1].Value.Trim();
            served.Should().Contain($"<base href=\"{expectedBase}\"", "served HTML should contain the expected base href");
        }

        // Always check for the Blazor loader
        served.Should().Contain("_framework/blazor.webassembly.js", "served HTML must include the Blazor loader");

        nServed.Should().Contain(nExpected, "served normalized HTML should include the normalized expected index content");
    }

    private static string? FindExpectedIndex()
    {
        var relativeCandidates = new[] {
            Path.Combine("src","DotNetApp.Client","wwwroot","index.html"),
            Path.Combine("src","DotNetApp.Client","bin","Debug","net8.0","wwwroot","index.html"),
            Path.Combine("src","DotNetApp.Client","bin","Release","net8.0","wwwroot","index.html")
        };

        var startDirs = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
        foreach (var start in startDirs)
        {
            var dir = start;
            for (int up = 0; up < 6; up++)
            {
                foreach (var rel in relativeCandidates)
                {
                    var candidate = Path.GetFullPath(Path.Combine(dir, rel));
                    if (File.Exists(candidate)) return candidate;
                }
                dir = Path.GetFullPath(Path.Combine(dir, ".."));
            }
        }

        var mounted = Path.Combine(Path.DirectorySeparatorChar.ToString(), "src", "DotNetApp.Client", "wwwroot", "index.html");
        if (File.Exists(mounted)) return mounted;
        return null;
    }

    private static string Normalize(string html)
    {
        if (html == null) return string.Empty;
        var collapsed = Regex.Replace(html, @"\r?\n|\s+", " ");
        return collapsed.Trim();
    }
}
