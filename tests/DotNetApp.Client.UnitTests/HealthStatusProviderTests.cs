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
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        var provider = ctx.Services.GetRequiredService<IHealthStatusProvider>();
        var status = await provider.FetchStatusAsync();
        status.Should().Be("healthy");
    }
}
