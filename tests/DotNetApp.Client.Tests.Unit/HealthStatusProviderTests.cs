using System.Threading.Tasks;
using Xunit;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using DotNetApp.Client;

namespace DotNetApp.Client.Tests.Unit;

[Trait("Category","Unit")]
public class HealthStatusProviderTests
{
    [Fact]
    public async Task FetchStatusAsync_WhenCalled_ReturnsHealthy()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"healthy\" }", System.Net.HttpStatusCode.OK);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var provider = ctx.Services.GetRequiredService<IHealthStatusProvider>();
        var status = await provider.FetchStatusAsync();

    // Assert
    Assert.Equal("healthy", status);
    }
}
