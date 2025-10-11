using System.Threading;
using System.Threading.Tasks;
using DotNetApp.Server.Controllers;
using DotNetApp.Core.Abstractions;
using DotNetApp.Core.Models;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DotNetApp.Server.Tests.Unit;

[Trait("Category", "Unit")]
public class StateControllerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Health_WhenCalled_ReturnsOkWithStatus()
    {
        // Arrange
        var mockHealth = new Mock<IHealthService>();
        mockHealth.Setup(h => h.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(HealthStatus.Healthy.Status);
        var sut = new StateController(mockHealth.Object);

        // Act
        var result = await sut.Health(CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        // Cast the returned value to the DTO and assert explicitly on the Status property
        var dto = ok.Value as DotNetApp.Server.Contracts.HealthDto;
        Assert.NotNull(dto);
        Assert.Equal(HealthStatus.Healthy.Status, dto!.Status);

        mockHealth.Verify(h => h.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

