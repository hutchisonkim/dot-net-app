using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RunnerTasks.Tests
{
    /// <summary>
    /// Simple implementation that shells out to docker-compose in the provided working directory.
    /// This class is intended for gated integration tests only.
    /// </summary>
    public class DockerComposeRunnerService : IRunnerService
    {
        private readonly string _workingDirectory;
        private readonly ILogger<DockerComposeRunnerService>? _logger;

        public DockerComposeRunnerService(string workingDirectory, ILogger<DockerComposeRunnerService>? logger = null)
        {
            _workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
            _logger = logger;
        }

        public async Task<bool> RegisterAsync(string token, string ownerRepo, string githubUrl, CancellationToken cancellationToken)
        {
            // For registration, we run the configure-runner.sh inside the container using docker-compose run.
            // The repository token must be provided via environment.
            var env = $"GITHUB_TOKEN={token} GITHUB_URL={githubUrl} GITHUB_REPOSITORY={ownerRepo}";
            var args = $"run --rm -e GITHUB_TOKEN -e GITHUB_URL -e GITHUB_REPOSITORY github-runner /usr/local/bin/configure-runner.sh";
            _logger?.LogInformation("Running docker-compose {Args} in {Dir}", args, _workingDirectory);
            return await RunProcessAsync("docker-compose", args, _workingDirectory, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> StartContainersAsync(string[] envVars, CancellationToken cancellationToken)
        {
            var args = "up -d";
            _logger?.LogInformation("Starting docker-compose in {Dir}", _workingDirectory);
            return await RunProcessAsync("docker-compose", args, _workingDirectory, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> StopContainersAsync(CancellationToken cancellationToken)
        {
            var args = "down";
            _logger?.LogInformation("Stopping docker-compose in {Dir}", _workingDirectory);
            return await RunProcessAsync("docker-compose", args, _workingDirectory, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> UnregisterAsync(CancellationToken cancellationToken)
        {
            // Run the remove-runner.sh script inside a one-off container to unregister the runner
            var args = "run --rm github-runner /usr/local/bin/remove-runner.sh";
            _logger?.LogInformation("Unregistering runner via docker-compose in {Dir}", _workingDirectory);
            return await RunProcessAsync("docker-compose", args, _workingDirectory, cancellationToken).ConfigureAwait(false);
        }

        private async Task<bool> RunProcessAsync(string fileName, string args, string workingDirectory, CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            try
            {
                proc.Start();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start process {FileName} {Args}", fileName, args);
                return false;
            }

            // Stream stdout and stderr lines to the logger as they arrive.
            var stdoutTask = Task.Run(async () =>
            {
                try
                {
                    string? line;
                    while ((line = await proc.StandardOutput.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        _logger?.LogInformation("[docker-compose stdout] {Line}", line);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Error reading stdout");
                }
            }, cancellationToken);

            var stderrTask = Task.Run(async () =>
            {
                try
                {
                    string? line;
                    while ((line = await proc.StandardError.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        _logger?.LogWarning("[docker-compose stderr] {Line}", line);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Error reading stderr");
                }
            }, cancellationToken);

            try
            {
                while (!proc.WaitForExit(100))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        try { proc.Kill(); } catch { }
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // already logged above; rethrow
                throw;
            }

            // Ensure readers finish
            try { await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false); } catch { }

            _logger?.LogInformation("Process {FileName} {Args} exited with code {ExitCode}", fileName, args, proc.ExitCode);

            return proc.ExitCode == 0;
        }
    }
}
