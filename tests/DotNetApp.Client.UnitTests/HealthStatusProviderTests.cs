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
    [Trait("Category","Unit")]
    public async Task ApiClient_AsProvider_Returns_Status()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"healthy\" }", System.Net.HttpStatusCode.OK);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var provider = ctx.Services.GetRequiredService<IHealthStatusProvider>();
        var status = await provider.FetchStatusAsync();

        // Assert
        status.Should().Be("healthy");
    }
}
