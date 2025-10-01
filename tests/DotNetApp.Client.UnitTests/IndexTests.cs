using Bunit;
using Xunit;
using DotNetApp.Client.Pages;
using Microsoft.Extensions.DependencyInjection;
using DotNetApp.Client.Shared;
using DotNetApp.Core.Abstractions;

namespace DotNetApp.Client.UnitTests;

public class IndexTests
{
    [Fact]
    public void Index_Renders_Welcome()
    {
    using var ctx = new BunitContext();

    var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"idle\" }", System.Net.HttpStatusCode.OK);
    ctx.Services.AddPlatformApi(handler, "http://localhost/");

    var cut = ctx.Render<Index>();
    Assert.Contains("<h1>DotNetApp</h1>", cut.Markup);
    }
}

