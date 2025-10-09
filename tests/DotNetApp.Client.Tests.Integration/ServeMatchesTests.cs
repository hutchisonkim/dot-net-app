using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Xunit;
using DotNetApp.Tests.Shared;

namespace DotNetApp.Client.Tests.Integration;

[Trait("Category","Integration")]
[Collection("docker-compose")]
public class ServeMatchesTests
{
    private readonly string[] CandidateUrls;

    public ServeMatchesTests(LocalStaticFrontendFixture fixture)
    {
        if (!string.IsNullOrWhiteSpace(fixture?.FrontendUrl))
        {
            CandidateUrls = new[] { fixture.FrontendUrl };
        }
        else
        {
            CandidateUrls = new[] { "http://localhost:8080/" };
        }
    }
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
                res = await DotNetApp.Tests.Shared.HttpRetryPolicy.WaitForSuccessAsync(() => http.GetAsync(baseUrl, cts.Token), TimeSpan.FromSeconds(timeoutSeconds), TimeSpan.FromSeconds(1), cts.Token);
                break;
            }
            catch (TimeoutException)
            {
                // try next candidate URL
            }
        }

    Assert.NotNull(res);
    Assert.True(res!.IsSuccessStatusCode);

        var served = await res.Content.ReadAsStringAsync();

        var expectedPath = FindExpectedIndex();
    Assert.NotNull(expectedPath);
    var expected = await File.ReadAllTextAsync(expectedPath!);

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
