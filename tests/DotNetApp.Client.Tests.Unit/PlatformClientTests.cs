using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DotNetApp.Client.Contracts;

namespace DotNetApp.Client.Tests.Unit;

[Trait("Category", "Unit")]
public class PlatformClientTests
{
    [Fact]
    public async Task GetHealthStatusAsync_ReturnsHealthStatusDto()
    {
        // Arrange
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"Healthy\" }", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var client = new PlatformApiClient(httpClient);

        // Act
        var result = await client.GetHealthStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Healthy", result.status);
    }

    [Fact]
    public async Task GetHealthStatusAsync_WithCancellationToken_ReturnsHealthStatusDto()
    {
        // Arrange
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"Healthy\" }", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var client = new PlatformApiClient(httpClient);
        using var cts = new CancellationTokenSource();

        // Act
        var result = await client.GetHealthStatusAsync(cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Healthy", result.status);
    }

    [Fact]
    public async Task GetHealthStatusAsync_WithNullStatus_ReturnsNullStatusProperty()
    {
        // Arrange
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": null }", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var client = new PlatformApiClient(httpClient);

        // Act
        var result = await client.GetHealthStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.status);
    }

    [Fact]
    public async Task GetHealthStatusAsync_WithEmptyResponse_ReturnsNull()
    {
        // Arrange
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("null", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var client = new PlatformApiClient(httpClient);

        // Act
        var result = await client.GetHealthStatusAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetHealthStatusAsync_WithDifferentStatus_ReturnsCorrectValue()
    {
        // Arrange
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"degraded\" }", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var client = new PlatformApiClient(httpClient);

        // Act
        var result = await client.GetHealthStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("degraded", result.status);
    }
}
