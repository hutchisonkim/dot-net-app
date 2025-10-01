using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ExampleClient.UnitTests;

public class ExamplePageTests
{
    [Fact]
    public async Task ExamplePage_ShowsStatusFromIHealthStatusProvider()
    {
        using var ctx = new TestContext();

        var mockProvider = new Mock<DotNetApp.Core.Abstractions.IHealthStatusProvider>();
        mockProvider.Setup(p => p.FetchStatusAsync(default)).ReturnsAsync("UnitTest: Healthy");

        ctx.Services.AddSingleton(mockProvider.Object);

    var cut = ctx.RenderComponent<ExampleClient.Pages.Example>();

    // Allow component to initialize (component will call the mock provider)
    await Task.Delay(10);

    Assert.Contains("UnitTest: Healthy", cut.Markup);
    }
}
