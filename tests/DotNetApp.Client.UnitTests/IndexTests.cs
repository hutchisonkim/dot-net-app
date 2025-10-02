using Bunit;
using Xunit;
using DotNetApp.Client.Pages;
using Microsoft.Extensions.DependencyInjection;
using DotNetApp.Client.Shared;
using DotNetApp.Core.Abstractions;
using FluentAssertions;

namespace DotNetApp.Client.UnitTests;

public class IndexTests
{
    [Fact]
    [Trait("Category","Unit")]
    public void Index_Renders_Welcome()
    {
        // Arrange
        using var ctx = new TestContext();
        var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"idle\" }", System.Net.HttpStatusCode.OK);
        ctx.Services.AddPlatformApi(handler, "http://localhost/");

        // Act
        var cut = ctx.RenderComponent<Index>();

        // Assert
        cut.Markup.Should().Contain("<h1>DotNetApp</h1>");
    }
}

