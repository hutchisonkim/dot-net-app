using System.Threading;
using System.Threading.Tasks;
using DotNetApp.Api.Controllers;
using DotNetApp.Core.Abstractions;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Moq;

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
    var ok = Assert.IsType<OkObjectResult>(result);
    Assert.NotNull(ok.Value);

    // Cast the returned value to the DTO and assert explicitly on the Status property
    var dto = ok.Value as DotNetApp.Api.Contracts.HealthDto;
    Assert.NotNull(dto);
    Assert.Equal("Healthy", dto!.Status);

        mockHealth.Verify(h => h.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

