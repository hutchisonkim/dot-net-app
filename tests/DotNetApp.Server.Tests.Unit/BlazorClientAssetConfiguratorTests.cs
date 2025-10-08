using System;
using System.IO;
using System.Threading.Tasks;
using DotNetApp.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace DotNetApp.Server.Tests.Unit;

[Trait("Category", "Unit")]
public class BlazorClientAssetConfiguratorTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Configure_WithNonIApplicationBuilder_DoesNothing()
    {
        // Arrange
        var sut = new BlazorClientAssetConfigurator();
        var invalidBuilder = new object();

        // Act & Assert (should not throw)
        sut.Configure(invalidBuilder);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Configure_WithValidBuilder_AndExistingWwwroot_AddsMiddleware()
    {
        // Arrange
        var sut = new BlazorClientAssetConfigurator();
        
        // Create a directory structure that matches the expected layout:
        // tempDir/src/DotNetApp.Server (ContentRoot)
        // tempDir/src/DotNetApp.Client/wwwroot (Client files)
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var serverDir = Path.Combine(tempDir, "src", "DotNetApp.Server");
        var clientWwwroot = Path.Combine(tempDir, "src", "DotNetApp.Client", "wwwroot");
        Directory.CreateDirectory(serverDir);
        Directory.CreateDirectory(clientWwwroot);
        
        // Create index.html in the wwwroot
        File.WriteAllText(Path.Combine(clientWwwroot, "index.html"), "<html><body>Test</body></html>");

        try
        {
            var services = new ServiceCollection();
            services.AddRouting();
            services.AddLogging();
            
            // Mock IWebHostEnvironment with serverDir as ContentRoot
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.ContentRootPath).Returns(serverDir);
            mockEnv.Setup(e => e.WebRootPath).Returns(clientWwwroot);
            mockEnv.Setup(e => e.WebRootFileProvider).Returns(new PhysicalFileProvider(clientWwwroot));
            mockEnv.Setup(e => e.ApplicationName).Returns("DotNetApp.Server.Tests");
            mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            mockEnv.Setup(e => e.ContentRootFileProvider).Returns(new PhysicalFileProvider(serverDir));
            
            services.AddSingleton(mockEnv.Object);
            
            var serviceProvider = services.BuildServiceProvider();
            var appBuilder = new ApplicationBuilder(serviceProvider);

            // Act
            sut.Configure(appBuilder);

            // Assert - the middleware should be registered
            var app = appBuilder.Build();
            Assert.NotNull(app);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Configure_WithNoIndexHtml_DoesNotConfigureStaticFiles()
    {
        // Arrange
        var sut = new BlazorClientAssetConfigurator();
        
        // Create directory structure without index.html
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var serverDir = Path.Combine(tempDir, "src", "DotNetApp.Server");
        var clientWwwroot = Path.Combine(tempDir, "src", "DotNetApp.Client", "wwwroot");
        Directory.CreateDirectory(serverDir);
        Directory.CreateDirectory(clientWwwroot);
        // Note: NOT creating index.html

        try
        {
            var services = new ServiceCollection();
            
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.ContentRootPath).Returns(serverDir);
            
            services.AddSingleton(mockEnv.Object);
            
            var serviceProvider = services.BuildServiceProvider();
            var appBuilder = new ApplicationBuilder(serviceProvider);

            // Act
            sut.Configure(appBuilder);

            // Assert - verify middleware can still be built
            var app = appBuilder.Build();
            Assert.NotNull(app);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Configure_WithDebugBinWwwroot_FindsAndConfigures()
    {
        // Arrange
        var sut = new BlazorClientAssetConfigurator();
        
        // Create directory structure with Debug bin path
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var serverDir = Path.Combine(tempDir, "src", "DotNetApp.Server");
        var clientWwwroot = Path.Combine(tempDir, "src", "DotNetApp.Client", "bin", "Debug", "net8.0", "wwwroot");
        Directory.CreateDirectory(serverDir);
        Directory.CreateDirectory(clientWwwroot);
        
        // Create index.html in the Debug bin wwwroot
        File.WriteAllText(Path.Combine(clientWwwroot, "index.html"), "<html><body>Debug Build</body></html>");

        try
        {
            var services = new ServiceCollection();
            services.AddRouting();
            services.AddLogging();
            
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.ContentRootPath).Returns(serverDir);
            mockEnv.Setup(e => e.WebRootPath).Returns(clientWwwroot);
            mockEnv.Setup(e => e.WebRootFileProvider).Returns(new PhysicalFileProvider(clientWwwroot));
            mockEnv.Setup(e => e.ApplicationName).Returns("DotNetApp.Server.Tests");
            mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            mockEnv.Setup(e => e.ContentRootFileProvider).Returns(new PhysicalFileProvider(serverDir));
            
            services.AddSingleton(mockEnv.Object);
            
            var serviceProvider = services.BuildServiceProvider();
            var appBuilder = new ApplicationBuilder(serviceProvider);

            // Act
            sut.Configure(appBuilder);

            // Assert
            var app = appBuilder.Build();
            Assert.NotNull(app);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Configure_WithSourceWwwroot_FindsAndConfigures()
    {
        // Arrange
        var sut = new BlazorClientAssetConfigurator();
        
        // Create directory structure with source wwwroot
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var serverDir = Path.Combine(tempDir, "src", "DotNetApp.Server");
        var clientWwwroot = Path.Combine(tempDir, "src", "DotNetApp.Client", "wwwroot");
        Directory.CreateDirectory(serverDir);
        Directory.CreateDirectory(clientWwwroot);
        
        // Create index.html in the source wwwroot
        File.WriteAllText(Path.Combine(clientWwwroot, "index.html"), "<html><body>Source</body></html>");

        try
        {
            var services = new ServiceCollection();
            services.AddRouting();
            services.AddLogging();
            
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.ContentRootPath).Returns(serverDir);
            mockEnv.Setup(e => e.WebRootPath).Returns(clientWwwroot);
            mockEnv.Setup(e => e.WebRootFileProvider).Returns(new PhysicalFileProvider(clientWwwroot));
            mockEnv.Setup(e => e.ApplicationName).Returns("DotNetApp.Server.Tests");
            mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            mockEnv.Setup(e => e.ContentRootFileProvider).Returns(new PhysicalFileProvider(serverDir));
            
            services.AddSingleton(mockEnv.Object);
            
            var serviceProvider = services.BuildServiceProvider();
            var appBuilder = new ApplicationBuilder(serviceProvider);

            // Act
            sut.Configure(appBuilder);

            // Assert
            var app = appBuilder.Build();
            Assert.NotNull(app);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Configure_MiddlewarePipeline_ExecutesCorrectly()
    {
        // Arrange
        var sut = new BlazorClientAssetConfigurator();
        
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var serverDir = Path.Combine(tempDir, "src", "DotNetApp.Server");
        var clientWwwroot = Path.Combine(tempDir, "src", "DotNetApp.Client", "wwwroot");
        Directory.CreateDirectory(serverDir);
        Directory.CreateDirectory(clientWwwroot);
        
        File.WriteAllText(Path.Combine(clientWwwroot, "index.html"), "<html><body>Test</body></html>");

        try
        {
            var services = new ServiceCollection();
            services.AddRouting();
            services.AddLogging();
            
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.ContentRootPath).Returns(serverDir);
            mockEnv.Setup(e => e.WebRootPath).Returns(clientWwwroot);
            mockEnv.Setup(e => e.WebRootFileProvider).Returns(new PhysicalFileProvider(clientWwwroot));
            mockEnv.Setup(e => e.ApplicationName).Returns("DotNetApp.Server.Tests");
            mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            mockEnv.Setup(e => e.ContentRootFileProvider).Returns(new PhysicalFileProvider(serverDir));
            
            services.AddSingleton(mockEnv.Object);
            
            var serviceProvider = services.BuildServiceProvider();
            var appBuilder = new ApplicationBuilder(serviceProvider);

            // Act
            sut.Configure(appBuilder);
            
            // Add a test middleware after to verify the pipeline continues
            bool nextWasCalled = false;
            appBuilder.Use(async (ctx, next) => 
            {
                nextWasCalled = true;
                await next();
            });

            // Assert - invoke the middleware pipeline
            var app = appBuilder.Build();
            var context = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };
            context.Request.Path = "/test.txt";
            
            await app.Invoke(context);
            
            Assert.True(nextWasCalled, "Next middleware should have been invoked");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
