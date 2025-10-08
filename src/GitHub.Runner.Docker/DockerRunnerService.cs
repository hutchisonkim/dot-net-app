using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitHub.Runner.Docker
{
    public class DockerRunnerService : IRunnerService, IAsyncDisposable
    {
        private readonly DockerClient? _docker;
        private readonly ILogger<DockerRunnerService>? _logger;
        private readonly bool _useCli;

        private const string ImageTag = "github-runner:latest";
        private string? _repoUrl;
        private string? _token;
        private string? _runnerName;
        private string? _containerName;

        public DockerRunnerService(ILogger<DockerRunnerService>? logger = null)
        {
            _logger = logger;
            _useCli = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (!_useCli)
            {
                _docker = CreateDockerClient();
            }
        }

        // Instead of persisting state on disk in the repo, discover any existing
        // runner container dynamically when stop/unregister are invoked from a
        // fresh process. This avoids writing tokens or transient info into source.

        private DockerClient CreateDockerClient()
        {
            var socket = "unix:///var/run/docker.sock";

            if (!File.Exists("/var/run/docker.sock"))
                throw new InvalidOperationException("Docker Unix socket not found. Ensure Docker is running.");

            var client = new DockerClientConfiguration(new Uri(socket)).CreateClient();

            // quick probe
            client.Images.ListImagesAsync(new ImagesListParameters { All = true }).GetAwaiter().GetResult();
            _logger?.LogInformation("Connected to Docker via Unix socket {Socket}", socket);
            return client;
        }

        public async Task<bool> RegisterAsync(string token, string ownerRepo, string githubUrl, CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Preparing registration for {Repo}", ownerRepo);

            // Use the full repository URL (https://github.com/{owner}/{repo}) so the runner registers at the correct API path
            _repoUrl = string.IsNullOrEmpty(ownerRepo)
                ? githubUrl.TrimEnd('/')
                : (githubUrl.TrimEnd('/') + "/" + ownerRepo);
            _token = token;
            _runnerName = $"github-runner-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 30);
            _containerName = _runnerName;

            if (!await ImageExistsAsync(cancellationToken))
            {
                _logger?.LogInformation("Docker image {ImageTag} not found; building new image", ImageTag);
                await BuildImageAsync(cancellationToken);
            }

            _logger?.LogInformation("Runner {RunnerName} prepared for registration", _runnerName);
            return true;
        }

        // High level convenience: start full runner lifecycle (register + start container)
        public async Task<bool> StartAsync(string token, string ownerRepo, string githubUrl, string[] envVars, CancellationToken cancellationToken)
        {
            SetRegistrationInfo(token, ownerRepo, githubUrl);
            var registered = await RegisterAsync(token, ownerRepo, githubUrl, cancellationToken).ConfigureAwait(false);
            if (!registered) return false;
            return await StartContainersAsync(envVars, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> StartContainersAsync(string[] envVars, CancellationToken cancellationToken)
        {
            if (_repoUrl == null || _token == null || _runnerName == null)
            {
                _logger?.LogError("Missing registration details; call RegisterAsync first");
                return false;
            }

            _logger?.LogInformation("Starting runner container {ContainerName}", _containerName);
            await RemoveContainerIfExistsAsync(_containerName!, cancellationToken);

            if (_useCli)
            {
                var mergedEnv = envVars.Select(e => $"--env {e}").ToList();
                mergedEnv.Add($"--env RUNNER_REPO_URL={_repoUrl}");
                mergedEnv.Add($"--env RUNNER_TOKEN={_token}");
                mergedEnv.Add($"--env RUNNER_NAME={_runnerName}");

                string envArgs = string.Join(" ", mergedEnv);
                string cmd = $"docker run --name {_containerName} {envArgs} -d {ImageTag} /bin/bash -c \"./config.sh --url {_repoUrl} --token {_token} --name {_runnerName} && ./run.sh\"";

                return await RunCliAsync(cmd, cancellationToken);
            }
            else
            {
                var mergedEnv = new List<string>(envVars)
                {
                    $"RUNNER_REPO_URL={_repoUrl}",
                    $"RUNNER_TOKEN={_token}",
                    $"RUNNER_NAME={_runnerName}"
                };

                var createParams = new CreateContainerParameters
                {
                    Image = ImageTag,
                    Name = _containerName,
                    Env = mergedEnv,
                    HostConfig = new HostConfig
                    {
                        AutoRemove = true,
                        RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.No }
                    },
                    Tty = true,
                    Cmd = new[]
                    {
                        "/bin/bash", "-c",
                        $"./config.sh --url {_repoUrl} --token {_token} --name {_runnerName} && ./run.sh"
                    }
                };

                var response = await _docker!.Containers.CreateContainerAsync(createParams, cancellationToken);
                _logger?.LogInformation("Container created: {Id}", response.ID);

                bool started = await _docker.Containers.StartContainerAsync(response.ID, null, cancellationToken);
                _logger?.LogInformation("Container started: {Started}", started);
                return started;
            }
        }

        public async Task<bool> StopContainersAsync(CancellationToken cancellationToken)
        {
            // If we don't yet know the container name (fresh process), try to discover
            // a suitable container (by image tag or by name pattern).
            if (string.IsNullOrEmpty(_containerName))
            {
                _logger?.LogInformation("No container name known; attempting to discover runner container");
                await DiscoverContainerAsync(cancellationToken).ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(_containerName))
            {
                _logger?.LogInformation("No runner container found; treating stop as success");
                return true;
            }

            if (_useCli)
            {
                string cmd = $"docker stop {_containerName}";
                return await RunCliAsync(cmd, cancellationToken);
            }
            else
            {
                try
                {
                    await _docker!.Containers.StopContainerAsync(_containerName, new ContainerStopParameters { WaitBeforeKillSeconds = 10 }, cancellationToken);
                    _logger?.LogInformation("Container stopped");
                }
                catch (DockerContainerNotFoundException)
                {
                    _logger?.LogWarning("Container {Container} not found", _containerName);
                }

                return true;
            }
        }

        // High level convenience: stop/unregister runner. If the container is present but
        // not running, start it temporarily to allow config.sh remove to execute.
        public async Task<bool> StopAsync(string? token, string? ownerRepo, string githubUrl, CancellationToken cancellationToken)
        {
            SetRegistrationInfo(token, ownerRepo, githubUrl);

            await DiscoverContainerAsync(cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(_containerName))
            {
                _logger?.LogInformation("No runner container found; nothing to stop");
                return true;
            }

            bool isRunning = await IsContainerRunningAsync(cancellationToken).ConfigureAwait(false);

            if (!isRunning)
            {
                _logger?.LogInformation("Container {Container} is not running; starting temporarily to run unregister", _containerName);
                if (_useCli)
                {
                    await RunCliAsync($"docker start {_containerName}", cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // find container id
                    var existing = await _docker!.Containers.ListContainersAsync(new ContainersListParameters { All = true }, cancellationToken).ConfigureAwait(false);
                    var found = existing.FirstOrDefault(c => c.Names.Any(n => n.TrimStart('/').Equals(_containerName)));
                    if (found != null)
                    {
                        await _docker.Containers.StartContainerAsync(found.ID, null, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            // attempt unregister inside the container
            var unregistered = await UnregisterAsync(cancellationToken).ConfigureAwait(false);

            // stop the container (best-effort)
            await StopContainersAsync(cancellationToken).ConfigureAwait(false);

            // remove the container so it doesn't linger
            await RemoveContainerIfExistsAsync(_containerName!, cancellationToken).ConfigureAwait(false);

            return unregistered;
        }

        public async Task<bool> UnregisterAsync(CancellationToken cancellationToken)
        {
            if (_repoUrl == null || _token == null)
            {
                _logger?.LogWarning("Unregister skipped: missing repo URL or token");
                return false;
            }

            // allow explicit override of token/repo via CLI (SetRegistrationInfo)

            if (string.IsNullOrEmpty(_containerName))
            {
                _logger?.LogInformation("No container name known for unregister; attempting discovery");
                await DiscoverContainerAsync(cancellationToken).ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(_containerName))
            {
                _logger?.LogWarning("Unregister skipped: no container found");
                return false;
            }

                if (_useCli)
                {
                    // config.sh remove does not accept --url; pass token only to avoid warning
                    string cmd = $"docker exec {_containerName} /bin/bash -c \"./config.sh remove --token {_token}\"";
                    return await RunCliAsync(cmd, cancellationToken);
                }
                else
                {
                    var execCreate = await _docker!.Containers.ExecCreateContainerAsync(_containerName, new ContainerExecCreateParameters
                    {
                        AttachStdout = true,
                        AttachStderr = true,
                        Cmd = new[] { "/bin/bash", "-c", $"./config.sh remove --token {_token}" }
                    }, cancellationToken);

                    using var stream = await _docker.Containers.StartAndAttachContainerExecAsync(execCreate.ID, false, cancellationToken);
                    var (stdout, stderr) = await stream.ReadOutputToEndAsync(cancellationToken);
                    _logger?.LogInformation("Unregister output: {Output}", stdout.Trim());
                    return true;
                }
        }

        // Allow the CLI to provide repo/token when invoked in a fresh process so
        // UnregisterAsync can run even if RegisterAsync wasn't called in this process.
        public void SetRegistrationInfo(string? token, string? ownerRepo, string githubUrl)
        {
            if (!string.IsNullOrEmpty(ownerRepo))
            {
                _repoUrl = githubUrl.TrimEnd('/') + "/" + ownerRepo;
            }
            else
            {
                _repoUrl ??= githubUrl.TrimEnd('/');
            }

            if (!string.IsNullOrEmpty(token)) _token = token;
        }

        private async Task DiscoverContainerAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_useCli)
                {
                    // look for a running container from the github-runner image, or name prefix
                    var out1 = await RunCliCaptureOutputAsync($"docker ps -a --filter ancestor={ImageTag} --format \"{{{{.ID}}}} {{{{.Names}}}}\"", cancellationToken);
                    if (!string.IsNullOrWhiteSpace(out1))
                    {
                        var first = out1.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                        var parts = first.Split(' ', 2);
                        if (parts.Length >= 2)
                        {
                            _containerName = parts[1].Trim();
                            _logger?.LogInformation("Discovered container by image: {Container}", _containerName);
                            return;
                        }
                    }

                    var out2 = await RunCliCaptureOutputAsync($"docker ps -a --filter \"name=github-runner-\" --format \"{{{{.ID}}}} {{{{.Names}}}}\"", cancellationToken);
                    if (!string.IsNullOrWhiteSpace(out2))
                    {
                        var first = out2.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                        var parts = first.Split(' ', 2);
                        if (parts.Length >= 2)
                        {
                            _containerName = parts[1].Trim();
                            _logger?.LogInformation("Discovered container by name pattern: {Container}", _containerName);
                            return;
                        }
                    }
                }
                else
                {
                    var existing = await _docker!.Containers.ListContainersAsync(new ContainersListParameters { All = true }, cancellationToken);
                    var found = existing.FirstOrDefault(c => (c.Image != null && c.Image.Contains("github-runner")) || c.Names.Any(n => n.Contains("github-runner-")));
                    if (found != null)
                    {
                        // Strip leading slash from names if present
                        var name = found.Names.FirstOrDefault() ?? found.ID;
                        _containerName = name.StartsWith("/") ? name.Substring(1) : name;
                        _logger?.LogInformation("Discovered container via Docker API: {Container}", _containerName);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error while discovering runner container");
            }
        }

        private async Task<bool> IsContainerRunningAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(_containerName)) return false;

                if (_useCli)
                {
                    var outp = await RunCliCaptureOutputAsync($"docker ps --filter name={_containerName} --format \"{{{{.ID}}}} {{{{.Names}}}}\"", cancellationToken).ConfigureAwait(false);
                    return !string.IsNullOrWhiteSpace(outp);
                }
                else
                {
                    var list = await _docker!.Containers.ListContainersAsync(new ContainersListParameters { All = false }, cancellationToken).ConfigureAwait(false);
                    return list.Any(c => c.Names.Any(n => n.TrimStart('/').Equals(_containerName)));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to determine container running state");
                return false;
            }
        }

        // --- Helpers --------------------------------------------------------

        private async Task<bool> ImageExistsAsync(CancellationToken cancellationToken)
        {
            if (_useCli)
            {
                string cmd = $"docker images -q {ImageTag}";
                var output = await RunCliCaptureOutputAsync(cmd, cancellationToken);
                return !string.IsNullOrWhiteSpace(output);
            }
            else
            {
                var images = await _docker!.Images.ListImagesAsync(new ImagesListParameters { All = true }, cancellationToken);
                return images.Any(i => i.RepoTags != null && i.RepoTags.Contains(ImageTag));
            }
        }

        private async Task BuildImageAsync(CancellationToken cancellationToken)
        {
            string dockerfile = @"
FROM ubuntu:22.04
# install only what the docs specify: curl, tar and CA certificates so the download works
RUN apt-get update && apt-get install -y curl tar ca-certificates libicu70 && rm -rf /var/lib/apt/lists/*
# create a non-root user 'runner' and make the actions-runner directory owned by that user
RUN useradd -m -s /bin/bash runner
RUN mkdir /actions-runner && chown runner:runner /actions-runner
WORKDIR /actions-runner
USER runner
RUN curl -o actions-runner-linux-x64-2.328.0.tar.gz -L https://github.com/actions/runner/releases/download/v2.328.0/actions-runner-linux-x64-2.328.0.tar.gz && \
    tar xzf ./actions-runner-linux-x64-2.328.0.tar.gz
";
            string tempDir = Path.Combine(Path.GetTempPath(), "runner-docker");
            Directory.CreateDirectory(tempDir);
            string dockerfilePath = Path.Combine(tempDir, "Dockerfile");
            await File.WriteAllTextAsync(dockerfilePath, dockerfile, cancellationToken);

            if (_useCli)
            {
                string cmd = $"docker build -t {ImageTag} -f \"{dockerfilePath}\" \"{tempDir}\"";
                await RunCliAsync(cmd, cancellationToken);
            }
            else
            {
                using var tarStream = DockerfileHelper.CreateTarballForBuildContext(tempDir);
                await _docker!.Images.BuildImageFromDockerfileAsync(
                    tarStream,
                    new ImageBuildParameters { Tags = new[] { ImageTag } },
                    cancellationToken);
            }
        }

        private async Task RemoveContainerIfExistsAsync(string name, CancellationToken cancellationToken)
        {
            if (_useCli)
            {
                // check if the container exists first to avoid docker emitting an error
                string checkCmd = $"docker ps -a --filter name={name} -q";
                var outp = await RunCliCaptureOutputAsync(checkCmd, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(outp))
                {
                    string cmd = $"docker rm -f {name}";
                    await RunCliAsync(cmd, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                var existing = await _docker!.Containers.ListContainersAsync(new ContainersListParameters { All = true }, cancellationToken);
                var found = existing.FirstOrDefault(c => c.Names.Contains("/" + name));
                if (found != null)
                {
                    _logger?.LogInformation("Removing existing container {Container}", name);
                    await _docker.Containers.RemoveContainerAsync(found.ID, new ContainerRemoveParameters { Force = true }, cancellationToken);
                }
            }
        }

        private async Task<bool> RunCliAsync(string cmd, CancellationToken cancellationToken)
        {
                _logger?.LogInformation("Executing CLI: {Cmd}", cmd);
                var psi = new ProcessStartInfo("cmd.exe", $"/c {cmd}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

                proc.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine(e.Data);
                        _logger?.LogInformation(e.Data);
                    }
                };

                proc.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.Error.WriteLine(e.Data);
                        _logger?.LogError(e.Data);
                    }
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                await proc.WaitForExitAsync(cancellationToken);
                return proc.ExitCode == 0;
        }

        private async Task<string> RunCliCaptureOutputAsync(string cmd, CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo("cmd.exe", $"/c {cmd}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi)!;
            var stdOutTask = proc.StandardOutput.ReadToEndAsync();
            var stdErrTask = proc.StandardError.ReadToEndAsync();

            await Task.WhenAll(stdOutTask, stdErrTask, proc.WaitForExitAsync(cancellationToken));
            var output = stdOutTask.Result + "\n" + stdErrTask.Result;
            return output.Trim();
        }

        public async ValueTask DisposeAsync()
        {
            _logger?.LogDebug("Disposing Docker client");
            _docker?.Dispose();
            await Task.CompletedTask;
        }
    }

    internal static class DockerfileHelper
    {
        public static Stream CreateTarballForBuildContext(string directoryPath)
        {
            var stream = new MemoryStream();
            System.IO.Compression.ZipFile.CreateFromDirectory(directoryPath, stream, System.IO.Compression.CompressionLevel.Fastest, false);
            stream.Position = 0;
            return stream;
        }
    }
}
