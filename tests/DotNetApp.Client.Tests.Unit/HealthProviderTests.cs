using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using DotNetApp.Client;
using DotNetApp.Client.Contracts;
using Moq;

namespace DotNetApp.Client.Tests.Unit;

[Trait("Category","Unit")]
public class HealthProviderTests
{
    [Fact]
    public async Task FetchStatusAsync_WhenCalled_ReturnsHealthy()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"Healthy\" }", System.Net.HttpStatusCode.OK);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var provider = ctx.Services.GetRequiredService<IHealthStatusProvider>();
        var status = await provider.FetchStatusAsync();

        // Assert
        Assert.Equal("Healthy", status);
    }

    [Fact]
    public async Task FetchStatusAsync_WithCancellationToken_ReturnsStatus()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"Healthy\" }", System.Net.HttpStatusCode.OK);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");
        using var cts = new CancellationTokenSource();

        // Act
        var provider = ctx.Services.GetRequiredService<IHealthStatusProvider>();
        var status = await provider.FetchStatusAsync(cts.Token);

        // Assert
        Assert.Equal("Healthy", status);
    }

    [Fact]
    public async Task FetchStatusAsync_WhenApiReturnsNull_ReturnsNull()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("null", System.Net.HttpStatusCode.OK);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var provider = ctx.Services.GetRequiredService<IHealthStatusProvider>();
        var status = await provider.FetchStatusAsync();

        // Assert
        Assert.Null(status);
    }

    [Fact]
    public async Task FetchStatusAsync_WhenApiReturnsNullStatus_ReturnsNull()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": null }", System.Net.HttpStatusCode.OK);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var provider = ctx.Services.GetRequiredService<IHealthStatusProvider>();
        var status = await provider.FetchStatusAsync();

        // Assert
        Assert.Null(status);
    }

    [Fact]
    public async Task FetchStatusAsync_WithDifferentStatus_ReturnsCorrectValue()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"degraded\" }", System.Net.HttpStatusCode.OK);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var provider = ctx.Services.GetRequiredService<IHealthStatusProvider>();
        var status = await provider.FetchStatusAsync();

        // Assert
        Assert.Equal("degraded", status);
    }

    [Fact]
    public async Task PlatformApiHealthStatusProviderAdapter_WithMockApi_ReturnsStatus()
    {
        // Arrange
        var mockApi = new Mock<IPlatformApi>();
        mockApi.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealthStatusDto("mock-status"));
        var adapter = new PlatformApiHealthStatusProviderAdapter(mockApi.Object);

        // Act
        var result = await adapter.FetchStatusAsync();

        // Assert
        Assert.Equal("mock-status", result);
        mockApi.Verify(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlatformApiHealthStatusProviderAdapter_WhenApiReturnsNull_ReturnsNull()
    {
        // Arrange
        var mockApi = new Mock<IPlatformApi>();
        mockApi.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((HealthStatusDto?)null);
        var adapter = new PlatformApiHealthStatusProviderAdapter(mockApi.Object);

        // Act
        var result = await adapter.FetchStatusAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task PlatformApiHealthStatusProviderAdapter_PassesCancellationToken()
    {
        // Arrange
        var mockApi = new Mock<IPlatformApi>();
        mockApi.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealthStatusDto("test"));
        var adapter = new PlatformApiHealthStatusProviderAdapter(mockApi.Object);
        using var cts = new CancellationTokenSource();

        // Act
        await adapter.FetchStatusAsync(cts.Token);

        // Assert
        mockApi.Verify(x => x.GetHealthStatusAsync(cts.Token), Times.Once);
    }
}
