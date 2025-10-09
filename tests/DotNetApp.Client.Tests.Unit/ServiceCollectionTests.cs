using System;
using System.Net.Http;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using DotNetApp.Client;
using DotNetApp.Client.Contracts;

namespace DotNetApp.Client.Tests.Unit;

[Trait("Category", "Unit")]
public class ServiceCollectionTests
{
    [Fact]
    public void AddPlatformApi_WithoutHandler_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddPlatformApi();

        // Assert
        Assert.Same(services, result);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<PlatformApiClient>());
        Assert.NotNull(provider.GetService<IPlatformApi>());
        Assert.NotNull(provider.GetService<IHealthStatusProvider>());
    }

    [Fact]
    public void AddPlatformApi_WithoutHandler_AndBaseAddress_SetsBaseAddress()
    {
        // Arrange
        var services = new ServiceCollection();
        const string baseAddress = "http://localhost:5000/";

        // Act
        services.AddPlatformApi(baseAddress);

        // Assert
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<PlatformApiClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddPlatformApi_WithoutHandler_AndNullBaseAddress_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPlatformApi(baseAddress: null);

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IPlatformApi>());
    }

    [Fact]
    public void AddPlatformApi_WithoutHandler_AndEmptyBaseAddress_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPlatformApi(baseAddress: "");

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IPlatformApi>());
    }

    [Fact]
    public void AddPlatformApi_WithoutHandler_AndWhitespaceBaseAddress_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPlatformApi(baseAddress: "   ");

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IPlatformApi>());
    }

    [Fact]
    public void AddPlatformApi_WithHandler_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestHttpMessageHandler("{ \"status\": \"test\" }", System.Net.HttpStatusCode.OK);

        // Act
        var result = services.AddPlatformApi(handler);

        // Assert
        Assert.Same(services, result);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<PlatformApiClient>());
        Assert.NotNull(provider.GetService<IPlatformApi>());
        Assert.NotNull(provider.GetService<IHealthStatusProvider>());
    }

    [Fact]
    public void AddPlatformApi_WithHandler_AndBaseAddress_SetsBaseAddress()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestHttpMessageHandler("{ \"status\": \"test\" }", System.Net.HttpStatusCode.OK);
        const string baseAddress = "http://localhost:5000/";

        // Act
        services.AddPlatformApi(handler, baseAddress);

        // Assert
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<PlatformApiClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddPlatformApi_WithHandler_AndNullBaseAddress_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestHttpMessageHandler("{ \"status\": \"test\" }", System.Net.HttpStatusCode.OK);

        // Act
        services.AddPlatformApi(handler, baseAddress: null);

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IPlatformApi>());
    }

    [Fact]
    public void AddPlatformApi_WithHandler_AndEmptyBaseAddress_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestHttpMessageHandler("{ \"status\": \"test\" }", System.Net.HttpStatusCode.OK);

        // Act
        services.AddPlatformApi(handler, baseAddress: "");

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IPlatformApi>());
    }

    [Fact]
    public void AddPlatformApi_ResolvesIPlatformApi_ReturnsPlatformApiClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestHttpMessageHandler("{ \"status\": \"test\" }", System.Net.HttpStatusCode.OK);
        services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var provider = services.BuildServiceProvider();
        var api = provider.GetService<IPlatformApi>();

        // Assert
        Assert.NotNull(api);
        Assert.IsType<PlatformApiClient>(api);
    }

    [Fact]
    public void AddPlatformApi_ResolvesIHealthStatusProvider_ReturnsPlatformApiHealthStatusProviderAdapter()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestHttpMessageHandler("{ \"status\": \"test\" }", System.Net.HttpStatusCode.OK);
        services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var provider = services.BuildServiceProvider();
        var healthProvider = provider.GetService<IHealthStatusProvider>();

        // Assert
        Assert.NotNull(healthProvider);
        Assert.IsType<PlatformApiHealthStatusProviderAdapter>(healthProvider);
    }
}
