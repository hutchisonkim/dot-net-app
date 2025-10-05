using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RunnerTasks.Tests
{
    public class RunnerLogsIntegrationTests
    {
        [Fact]
        public async Task RunnerLogs_Contain_ListeningForJobs_IntegrationOrMock()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("RUN_INTEGRATION_DOCKERDOTNET"), "1", StringComparison.OrdinalIgnoreCase))
            {
                // Probe Docker availability
                try
                {
                    var dockerUri = Environment.OSVersion.Platform == PlatformID.Win32NT
                        ? new Uri("npipe://./pipe/docker_engine")
                        : new Uri("unix:///var/run/docker.sock");
                    using var client = new Docker.DotNet.DockerClientConfiguration(dockerUri).CreateClient();
                    using var ctsPing = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    await client.System.PingAsync(ctsPing.Token);
                }
                catch
                {
                    // Can't reach docker — fall back to mock
                    await RunMockPathAsync();
                    return;
                }

                var workingDir = System.IO.Path.GetFullPath("github-self-hosted-runner-docker");
                var svc = new DockerDotNetRunnerService(workingDir, new TestLogger<DockerDotNetRunnerService>());
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

                // Search docker volumes for runner_data_* and inspect _diag files for the marker
                var found = await SearchVolumesForMarkerAsync("runner_data_", "Listening for Jobs", TimeSpan.FromSeconds(10));

                // Unregister and stop
                await svc.UnregisterAsync(cts.Token);
                await svc.StopContainersAsync(cts.Token);

                Assert.True(found, "Did not find 'Listening for Jobs' in any runner_data_* volume _diag files");
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

        private static Task<bool> SearchVolumesForMarkerAsync(string volumePrefix, string marker, TimeSpan timeout)
        {
            return Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = "volume ls --format \"{{.Name}}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var p = Process.Start(psi)!;
                    var outText = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    var vols = outText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(v => v.StartsWith(volumePrefix, StringComparison.OrdinalIgnoreCase)).ToArray();

                    foreach (var v in vols)
                    {
                        // Run an ephemeral container to grep the _diag files
                        var args = $"run --rm -v {v}:/data alpine sh -c \"grep -I -R \"{marker}\" /data/_diag || true\"";
                        var psi2 = new ProcessStartInfo
                        {
                            FileName = "docker",
                            Arguments = args,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using var p2 = Process.Start(psi2)!;
                        var out2 = p2.StandardOutput.ReadToEnd();
                        p2.WaitForExit((int)timeout.TotalMilliseconds);
                        if (!string.IsNullOrEmpty(out2) && out2.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
