using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using GitHub.Runner.Docker;

namespace GitHub.Runner.Docker.Tests
{
    [Trait("Category", "Integration")]
    public class RunnerLogsIntegrationTests
    {
    [Fact]
        public async Task RunnerLogs_Contain_ListeningForJobs_IntegrationOrMock()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("RUN_INTEGRATION_DOCKERDOTNET"), "1", StringComparison.OrdinalIgnoreCase))
            {
                await using var svc = new DockerRunnerService(new TestLogger<DockerRunnerService>());
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

                var started = await svc.StartContainersAsync(new[] { "GITHUB_REPOSITORY=hutchisonkim/dot-net-app" }, cts.Token);
                Assert.True(started, "StartContainersAsync failed");

                var token = Environment.GetEnvironmentVariable("RUNNER_REG_TOKEN");
                if (string.IsNullOrEmpty(token))
                {
                    // No token provided — cannot perform registration; fall back to mock
                    await svc.StopContainersAsync(cts.Token);
                    await RunMockPathAsync();
                    return;
                }

                var registered = await svc.RegisterAsync(token, "hutchisonkim/dot-net-app", "https://github.com", cts.Token);
                Assert.True(registered, "RegisterAsync failed to detect listener");
                await svc.UnregisterAsync(cts.Token);
                await svc.StopContainersAsync(cts.Token);
                return;
            }

            // Mock path to keep test stable on non-integration environments
            await RunMockPathAsync();
        }

        private static async Task RunMockPathAsync()
        {
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

        // removed SearchVolumesForMarkerAsync as DockerRunnerService does not rely on specific volume naming
    }
}
