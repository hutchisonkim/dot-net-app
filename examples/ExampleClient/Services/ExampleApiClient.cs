using System.Net.Http.Json;
using DotNetApp.Core.Abstractions;

namespace ExampleClient.Services;

public class ExampleApiClient : IHealthStatusProvider
{
    private readonly HttpClient _http;

    public ExampleApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<string?> FetchStatusAsync(CancellationToken cancellationToken = default)
    {
        // Call the API endpoint exposed by DotNetApp.Api: GET /api/state/health
        try
        {
            var resp = await _http.GetFromJsonAsync<HealthResponse?>("api/state/health", cancellationToken: cancellationToken);
            return resp?.Status;
        }
        catch
        {
            // For the example keep failure handling simple and return null on errors
            return null;
        }
    }
}

public record HealthResponse(string? Status);
