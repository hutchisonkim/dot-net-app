using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ExampleClient.UnitTests;

public class ExamplePageTests
{
    [Fact]
    [Trait("Category","Unit")]
    public void ExamplePage_ShowsStatusFromIHealthStatusProvider()
    {
        // Arrange
        using var ctx = new TestContext();
        var mockProvider = new Mock<DotNetApp.Core.Abstractions.IHealthStatusProvider>();
        mockProvider.Setup(p => p.FetchStatusAsync(default)).ReturnsAsync("UnitTest: Healthy");
        ctx.Services.AddSingleton(mockProvider.Object);

        // Act
    var cut = ctx.RenderComponent<ExampleClient.Pages.Example>();

    // Assert (wait until async render completes instead of arbitrary delay)
    cut.WaitForAssertion(() => Assert.Contains("UnitTest: Healthy", cut.Markup));
    mockProvider.Verify(p => p.FetchStatusAsync(default), Times.Once);
    }
}
