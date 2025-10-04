using Bunit;
using Xunit;
using DotNetApp.Client.Pages;
using Microsoft.Extensions.DependencyInjection;
using DotNetApp.Client;

namespace DotNetApp.Client.UnitTests;

public class IndexTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Index_Renders_Welcome()
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

