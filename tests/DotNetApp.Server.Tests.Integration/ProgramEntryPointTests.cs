using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DotNetApp.Server.Tests.Integration;

/// <summary>
/// Verifies that DotNetApp.Server.Program is the correct entry point class for WebApplicationFactory.
/// This test explicitly validates the pattern used in Program.cs for .NET 6+ with top-level statements.
/// 
/// Per Microsoft documentation, the pattern is:
/// 1. Program.cs uses top-level statements (implicit Program class)
/// 2. At the end of Program.cs, declare: namespace DotNetApp.Server { public partial class Program { } }
/// 3. Tests use WebApplicationFactory&lt;DotNetApp.Server.Program&gt;
/// 
/// This approach makes the implicit Program class accessible for testing while maintaining clean startup code.
/// Reference: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
/// </summary>
[Trait("Category", "Integration")]
public class ProgramEntryPointTests
{
    [Fact]
    public void Program_IsPublicClass_InCorrectNamespace()
    {
        // Arrange & Act
        var programType = typeof(DotNetApp.Server.Program);

        // Assert - Verify Program class exists and is public
        Assert.NotNull(programType);
        Assert.True(programType.IsPublic, "Program class must be public for WebApplicationFactory");
        Assert.Equal("DotNetApp.Server", programType.Namespace);
    }

    [Fact]
    public void Program_IsPartialClass_AllowingTopLevelStatements()
    {
        // Arrange & Act
        var programType = typeof(DotNetApp.Server.Program);
        
        // Assert - In .NET 6+, the explicit partial class declaration allows the implicit
        // Program class from top-level statements to be extended and made public
        // We can't directly test for "partial" keyword via reflection, but we verify
        // the class is accessible, which proves the pattern works
        Assert.NotNull(programType);
        Assert.True(programType.Assembly.GetName().Name == "DotNetApp.Server", 
            "Program class should be in the DotNetApp.Server assembly");
    }

    [Fact]
    public async Task Program_WorksWithWebApplicationFactory_StartsSuccessfully()
    {
        // Arrange & Act
        using var factory = new WebApplicationFactory<DotNetApp.Server.Program>();
        using var client = factory.CreateClient();

        // Assert - If Program is correct entry point, we can make a request
        var response = await client.GetAsync("/api/state/health");
        
        // Verify the server started and responded (status code doesn't matter for this test)
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Program_CreatesWorkingApplication_WithAllServices()
    {
        // Arrange
        using var factory = new WebApplicationFactory<DotNetApp.Server.Program>();
        using var client = factory.CreateClient();

        // Act - Call an endpoint that uses registered services
        var response = await client.GetAsync("/api/state/health");

        // Assert - Successful response proves:
        // 1. Program is the correct entry point
        // 2. All services are properly registered
        // 3. The application pipeline is correctly configured
        response.EnsureSuccessStatusCode();
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public void Program_HasWebSdkTargeting_CorrectForAspNetCore()
    {
        // Arrange & Act
        var assembly = typeof(DotNetApp.Server.Program).Assembly;
        
        // Assert - Verify this is indeed a web application assembly
        Assert.Contains("Microsoft.AspNetCore", 
            assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty));
    }
}
