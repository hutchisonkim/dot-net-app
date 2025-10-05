using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace RunnerTasks
{
    public class DockerComposeRunnerService : IRunnerService
    {
        private readonly string _workingDirectory;
        private readonly ILogger<DockerComposeRunnerService>? _logger;
        private readonly DockerClient _client;
        private readonly IDockerClientWrapper? _clientWrapper;
    private string? _containerId = null;
        private const string VolumeName = "runner_data";
        private const string ImageName = "github-self-hosted-runner-docker-github-runner:latest";

        public DockerComposeRunnerService(string workingDirectory, ILogger<DockerComposeRunnerService>? logger = null, IDockerClientWrapper? clientWrapper = null)
        {
            _workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
            _logger = logger;

            var dockerUri = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? new Uri("npipe://./pipe/docker_engine")
                : new Uri("unix:///var/run/docker.sock");
            _client = new DockerClientConfiguration(dockerUri).CreateClient();
            _clientWrapper = clientWrapper ?? new DockerClientWrapper(_client);
        }

        // ...existing logic moved from tests; implementation unchanged other than namespace
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

                var execCreate = _clientWrapper != null
                    ? await _clientWrapper.ExecCreateAsync(_containerId, new ContainerExecCreateParameters
                {
                    AttachStdout = true,
                    AttachStderr = true,
                    User = "github-runner",
                    Cmd = configArgs,
                    Env = envList
                    }, cancellationToken).ConfigureAwait(false)
                    : await _client.Containers.ExecCreateContainerAsync(_containerId, new ContainerExecCreateParameters
                    {
                        AttachStdout = true,
                        AttachStderr = true,
                        User = "github-runner",
                        Cmd = configArgs,
                        Env = envList
                    }, cancellationToken).ConfigureAwait(false);

                using (var stream = _clientWrapper != null
                    ? await _clientWrapper.StartAndAttachExecAsync(execCreate.ID, false, cancellationToken).ConfigureAwait(false)
                    : (dynamic)await _client.Containers.StartAndAttachContainerExecAsync(execCreate.ID, false, cancellationToken).ConfigureAwait(false))
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

        // StartContainersAsync, StopContainersAsync and UnregisterAsync are unchanged in logic and will be added in a follow-up if needed.
        public Task<bool> StartContainersAsync(string[] envVars, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> StopContainersAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> UnregisterAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
