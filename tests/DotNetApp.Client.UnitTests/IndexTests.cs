using Bunit;
using Xunit;
using DotNetApp.Client.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetApp.Client.UnitTests;

public class IndexTests
{
    [Fact]
    public void Index_Renders_Welcome()
    {
    using var ctx = new BunitContext();

    // Provide a minimal HttpClient and ApiClient for DI so Index can be rendered
    var handler = new DotNetApp.Client.Tests.TestHttpMessageHandler("{ \"status\": \"idle\" }", System.Net.HttpStatusCode.OK);
    var client = new System.Net.Http.HttpClient(handler) { BaseAddress = new System.Uri("http://localhost/") };
    ctx.Services.AddSingleton<System.Net.Http.HttpClient>(client);
    var inMemory = new System.Collections.Generic.Dictionary<string, string?> { ["ApiBaseAddress"] = "http://localhost/" };
    ctx.Services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(new DotNetApp.Client.Tests.SimpleConfiguration(inMemory));
    ctx.Services.AddSingleton<DotNetApp.Client.Services.ApiClient>();

    var cut = ctx.Render<Index>();
        Assert.Contains("DotNetApp Blazor Index Page", cut.Markup);
    }
}

