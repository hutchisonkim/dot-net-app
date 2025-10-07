using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GitHub.RunnerTasks
{
    /// <summary>
    /// CLI-based runner service implementation. Uses the `docker` CLI via ProcessRunner to build images,
    /// start/stop containers, run configure scripts, and cleanup volumes. This keeps CLI behavior separated
    /// from the Docker.DotNet-based implementation.
    /// </summary>
    public class DockerCliRunnerService : IRunnerService
    {
        private readonly string _workingDirectory;
        private readonly ILogger<DockerCliRunnerService>? _logger;
        private string? _containerId;
        private string? _createdVolumeName;
        private string? _lastRegistrationToken;

        public DockerCliRunnerService(string workingDirectory, ILogger<DockerCliRunnerService>? logger = null)
        {
            _workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
            _logger = logger;
        }

        private async Task<bool> TryBuildImageWithDockerCli(string tag, CancellationToken cancellationToken)
        {
            var tmp = Path.Combine(Path.GetTempPath(), "runner-image-build-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            try
            {
                var lines = new[]
                {
                    "FROM ubuntu:20.04",
                    // ensure PATH is set for all users and basic tools installed
                    "ENV PATH=\"/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin\"",
                    "RUN apt-get update && apt-get install -y curl ca-certificates tar gzip sudo openssl && rm -rf /var/lib/apt/lists/*",
                    "RUN useradd -m -s /bin/bash github-runner",
                    "WORKDIR /actions-runner",
                    "ARG RUNNER_VERSION=2.328.0",
                    // download and extract runner, tolerate failures
                    "RUN curl -L -o actions-runner.tar.gz https://github.com/actions/runner/releases/download/v2.328.0/actions-runner-linux-x64-2.328.0.tar.gz && tar xzf actions-runner.tar.gz --strip-components=0 || true",
                    // ensure files are owned by the non-root user and provide a small wrapper for one-off configure invocation
                    "RUN chown -R github-runner:github-runner /actions-runner || true",
                    "RUN printf '#!/bin/sh\\nexec /actions-runner/config.sh \"$@\"' > /usr/local/bin/configure-runner.sh && chmod +x /usr/local/bin/configure-runner.sh && chown github-runner:github-runner /usr/local/bin/configure-runner.sh || true",
                    "USER github-runner",
                    "CMD [\"/bin/bash\", \"-c\", \"tail -f /dev/null\"]"
                };

                var dockerfile = string.Join(Environment.NewLine, lines);
                File.WriteAllText(Path.Combine(tmp, "Dockerfile"), dockerfile);

                var exit = await ProcessRunner.RunAndStreamAsync("docker", $"build --progress=plain -t {tag} .", tmp, cancellationToken).ConfigureAwait(false);
                if (exit != 0)
                {
                    _logger?.LogWarning("docker build exited with code {ExitCode}", exit);
                    return false;
                }

                _logger?.LogInformation("Successfully built runner image {Tag}", tag);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Exception during docker build");
                return false;
            }
            finally
            {
                try { Directory.Delete(tmp, true); } catch { }
            }
        }

        public async Task<bool> StartContainersAsync(string[] envVars, CancellationToken cancellationToken)
        {
            var wantedTag = "github-self-hosted-runner-docker-github-runner:latest";

            try
            {
                var id = await ProcessRunner.CaptureOutputAsync("docker", $"images -q {wantedTag}", null, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger?.LogInformation("Image {Tag} not found locally -- attempting to build via docker CLI", wantedTag);
                    var built = await TryBuildImageWithDockerCli(wantedTag, cancellationToken).ConfigureAwait(false);
                    if (!built) return false;
                }

                var volumeName = "runner_data_cli_" + Guid.NewGuid().ToString("n");
                _logger?.LogInformation("Creating docker volume {Volume}", volumeName);
                var vout = await ProcessRunner.CaptureOutputAsync("docker", $"volume create {volumeName}", null, cancellationToken).ConfigureAwait(false);
                if (vout == null) { _logger?.LogWarning("Failed to create docker volume {Volume}", volumeName); return false; }
                _logger?.LogInformation("Created docker volume {Volume}", vout.Trim());

                var envListMutable = envVars?.ToList() ?? new System.Collections.Generic.List<string>();
                if (!envListMutable.Any(e => e.StartsWith("RUNNER_LABELS=", StringComparison.OrdinalIgnoreCase)))
                {
                    envListMutable.Add($"RUNNER_LABELS=self-hosted");
                }

                var envArgs = string.Join(' ', envListMutable.Select(e => $"-e \"{e}\""));
                var runCmd = $"run -d --name runner-cli-{Guid.NewGuid():N} -v {volumeName}:/actions-runner -v {volumeName}:/runner {envArgs} {wantedTag} tail -f /dev/null";
                var rout = await ProcessRunner.CaptureOutputAsync("docker", runCmd, null, cancellationToken).ConfigureAwait(false);
                if (rout == null)
                {
                    _logger?.LogWarning("Failed to start docker run for {Cmd}", runCmd);
                    return false;
                }
                _containerId = rout.Trim();
                _createdVolumeName = volumeName;
                _logger?.LogInformation("Started container via docker CLI: {Id}", _containerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "TryStartContainersWithDockerCli exception");
                return false;
            }
        }

        public async Task<bool> RegisterAsync(string token, string ownerRepo, string githubUrl, CancellationToken cancellationToken)
        {
            _lastRegistrationToken = token;
            if (string.IsNullOrEmpty(_containerId))
            {
                // Run a one-off container to execute configure script
                return await RunOneOffContainerAsync("github-self-hosted-runner-docker-github-runner:latest",
                    new[] { $"GITHUB_TOKEN={token}", $"GITHUB_URL={githubUrl}", $"GITHUB_REPOSITORY={ownerRepo}" },
                    new[] { "/usr/local/bin/configure-runner.sh" }, cancellationToken).ConfigureAwait(false);
            }

            // Exec into existing container
            try
            {
                var joinedArgs = $"--url {(githubUrl ?? "").TrimEnd('/')}/{ownerRepo} --token {token} --name runner-{Guid.NewGuid().ToString("N").Substring(0,8)} --labels self-hosted --work _work --ephemeral";
                var cmd = $"exec -u github-runner {_containerId} /actions-runner/config.sh {joinedArgs}";
                _logger?.LogInformation("Running docker {Cmd}", cmd);
                var exit = await ProcessRunner.RunAndStreamAsync("docker", cmd, null, cancellationToken).ConfigureAwait(false);
                _logger?.LogInformation("docker exec exit code: {ExitCode}", exit);
                if (exit != 0) return false;

                // Start the runner in the container
                var startCmd = $"exec -u github-runner {_containerId} /actions-runner/run.sh";
                var startExit = await ProcessRunner.RunAndStreamAsync("docker", startCmd, null, cancellationToken).ConfigureAwait(false);
                _logger?.LogInformation("docker exec run.sh exit code: {ExitCode}", startExit);
                return startExit == 0;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "RegisterAsync via docker CLI failed");
                return false;
            }
        }

        public async Task<bool> RunOneOffContainerAsync(string image, string[]? env, string[] cmd, CancellationToken cancellationToken)
        {
            try
            {
                var parts = image.Split(':');
                var fromImage = parts[0];
                var tag = parts.Length > 1 ? parts[1] : "latest";
                try { await ProcessRunner.CaptureOutputAsync("docker", $"pull {fromImage}:{tag}", null, cancellationToken).ConfigureAwait(false); } catch { }

                var name = $"oneoff-{Guid.NewGuid():N}";
                var envArgs = env != null ? string.Join(' ', env.Select(e => $"-e \"{e}\"")) : string.Empty;
                var cmdArgs = string.Join(' ', cmd.Select(a => a.Contains(' ') ? '"' + a + '"' : a));
                var runCmd = $"run --rm --name {name} {envArgs} {image} {cmdArgs}";
                var exit = await ProcessRunner.RunAndStreamAsync("docker", runCmd, null, cancellationToken).ConfigureAwait(false);
                return exit == 0;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "RunOneOffContainerAsync failed");
                return false;
            }
        }

        public async Task<bool> StopContainersAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Stop and remove any runner-cli containers
                var outStr = await ProcessRunner.CaptureOutputAsync("docker", "ps -a --filter \"name=runner-cli\" --format \"{{.ID}}\"", null, cancellationToken).ConfigureAwait(false) ?? string.Empty;
                var ids = outStr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                foreach (var id in ids)
                {
                    try { await ProcessRunner.CaptureOutputAsync("docker", $"rm -f {id}", null, cancellationToken).ConfigureAwait(false); } catch { }
                }

                // Remove volumes named runner_data_cli_*
                var vout = await ProcessRunner.CaptureOutputAsync("docker", "volume ls --format \"{{.Name}}\"", null, cancellationToken).ConfigureAwait(false) ?? string.Empty;
                var vols = vout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.StartsWith("runner_data_cli_") || s.Equals("runner_data", StringComparison.OrdinalIgnoreCase) || s.StartsWith("github-self-hosted-runner-docker_runner_data")).ToArray();
                foreach (var vol in vols)
                {
                    try { await ProcessRunner.CaptureOutputAsync("docker", $"volume rm {vol}", null, cancellationToken).ConfigureAwait(false); } catch { }
                }

                // Also stop tracked container if present
                if (!string.IsNullOrEmpty(_containerId))
                {
                    try { await ProcessRunner.CaptureOutputAsync("docker", $"rm -f {_containerId}", null, cancellationToken).ConfigureAwait(false); } catch { }
                    _containerId = null;
                }

                if (!string.IsNullOrEmpty(_createdVolumeName))
                {
                    try { await ProcessRunner.CaptureOutputAsync("docker", $"volume rm {_createdVolumeName}", null, cancellationToken).ConfigureAwait(false); } catch { }
                    _createdVolumeName = null;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error during docker CLI cleanup of runner-cli containers/volumes");
                return false;
            }
        }

        public async Task<bool> UnregisterAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_lastRegistrationToken) || string.IsNullOrEmpty(_containerId)) return true;
            try
            {
                var args = $"exec -u github-runner {_containerId} /actions-runner/config.sh remove --unattended --token {_lastRegistrationToken}";
                var exit = await ProcessRunner.RunAndStreamAsync("docker", args, null, cancellationToken).ConfigureAwait(false);
                _lastRegistrationToken = null;
                return exit == 0;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "docker CLI unregister attempt failed");
                _lastRegistrationToken = null;
                return false;
            }
        }
    }
}
