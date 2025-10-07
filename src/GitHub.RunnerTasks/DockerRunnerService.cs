using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace GitHub.RunnerTasks
{
    public class DockerRunnerService : IRunnerService, IAsyncDisposable
    {
        private readonly DockerClient _docker;
        private readonly ILogger<DockerRunnerService>? _logger;

        private const string ImageTag = "github-runner:latest";
        private string? _repoUrl;
        private string? _token;
        private string? _runnerName;
        private string? _containerName;

        public DockerRunnerService(ILogger<DockerRunnerService>? logger = null)
        {
            _logger = logger;
            _docker = new DockerClientConfiguration(
                new Uri(Environment.GetEnvironmentVariable("DOCKER_HOST") ?? "unix:///var/run/docker.sock"))
                .CreateClient();
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

            var response = await _docker.Containers.CreateContainerAsync(createParams, cancellationToken);
            _logger?.LogInformation("Container created: {Id}", response.ID);

            bool started = await _docker.Containers.StartContainerAsync(response.ID, null, cancellationToken);
            _logger?.LogInformation("Container started: {Started}", started);

            return started;
        }

        public async Task<bool> StopContainersAsync(CancellationToken cancellationToken)
        {
            if (_containerName == null)
            {
                _logger?.LogWarning("Stop requested but no container name known");
                return false;
            }

            _logger?.LogInformation("Stopping container {Container}", _containerName);
            try
            {
                await _docker.Containers.StopContainerAsync(_containerName, new ContainerStopParameters
                {
                    WaitBeforeKillSeconds = 10
                }, cancellationToken);
                _logger?.LogInformation("Container stopped");
            }
            catch (DockerContainerNotFoundException)
            {
                _logger?.LogWarning("Container {Container} not found", _containerName);
            }

            return true;
        }

        public async Task<bool> UnregisterAsync(CancellationToken cancellationToken)
        {
            if (_repoUrl == null || _token == null || _containerName == null)
            {
                _logger?.LogWarning("Unregister skipped: missing parameters");
                return false;
            }

            _logger?.LogInformation("Unregistering runner {Runner}", _runnerName);

            var execCreate = await _docker.Containers.ExecCreateContainerAsync(_containerName, new ContainerExecCreateParameters
            {
                AttachStdout = true,
                AttachStderr = true,
                Cmd = new[]
                {
                    "/bin/bash", "-c",
                    $"./config.sh remove --url {_repoUrl} --token {_token}"
                }
            }, cancellationToken);

            using var stream = await _docker.Containers.StartAndAttachContainerExecAsync(execCreate.ID, false, cancellationToken);
            var (stdout, stderr) = await stream.ReadOutputToEndAsync(cancellationToken);
            string output = stdout; // Use stdout or stderr as needed

            _logger?.LogInformation("Unregister output: {Output}", output.Trim());
            return true;
        }

        // --- Helpers --------------------------------------------------------

        private async Task<bool> ImageExistsAsync(CancellationToken cancellationToken)
        {
            var images = await _docker.Images.ListImagesAsync(new ImagesListParameters { All = true }, cancellationToken);
            return images.Any(i => i.RepoTags != null && i.RepoTags.Contains(ImageTag));
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

            using var tarStream = DockerfileHelper.CreateTarballForBuildContext(tempDir);
            await _docker.Images.BuildImageFromDockerfileAsync(
                tarStream,
                new ImageBuildParameters { Tags = new[] { ImageTag } },
                cancellationToken);
        }

        private async Task RemoveContainerIfExistsAsync(string name, CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _docker.Containers.ListContainersAsync(
                    new ContainersListParameters { All = true }, cancellationToken);

                var found = existing.FirstOrDefault(c => c.Names.Contains("/" + name));
                if (found != null)
                {
                    _logger?.LogInformation("Removing existing container {Container}", name);
                    await _docker.Containers.RemoveContainerAsync(found.ID, new ContainerRemoveParameters { Force = true }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error cleaning up existing container");
            }
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
            // Simplified tarball creator; production code should use proper tar.
            var stream = new MemoryStream();
            System.IO.Compression.ZipFile.CreateFromDirectory(directoryPath, stream, System.IO.Compression.CompressionLevel.Fastest, false);
            stream.Position = 0;
            return stream;
        }
    }
}
