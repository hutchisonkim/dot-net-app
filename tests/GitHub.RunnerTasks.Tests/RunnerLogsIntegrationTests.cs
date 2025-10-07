using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using GitHub.RunnerTasks;

namespace GitHub.RunnerTasks.Tests
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

        private static async Task<bool> SearchVolumesForMarkerAsync(string volumePrefix, string marker, TimeSpan timeout)
        {
            var dockerUri = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? new Uri("npipe://./pipe/docker_engine")
                : new Uri("unix:///var/run/docker.sock");

            using var client = new Docker.DotNet.DockerClientConfiguration(dockerUri).CreateClient();

            // Docker.DotNet v3.x exposes ListAsync with a single parameter; it returns a VolumesListResponse
            // Docker.DotNet's Volumes.ListAsync in this package version takes a CancellationToken
            var volsList = await client.Volumes.ListAsync(CancellationToken.None).ConfigureAwait(false);
            var matches = (volsList.Volumes ?? Array.Empty<Docker.DotNet.Models.VolumeResponse>()).Where(v => v.Name != null && v.Name.StartsWith(volumePrefix, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length == 0) return false;

            foreach (var vol in matches)
            {
                var containerName = $"runner-logs-scan-{Guid.NewGuid():N}";
                var createParams = new Docker.DotNet.Models.CreateContainerParameters
                {
                    Image = "alpine:latest",
                    Name = containerName,
                    Cmd = new[] { "sh", "-c", $"grep -I -R \"{marker}\" /data/_diag || true" },
                    HostConfig = new Docker.DotNet.Models.HostConfig
                    {
                        AutoRemove = true,
                        Mounts = new System.Collections.Generic.List<Docker.DotNet.Models.Mount>
                        {
                            new Docker.DotNet.Models.Mount { Type = "volume", Source = vol.Name, Target = "/data" }
                        }
                    }
                };

                try
                {
                    // Ensure image exists (pull if necessary)
                    try
                    {
                        await client.Images.CreateImageAsync(new Docker.DotNet.Models.ImagesCreateParameters { FromImage = "alpine", Tag = "latest" }, null, new Progress<Docker.DotNet.Models.JSONMessage>());
                    }
                    catch { /* ignore pull failures - image may exist locally */ }

                    var created = await client.Containers.CreateContainerAsync(createParams).ConfigureAwait(false);
                    var started = await client.Containers.StartContainerAsync(created.ID, new Docker.DotNet.Models.ContainerStartParameters()).ConfigureAwait(false);
                    if (!started)
                    {
                        try { await client.Containers.RemoveContainerAsync(created.ID, new Docker.DotNet.Models.ContainerRemoveParameters { Force = true }).ConfigureAwait(false); } catch { }
                        continue;
                    }

                    // Attach and stream logs
                    using var stream = await client.Containers.GetContainerLogsAsync(created.ID, new Docker.DotNet.Models.ContainerLogsParameters { ShowStdout = true, ShowStderr = true, Follow = false, Tail = "all" });
                    var buffer = new byte[4096];
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var output = new System.Text.StringBuilder();
                    while (sw.Elapsed < timeout)
                    {
                        var type = stream.GetType();
                        var hasReadOutput = type.GetMethod("ReadOutputAsync", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(CancellationToken) }) != null;
                        if (hasReadOutput)
                        {
                            dynamic d = stream;
                            var res = await d.ReadOutputAsync(buffer, 0, buffer.Length, CancellationToken.None);
                            int count = 0; bool eof = false;
                            try { count = (int)res.Count; } catch { try { count = (int)((long)res.Count); } catch { } }
                            try { eof = (bool)res.EOF; } catch { }
                            if (count > 0)
                            {
                                output.Append(System.Text.Encoding.UTF8.GetString(buffer, 0, count));
                            }
                            if (eof) break;
                        }
                        else
                        {
                            var read = await stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None).ConfigureAwait(false);
                            if (read > 0) output.Append(System.Text.Encoding.UTF8.GetString(buffer, 0, read));
                            else break;
                        }
                        await Task.Delay(50).ConfigureAwait(false);
                    }

                    var outStr = output.ToString();
                    try { await client.Containers.RemoveContainerAsync(created.ID, new Docker.DotNet.Models.ContainerRemoveParameters { Force = true }).ConfigureAwait(false); } catch { }
                    if (!string.IsNullOrEmpty(outStr) && outStr.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }
                catch
                {
                    // continue to next volume
                }
            }

            return false;
        }
    }
}
