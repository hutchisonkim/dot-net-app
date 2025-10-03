using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetApp.Client.Shared.Contracts;

// Minimal client implementation used for testing and local builds when codegen is not available.
public sealed class PlatformApiClient : IPlatformApi
{
    private readonly HttpClient _http;
    public PlatformApiClient(HttpClient http) => _http = http;

    public Task<HealthStatusDto?> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        // Matches generated client's behavior: GET api/state/health
        return _http.GetFromJsonAsync<HealthStatusDto?>("api/state/health", cancellationToken);
    }
}
