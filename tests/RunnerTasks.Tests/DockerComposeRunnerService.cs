using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace RunnerTasks.Tests
{
    /// <summary>
    /// Docker.DotNet-based implementation that mirrors the limited docker-compose usage in tests.
    /// It does not attempt to be a full docker-compose replacement — only what our tests need:
    /// - create a named volume 'runner_data' (if missing)
    /// - create/start a container from the compose-built image tag (github-self-hosted-runner-docker-github-runner:latest)
    /// - exec into the running container to run configure/remove scripts
    /// - stop/remove the container
    /// This keeps tests programmatic and removes CLI dependencies.
    /// </summary>
    public class DockerComposeRunnerService : IRunnerService
    {
        private readonly string _workingDirectory;
        private readonly ILogger<DockerComposeRunnerService>? _logger;
        private readonly DockerClient _client;
        private string? _containerId;
        private const string VolumeName = "runner_data";
        private const string ImageName = "github-self-hosted-runner-docker-github-runner:latest";

        public DockerComposeRunnerService(string workingDirectory, ILogger<DockerComposeRunnerService>? logger = null)
        {
            _workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
            _logger = logger;

            var dockerUri = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? new Uri("npipe://./pipe/docker_engine")
                : new Uri("unix:///var/run/docker.sock");
            _client = new DockerClientConfiguration(dockerUri).CreateClient();
        }

        public async Task<bool> RegisterAsync(string token, string ownerRepo, string githubUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_containerId))
            {
                _logger?.LogError("No running container to exec into for registration");
                return false;
            }

            try
            {
                var runnerUrl = (githubUrl ?? "").TrimEnd('/') + "/" + ownerRepo;
                var runnerName = "runner-" + Guid.NewGuid().ToString("N").Substring(0, 8);

                var envList = new System.Collections.Generic.List<string>
                {
                    $"GITHUB_TOKEN={token}",
                    $"GITHUB_URL={githubUrl}",
                    $"GITHUB_REPOSITORY={ownerRepo}"
                };

                var configArgs = new[]
                {
                    "/actions-runner/config.sh",
                    "--url",
                    runnerUrl,
                    "--token",
                    token,
                    "--name",
                    runnerName,
                    "--labels",
                    "",
                    "--work",
                    "_work",
                    "--ephemeral"
                };

                var execCreate = await _client.Containers.ExecCreateContainerAsync(_containerId, new ContainerExecCreateParameters
                {
                    AttachStdout = true,
                    AttachStderr = true,
                    User = "github-runner",
                    Cmd = configArgs,
                    Env = envList
                }, cancellationToken).ConfigureAwait(false);

                using (var stream = await _client.Containers.StartAndAttachContainerExecAsync(execCreate.ID, false, cancellationToken).ConfigureAwait(false))
                {
                    var buffer = new byte[1024];
                    try
                    {
                        while (true)
                        {
                            var res = await ((dynamic)stream).ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
                            if ((bool)res.EOF) break;
                            var count = (int)res.Count;
                            if (count > 0)
                            {
                                var s = System.Text.Encoding.UTF8.GetString(buffer, 0, count);
                                _logger?.LogInformation("[configure exec] {Line}", s.TrimEnd());
                            }
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { _logger?.LogDebug(ex, "Error streaming configure exec output"); }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Register via Docker.DotNet failed");
                return false;
            }
        }

        public async Task<bool> StartContainersAsync(string[] envVars, CancellationToken cancellationToken)
        {
            try
            {
                // Ensure volume exists
                try
                {
                    await _client.Volumes.CreateAsync(new VolumesCreateParameters { Name = VolumeName }, cancellationToken).ConfigureAwait(false);
                }
                catch { }

                // Ensure image exists; if missing, try to pull
                try
                {
                    var images = await _client.Images.ListImagesAsync(new ImagesListParameters { All = true }, cancellationToken).ConfigureAwait(false);
                    var found = images.FirstOrDefault(i => (i.RepoTags ?? Array.Empty<string>()).Any(t => string.Equals(t, ImageName, StringComparison.OrdinalIgnoreCase)));
                    if (found == null)
                    {
                        // Try pull
                        try
                        {
                            await _client.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = "github-self-hosted-runner-docker-github-runner", Tag = "latest" }, null, new Progress<JSONMessage>(), cancellationToken).ConfigureAwait(false);
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Error ensuring image exists");
                }

                // Create the container similar to docker-compose service
                var createParams = new CreateContainerParameters
                {
                    Image = ImageName,
                    Name = $"runner-compose-{Guid.NewGuid():N}",
                    Cmd = new[] { "tail", "-f", "/dev/null" },
                    Env = envVars?.ToList() ?? new System.Collections.Generic.List<string>(),
                    HostConfig = new HostConfig
                    {
                        Mounts = new System.Collections.Generic.List<Mount>
                        {
                            new Mount { Type = "volume", Source = VolumeName, Target = "/runner" },
                            new Mount { Type = "volume", Source = VolumeName, Target = "/actions-runner" }
                        }
                    }
                };

                var created = await _client.Containers.CreateContainerAsync(createParams, cancellationToken).ConfigureAwait(false);
                _containerId = created.ID;
                var started = await _client.Containers.StartContainerAsync(_containerId, new ContainerStartParameters(), cancellationToken).ConfigureAwait(false);
                if (!started)
                {
                    try { await _client.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters { Force = true }, cancellationToken).ConfigureAwait(false); } catch { }
                    _containerId = null;
                    return false;
                }

                // Wait briefly for running state
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.Elapsed < TimeSpan.FromSeconds(10))
                {
                    var inspect = await _client.Containers.InspectContainerAsync(_containerId, cancellationToken).ConfigureAwait(false);
                    if (inspect.State != null && inspect.State.Running) break;
                    await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "StartContainers via Docker.DotNet failed");
                return false;
            }
        }

        public async Task<bool> StopContainersAsync(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(_containerId))
            {
                try
                {
                    await _client.Containers.StopContainerAsync(_containerId, new ContainerStopParameters { WaitBeforeKillSeconds = 5 }, cancellationToken).ConfigureAwait(false);
                }
                catch { }

                try { await _client.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters { Force = true }, cancellationToken).ConfigureAwait(false); } catch { }
                _containerId = null;
            }

            try
            {
                // Do not remove well-known volume here; keep data for diagnostics/tests
            }
            catch { }

            return true;
        }

        public async Task<bool> UnregisterAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_containerId)) return true;

            try
            {
                var exec = await _client.Containers.ExecCreateContainerAsync(_containerId, new ContainerExecCreateParameters
                {
                    AttachStdout = true,
                    AttachStderr = true,
                    User = "github-runner",
                    Cmd = new[] { "/actions-runner/config.sh", "remove", "--unattended" }
                }, cancellationToken).ConfigureAwait(false);

                using var streamObj = await _client.Containers.StartAndAttachContainerExecAsync(exec.ID, false, cancellationToken).ConfigureAwait(false);
                var buffer = new byte[1024];
                try
                {
                    while (true)
                    {
                        var res = await ((dynamic)streamObj).ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
                        if ((bool)res.EOF) break;
                        var count = (int)res.Count;
                        if (count > 0)
                        {
                            var s = System.Text.Encoding.UTF8.GetString(buffer, 0, count);
                            _logger?.LogInformation("[unregister exec] {Line}", s.TrimEnd());
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { _logger?.LogWarning(ex, "Error while streaming unregister exec output"); }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to run unregister exec");
            }

            return true;
        }
    }
}
