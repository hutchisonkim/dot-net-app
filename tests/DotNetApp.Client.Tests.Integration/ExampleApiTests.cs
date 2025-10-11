using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using DotNetApp.Client.Contracts;

namespace DotNetApp.Client.Tests.Integration;

[Trait("Category", "Integration")]
public class ExampleApiTests : IClassFixture<WebApplicationFactory<DotNetApp.Server.Program>>
{
    private readonly WebApplicationFactory<DotNetApp.Server.Program> _factory;

    public ExampleApiTests(WebApplicationFactory<DotNetApp.Server.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PlatformApiClient_CallsApi_ReturnsHealthStatus()
    {
        using var client = _factory.CreateClient();
        var apiClient = new PlatformApiClient(client);
        var dto = await apiClient.GetHealthStatusAsync();
        // API returns "Healthy" (capital H) — tests should match the real API behavior
        Assert.Equal("Healthy", dto?.status);
    }
}
