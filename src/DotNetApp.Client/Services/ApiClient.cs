using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

using DotNetApp.Core.Abstractions;

namespace DotNetApp.Client.Services;

public class ApiClient : IHealthStatusProvider
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public ApiClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public class HealthResponse { public string? status { get; set; } }

    public async Task<HealthResponse?> GetHealthAsync()
    {
        try
        {
            var res = await _http.GetFromJsonAsync<HealthResponse?>("api/state/health");
            if (res?.status != null) return res;
        }
        catch
        {
        }

        var configured = _config["ApiBaseAddress"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            try
            {
                using var client = new HttpClient { BaseAddress = new System.Uri(configured) };
                var res = await client.GetFromJsonAsync<HealthResponse?>("api/state/health");
                if (res?.status != null) return res;
            }
            catch
            {
            }
        }

        return null;
    }

    public async Task<string?> FetchStatusAsync(CancellationToken cancellationToken = default)
    {
        var health = await GetHealthAsync();
        return health?.status;
    }
}

