using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Xunit;
using DotNetApp.Tests.Shared;

#nullable enable

namespace DotNetApp.Server.Tests.Integration;

[Trait("Category","Integration")]
[Collection("docker-compose")]
public class ServeClientFromBackendTests
{
    private readonly string[] CandidateUrls;

    public ServeClientFromBackendTests(LocalStaticFrontendFixture fixture)
    {
        // Prefer fixture-provided FrontendUrl when the collection fixture started a local server.
        if (!string.IsNullOrWhiteSpace(fixture?.FrontendUrl))
        {
            CandidateUrls = new[] { fixture.FrontendUrl };
        }
        else
        {
            CandidateUrls = BuildCandidateUrls();
        }
    }

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
    public async Task ClientRootRequest_WhenServed_MatchesExpectedIndex()
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

        HttpResponseMessage res = null!;
        // Try each candidate URL until one responds with success or we hit the overall timeout
    foreach (var baseUrl in CandidateUrls)
        {
            try
            {
                res = await DotNetApp.Tests.Shared.HttpRetry.WaitForSuccessAsync(() => http.GetAsync(baseUrl, cts.Token), TimeSpan.FromSeconds(timeoutSeconds), TimeSpan.FromSeconds(1), cts.Token);
                break;
            }
            catch (TimeoutException)
            {
                // try next candidate URL
            }
        }

    Assert.NotNull(res);

        var served = await res.Content.ReadAsStringAsync();

        var expectedPath = FindExpectedIndex();
    Assert.NotNull(expectedPath);

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
                Assert.Contains(expectedTitle, served);
            }

        var baseMatch = Regex.Match(expected, "<base href=\"(.*?)\"", RegexOptions.IgnoreCase);
            if (baseMatch.Success)
            {
                var expectedBase = baseMatch.Groups[1].Value.Trim();
                Assert.Contains($"<base href=\"{expectedBase}\"", served);
            }

    // Always check for the Blazor loader
    Assert.Contains("_framework/blazor.webassembly.js", served);

    Assert.Contains(nExpected, nServed);
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
