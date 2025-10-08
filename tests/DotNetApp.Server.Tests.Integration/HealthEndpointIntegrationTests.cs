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
public class HealthEndpointIntegrationTests : IClassFixture<HealthEndpointIntegrationTests.CustomWebAppFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointIntegrationTests(CustomWebAppFactory factory)
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
    public async Task RootRequest_WhenFrontendConfigured_ReturnsFakeIndex()
    {
    var html = await _client.GetStringAsync("/");
    Assert.Contains("Fake Frontend", html);
    }

    public class CustomWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
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

    private class FakeClientAssetConfigurator : IClientAssetConfigurator
    {
        public const string Html = "<html><head><title>Fake Index</title></head><body><h1>Fake Frontend</h1></body></html>";
        public void Configure(object appBuilder)
        {
            if (appBuilder is not IApplicationBuilder app) return;
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path == "/" || ctx.Request.Path == "/index.html")
                {
                    ctx.Response.ContentType = "text/html";
                    await ctx.Response.WriteAsync(Html);
                    return;
                }
                await next();
            });
        }
    }

    private class FakeHealthService : IHealthService
    {
        public const string CustomStatus = "integration-mocked";
        public Task<string> GetStatusAsync(CancellationToken cancellationToken = default) => Task.FromResult(CustomStatus);
    }
}
