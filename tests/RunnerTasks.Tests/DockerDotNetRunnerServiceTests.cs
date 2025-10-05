using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RunnerTasks.Tests
{
    public class DockerDotNetRunnerServiceTests
    {
        [Fact]
        public async Task DockerDotNetRunnerService_GatedIntegrationOrMock_Works()
        {
            // Only run the real Docker.DotNet integration when explicitly enabled.
            if (string.Equals(Environment.GetEnvironmentVariable("RUN_INTEGRATION_DOCKERDOTNET"), "1", StringComparison.OrdinalIgnoreCase))
            {
                var workingDir = System.IO.Path.GetFullPath("github-self-hosted-runner-docker");
                var svc = new DockerDotNetRunnerService(workingDir, new TestLogger<DockerDotNetRunnerService>());

                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

                var started = await svc.StartContainersAsync(new[] { "GITHUB_REPOSITORY=hutchisonkim/dot-net-app" }, cts.Token);
                Assert.True(started, "DockerDotNet StartContainersAsync failed");

                var regToken = Environment.GetEnvironmentVariable("RUNNER_REG_TOKEN");
                if (!string.IsNullOrEmpty(regToken))
                {
                    var registered = await svc.RegisterAsync(regToken, "hutchisonkim/dot-net-app", "https://github.com", cts.Token);
                    Assert.True(registered, "DockerDotNet registration container failed");
                }

                var stopped = await svc.StopContainersAsync(cts.Token);
                Assert.True(stopped, "DockerDotNet StopContainersAsync failed");
            }
            else
            {
                // Mock path: ensure orchestration works using fake service
                var fake = new FakeRunnerService(new[] { true });
                var manager = new RunnerManager(fake, new TestLogger<RunnerManager>());

                var env = new[] { "GITHUB_REPOSITORY=hutchisonkim/dot-net-app" };
                var ok = await manager.OrchestrateStartAsync("token", "hutchisonkim/dot-net-app", "https://github.com", env, maxRetries: 1, baseDelayMs: 1);
                Assert.True(ok);
                Assert.Equal(1, fake.RegisterCallCount);
                Assert.Equal(1, fake.StartCallCount);

                var stopped = await manager.OrchestrateStopAsync();
                Assert.True(stopped);
                Assert.Equal(1, fake.StopCallCount);
            }
        }
    }
}
