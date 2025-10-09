using System.Threading;
using System.Threading.Tasks;
using DotNetApp.Core.Models;
using DotNetApp.Server.Services;
using Xunit;

namespace DotNetApp.Server.Tests.Unit;

[Trait("Category", "Unit")]
public class DefaultHealthServiceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStatusAsync_ReturnsHealthyStatus()
    {
        // Arrange
        var sut = new DefaultHealthService();

        // Act
        var result = await sut.GetStatusAsync(CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Healthy.Status, result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStatusAsync_WithCancellationToken_ReturnsHealthyStatus()
    {
        // Arrange
        var sut = new DefaultHealthService();
        using var cts = new CancellationTokenSource();

        // Act
        var result = await sut.GetStatusAsync(cts.Token);

        // Assert
        Assert.Equal(HealthStatus.Healthy.Status, result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStatusAsync_ReturnsHealthyWithCapitalH()
    {
        // Arrange
        var sut = new DefaultHealthService();

        // Act
        var result = await sut.GetStatusAsync();

        // Assert - Verify exact casing: "Healthy" not "healthy"
        Assert.Equal("Healthy", result);
    }
}
