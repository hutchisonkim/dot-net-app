using System.Threading;
using System.Threading.Tasks;
using DotNetApp.Api.Controllers;
using DotNetApp.Core.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DotNetApp.Api.UnitTests;

public class StateControllerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Health_WhenCalled_ReturnsOkWithStatus()
    {
        // Arrange
        var mockHealth = new Mock<IHealthService>();
        mockHealth.Setup(h => h.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Healthy");
        var sut = new StateController(mockHealth.Object);

        // Act
        var result = await sut.Health(CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    // Controller wraps the status in an anonymous object; API uses "Healthy"
    ok.Value!.ToString().Should().Contain("Healthy");
        mockHealth.Verify(h => h.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

