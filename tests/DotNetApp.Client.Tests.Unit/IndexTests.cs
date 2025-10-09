using Bunit;
using Xunit;
using DotNetApp.Client.Pages;
using Microsoft.Extensions.DependencyInjection;
using DotNetApp.Client;
using DotNetApp.Core.Models;
using System.Threading.Tasks;

namespace DotNetApp.Client.Tests.Unit;

[Trait("Category", "Unit")]
public class IndexTests
{
    [Fact]
    public void Index_WhenRendered_ContainsAppTitle()
    {
        // Arrange
        using var ctx = new TestContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"idle\" }", System.Net.HttpStatusCode.OK);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var cut = ctx.RenderComponent<Index>();

        // Assert (xUnit)
        Assert.Contains("<h1>DotNetApp</h1>", cut.Markup);
    }

    [Fact]
    public void Index_InitialRender_ShowsLoadingMessage()
    {
        // Arrange
        using var ctx = new TestContext();
        // Use a delayed handler to keep the loading state visible
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler($"{{ \"status\": \"{HealthStatus.Healthy.Status}\" }}", System.Net.HttpStatusCode.OK, delayMs: 5000);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var cut = ctx.RenderComponent<Index>();

        // Assert - initially shows loading state
        var markup = cut.Markup;
        Assert.Contains("<em>Checking API...</em>", markup);
        Assert.DoesNotContain(HealthStatus.Healthy.Status, markup);
    }

    [Fact]
    public void Index_AfterLoad_WithHealthyStatus_ShowsSuccessMessage()
    {
        // Arrange
        using var ctx = new TestContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler($"{{ \"status\": \"{HealthStatus.Healthy.Status}\" }}", System.Net.HttpStatusCode.OK);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var cut = ctx.RenderComponent<Index>();
        cut.WaitForState(() => !cut.Markup.Contains("Checking API"));

        // Assert
        var markup = cut.Markup;
        Assert.Contains(HealthStatus.Healthy.Status, markup);
        Assert.Contains("text-success", markup);
        Assert.DoesNotContain("Checking API", markup);
    }

    [Fact]
    public void Index_AfterLoad_WithNullStatus_ShowsUnavailableMessage()
    {
        // Arrange
        using var ctx = new TestContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("null", System.Net.HttpStatusCode.OK);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var cut = ctx.RenderComponent<Index>();
        cut.WaitForState(() => !cut.Markup.Contains("Checking API"));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Unavailable", markup);
        Assert.Contains("text-warning", markup);
        Assert.Contains("could not reach API", markup);
    }

    [Fact]
    public void Index_AfterLoad_WithDifferentStatus_DisplaysStatus()
    {
        // Arrange
        using var ctx = new TestContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"degraded\" }", System.Net.HttpStatusCode.OK);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var cut = ctx.RenderComponent<Index>();
        cut.WaitForState(() => !cut.Markup.Contains("Checking API"));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("degraded", markup);
        Assert.Contains("text-success", markup);
    }
}

