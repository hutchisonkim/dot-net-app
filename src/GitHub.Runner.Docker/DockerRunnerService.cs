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

            _repoUrl = githubUrl.TrimEnd('/');
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
            if (_containerName == null)
            {
                _logger?.LogInformation("Stop requested but no container name known; treating as success");
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

        public async Task<bool> UnregisterAsync(CancellationToken cancellationToken)
        {
            if (_repoUrl == null || _token == null || _containerName == null)
            {
                _logger?.LogWarning("Unregister skipped: missing parameters");
                return false;
            }

            if (_useCli)
            {
                string cmd = $"docker exec {_containerName} /bin/bash -c \"./config.sh remove --url {_repoUrl} --token {_token}\"";
                return await RunCliAsync(cmd, cancellationToken);
            }
            else
            {
                var execCreate = await _docker!.Containers.ExecCreateContainerAsync(_containerName, new ContainerExecCreateParameters
                {
                    AttachStdout = true,
                    AttachStderr = true,
                    Cmd = new[] { "/bin/bash", "-c", $"./config.sh remove --url {_repoUrl} --token {_token}" }
                }, cancellationToken);

                using var stream = await _docker.Containers.StartAndAttachContainerExecAsync(execCreate.ID, false, cancellationToken);
                var (stdout, stderr) = await stream.ReadOutputToEndAsync(cancellationToken);
                _logger?.LogInformation("Unregister output: {Output}", stdout.Trim());
                return true;
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
RUN apt-get update && apt-get install -y curl tar sudo git
RUN mkdir /actions-runner && cd /actions-runner && \
    curl -o actions-runner-linux-x64-2.328.0.tar.gz -L https://github.com/actions/runner/releases/download/v2.328.0/actions-runner-linux-x64-2.328.0.tar.gz && \
    tar xzf ./actions-runner-linux-x64-2.328.0.tar.gz
WORKDIR /actions-runner
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
                string cmd = $"docker rm -f {name}";
                await RunCliAsync(cmd, cancellationToken);
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
                UseShellExecute = false
            };
            using var proc = Process.Start(psi)!;
            await proc.WaitForExitAsync(cancellationToken);
            return proc.ExitCode == 0;
        }

        private async Task<string> RunCliCaptureOutputAsync(string cmd, CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo("cmd.exe", $"/c {cmd}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var proc = Process.Start(psi)!;
            string output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync(cancellationToken);
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
