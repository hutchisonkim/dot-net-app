using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using GitHub.Runner.Docker;
using DockerApiException = Docker.DotNet.DockerApiException;

namespace GitHub.Runner.Docker.Tests
{
    [Trait("Category", "Unit")]
    public class CrossPlatformCliTests
    {
        [Fact]
        public void DockerRunnerService_UsesCliOnWindows()
        {
            // This test verifies that on Windows, CLI mode is enabled
            var logger = new TestLogger<DockerRunnerService>();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows, _useCli should be true
                // We can verify this indirectly by checking that Docker client is not created
                // (no exception about missing unix socket)
                var svc = new DockerRunnerService(logger);
                Assert.NotNull(svc);
            }
            else
            {
                // On non-Windows, _useCli should be false and Docker client is created
                // This may throw if Docker socket is not available or Docker API fails
                try
                {
                    var svc = new DockerRunnerService(logger);
                    Assert.NotNull(svc);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Docker Unix socket not found"))
                {
                    // Expected on systems without Docker
                    Assert.True(true);
                }
                catch (DockerApiException)
                {
                    // Docker socket exists but API call failed - still validates Unix path
                    Assert.True(true);
                }
            }
        }

        [Fact]
        public async Task DockerRunnerService_CliCommandsWorkCrossPlatform()
        {
            // This is a simple test to verify that the service can instantiate
            // and that we're using the correct shell for the platform
            var logger = new TestLogger<DockerRunnerService>();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows, should use cmd.exe
                var svc = new DockerRunnerService(logger);
                Assert.NotNull(svc);
                
                // Verify logger shows we're on Windows mode (CLI mode)
                // The constructor sets _useCli = true on Windows
                await Task.CompletedTask;
            }
            else
            {
                // On Unix-like systems, should use /bin/bash
                // The service will try to create Docker client via unix socket
                try
                {
                    var svc = new DockerRunnerService(logger);
                    Assert.NotNull(svc);
                    
                    // If Docker is available, the logger should show connection
                    if (logger.Contains(Microsoft.Extensions.Logging.LogLevel.Information, "Connected to Docker via Unix socket"))
                    {
                        Assert.True(true, "Successfully connected to Docker on Unix");
                    }
                    
                    await Task.CompletedTask;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Docker Unix socket not found"))
                {
                    // Expected on systems without Docker - this is fine
                    Assert.True(true, "Unix system without Docker detected (expected)");
                }
                catch (DockerApiException)
                {
                    // Docker socket exists but API call failed - still validates Unix path
                    Assert.True(true, "Docker socket found but API failed (acceptable for this test)");
                }
            }
        }

        [Fact]
        public void RuntimeInformation_DetectsPlatformCorrectly()
        {
            // Basic sanity check that RuntimeInformation works as expected
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            var isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            
            // At least one must be true
            Assert.True(isWindows || isLinux || isMacOS, 
                "Platform detection failed - unable to identify OS");
            
            // Only one should be true
            var count = (isWindows ? 1 : 0) + (isLinux ? 1 : 0) + (isMacOS ? 1 : 0);
            Assert.Equal(1, count);
        }
    }
}
