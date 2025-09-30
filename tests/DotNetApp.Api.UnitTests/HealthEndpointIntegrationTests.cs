using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DotNetApp.Api.UnitTests.Fakes;
using DotNetApp.Core.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;

namespace DotNetApp.Api.UnitTests;

public class CustomWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace real services with fakes
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IClientAssetConfigurator));
            if (descriptor != null) services.Remove(descriptor);
            services.AddSingleton<IClientAssetConfigurator, FakeClientAssetConfigurator>();

            var healthDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IHealthService));
            if (healthDescriptor != null) services.Remove(healthDescriptor);
            services.AddSingleton<IHealthService, FakeHealthService>();
        });
    }
}

public class HealthEndpointIntegrationTests : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointIntegrationTests(CustomWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_Returns_Fake_Status_From_Mocked_Service()
    {
        var json = await _client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/state/health");
        json.GetProperty("status").GetString().Should().Be(FakeHealthService.CustomStatus);
    }

    [Fact]
    public async Task Root_Serves_Fake_Index_From_FakeConfigurator()
    {
        var html = await _client.GetStringAsync("/");
        html.Should().Contain("Fake Frontend");
    }
}