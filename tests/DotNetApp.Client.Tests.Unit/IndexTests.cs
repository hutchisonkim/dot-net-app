using Bunit;
using Xunit;
using DotNetApp.Client.Pages;
using Microsoft.Extensions.DependencyInjection;
using DotNetApp.Client;
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
    public async Task Index_AfterLoad_WithHealthyStatus_ShowsSuccessMessage()
    {
        // Arrange
        using var ctx = new TestContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"healthy\" }", System.Net.HttpStatusCode.OK);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var cut = ctx.RenderComponent<Index>();
        await Task.Delay(100); // Allow OnInitializedAsync to complete
        cut.WaitForState(() => !cut.Markup.Contains("Checking API"));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("healthy", markup);
        Assert.Contains("text-success", markup);
        Assert.DoesNotContain("Checking API", markup);
    }

    [Fact]
    public async Task Index_AfterLoad_WithNullStatus_ShowsUnavailableMessage()
    {
        // Arrange
        using var ctx = new TestContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("null", System.Net.HttpStatusCode.OK);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var cut = ctx.RenderComponent<Index>();
        await Task.Delay(100); // Allow OnInitializedAsync to complete
        cut.WaitForState(() => !cut.Markup.Contains("Checking API"));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Unavailable", markup);
        Assert.Contains("text-warning", markup);
        Assert.Contains("could not reach API", markup);
    }

    [Fact]
    public async Task Index_AfterLoad_WithDifferentStatus_DisplaysStatus()
    {
        // Arrange
        using var ctx = new TestContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"degraded\" }", System.Net.HttpStatusCode.OK);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var cut = ctx.RenderComponent<Index>();
        await Task.Delay(100); // Allow OnInitializedAsync to complete
        cut.WaitForState(() => !cut.Markup.Contains("Checking API"));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("degraded", markup);
        Assert.Contains("text-success", markup);
    }
}

