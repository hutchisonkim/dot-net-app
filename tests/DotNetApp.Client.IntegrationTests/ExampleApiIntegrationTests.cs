using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using DotNetApp.Client.Contracts;

namespace DotNetApp.Client.IntegrationTests;

public class ExampleApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExampleApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PlatformApiClient_CallsApi_ReturnsHealthStatus()
    {
        using var client = _factory.CreateClient();
        var apiClient = new PlatformApiClient(client);
        var dto = await apiClient.GetHealthStatusAsync();
        // API returns "Healthy" (capital H) — tests should match the real API behavior
        dto?.status.Should().Be("Healthy");
    }
}
