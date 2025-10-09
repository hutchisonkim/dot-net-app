using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using DotNetApp.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetApp.Server.Tests.Integration;

// Integration-style test verifies the ASP.NET Core pipeline + DI wiring using WebApplicationFactory.
// Per Microsoft guidance, this belongs in an integration test project (not a pure unit test assembly)
// to keep unit tests fast and isolated.
[Trait("Category","Integration")]
public class HealthEndpointTests : IClassFixture<HealthEndpointTests.CustomWebAppFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(CustomWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_WhenCalled_ReturnsMockedStatus()
    {
    var json = await _client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/state/health");
    Assert.Equal(FakeHealthService.CustomStatus, json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Health_JsonSerialization_UsesCamelCasePropertyNames()
    {
        // This test verifies the API contract: JSON property names should be camelCase (ASP.NET Core default)
        var response = await _client.GetAsync("/api/state/health");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify the JSON contains "status" (camelCase) not "Status" (PascalCase)
        Assert.Contains("\"status\":", content);
        Assert.DoesNotContain("\"Status\":", content);
    }

    [Fact]
    public async Task Health_StatusValue_PreservesCasing()
    {
        // This test verifies that status values preserve their casing
        // The API should return the exact status value from HealthStatus (e.g., "Healthy" not "healthy")
        var json = await _client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/state/health");
        var statusValue = json.GetProperty("status").GetString();
        
        // Verify the status value is exactly what the service returns
        Assert.Equal(FakeHealthService.CustomStatus, statusValue);
    }

    // Frontend no longer hosted by the server. If you expect SPA content, run the frontend separately and configure CORS.

    public class CustomWebAppFactory : WebApplicationFactory<DotNetApp.Server.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Replace real services with fakes
                var healthDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IHealthService));
                if (healthDescriptor != null) services.Remove(healthDescriptor);
                services.AddSingleton<IHealthService, FakeHealthService>();
            });
        }
    }

    private class FakeHealthService : IHealthService
    {
        public const string CustomStatus = "integration-mocked";
        public Task<string> GetStatusAsync(CancellationToken cancellationToken = default) => Task.FromResult(CustomStatus);
    }
}
