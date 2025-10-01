using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using DotNetApp.Client.Shared;
using DotNetApp.Core.Abstractions;

namespace DotNetApp.Client.UnitTests;

public class HealthStatusProviderTests
{
    [Fact]
    public async Task ApiClient_AsProvider_Returns_Status()
    {
        using var ctx = new BunitContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"healthy\" }", System.Net.HttpStatusCode.OK);
        var client = new System.Net.Http.HttpClient(handler) { BaseAddress = new System.Uri("http://localhost/") };
    ctx.Services.AddSingleton<System.Net.Http.HttpClient>(client);
    var inMemory = new System.Collections.Generic.Dictionary<string, string> { ["ApiBaseAddress"] = "http://localhost/" };
    ctx.Services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(new DotNetApp.Client.Tests.SimpleConfiguration(inMemory));
    ctx.Services.AddDotNetAppClient(explicitBaseAddress: "http://localhost/");

        var provider = ctx.Services.GetRequiredService<IHealthStatusProvider>();
        var status = await provider.FetchStatusAsync();
        status.Should().Be("healthy");
    }
}
