using DotNetApp.Core.Models;
using DotNetApp.Server.Contracts;
using Xunit;

namespace DotNetApp.Server.Tests.Unit;

[Trait("Category", "Unit")]
public class HealthStatusTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Healthy_HasExpectedStatus()
    {
        // Act
        var status = HealthStatus.Healthy.Status;

        // Assert
        Assert.Equal("Healthy", status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Healthy_IsNotNull()
    {
        // Act
        var healthy = HealthStatus.Healthy;

        // Assert
        Assert.NotNull(healthy);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void HealthStatusValue_CreatesCorrectly()
    {
        // Act
        var status = new HealthStatus.HealthStatusValue("TestStatus");

        // Assert
        Assert.Equal("TestStatus", status.Status);
    }
}

[Trait("Category", "Unit")]
public class HealthDtoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Status_CanBeSet()
    {
        // Arrange
        var dto = new HealthDto();

        // Act
        dto.Status = "TestStatus";

        // Assert
        Assert.Equal("TestStatus", dto.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Status_DefaultsToNull()
    {
        // Arrange & Act
        var dto = new HealthDto();

        // Assert
        Assert.Null(dto.Status);
    }
}
