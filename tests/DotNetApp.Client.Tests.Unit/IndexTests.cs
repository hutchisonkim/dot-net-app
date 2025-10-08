using Bunit;
using Xunit;
using DotNetApp.Client.Pages;
using Microsoft.Extensions.DependencyInjection;
using DotNetApp.Client;

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
}

