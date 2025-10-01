using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using DotNetApp.Client.Shared.Contracts;

namespace ExampleClient.IntegrationTests;

public class ExampleApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExampleApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PlatformApiClient_CallsApi_ReturnsHealthStatus()
    {
        using var client = _factory.CreateClient();
        var apiClient = new PlatformApiClient(client);
        var dto = await apiClient.GetHealthStatusAsync();
        Assert.Equal("healthy", dto?.status);
    }
}
