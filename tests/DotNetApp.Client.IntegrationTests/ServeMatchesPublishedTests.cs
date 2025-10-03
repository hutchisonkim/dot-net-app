using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DotNetApp.Client.IntegrationTests;

[Trait("Category","Integration")]
public class ServeMatchesPublishedTests
{
    // Prefer localhost first when running tests locally; when running inside containers 'client' may be reachable
    private static readonly string[] CandidateUrls = new[] { "http://localhost:8080/" };

    [Fact]
    public async Task ClientRootRequest_WhenServed_MatchesPublishedIndexHtml()
    {
        // Configurable overall timeout for the test (seconds). Defaults to 20s.
        var timeoutSeconds = 20;
        var envTimeout = Environment.GetEnvironmentVariable("INTEGRATION_TEST_TIMEOUT_SECONDS");
        if (!string.IsNullOrWhiteSpace(envTimeout) && int.TryParse(envTimeout, out var parsed))
        {
            timeoutSeconds = parsed;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var http = new HttpClient();

        HttpResponseMessage res = null!;
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

    res.Should().NotBeNull();
    res!.IsSuccessStatusCode.Should().BeTrue();

        var served = await res.Content.ReadAsStringAsync();

        var expectedPath = FindExpectedIndex();
    expectedPath.Should().NotBeNull();
    var expected = await File.ReadAllTextAsync(expectedPath!);

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
