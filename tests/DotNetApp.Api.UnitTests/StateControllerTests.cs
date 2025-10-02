using System.Threading;
using System.Threading.Tasks;
using DotNetApp.Api.Controllers;
using DotNetApp.Core.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DotNetApp.Api.UnitTests;

// Pure unit tests for StateController (no WebApplicationFactory) to follow MS guidance of fast, isolated unit tests.
public class StateControllerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Health_Returns_Ok_With_Status()
    {
        // Arrange
        var mockHealth = new Mock<IHealthService>();
        mockHealth.Setup(h => h.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("healthy");
        var sut = new StateController(mockHealth.Object);

        // Act
        var result = await sut.Health(CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
        ok.Value!.ToString().Should().Contain("healthy");
        mockHealth.Verify(h => h.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

