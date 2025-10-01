using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using ExampleClient.Services;

namespace ExampleClient.IntegrationTests;

public class ExampleApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExampleApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExampleApiClient_CallsApi_ReturnsHealthStatus()
    {
        using var client = _factory.CreateClient();

        // Provide the factory HttpClient to the ExampleApiClient to hit the in-memory server
        var exampleClient = new ExampleApiClient(client);

        var status = await exampleClient.FetchStatusAsync();

    Assert.Equal("healthy", status);
    }
}
