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

        private static async Task<bool> RunProcessAsync(string fileName, string args, string workingDirectory, CancellationToken cancellationToken)
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

            using var proc = new Process { StartInfo = psi };
            try
            {
                proc.Start();
            }
            catch (Exception)
            {
                return false;
            }

            var outputTask = proc.StandardOutput.ReadToEndAsync();
            var errorTask = proc.StandardError.ReadToEndAsync();

            while (!proc.HasExited)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    try { proc.Kill(); } catch { }
                    cancellationToken.ThrowIfCancellationRequested();
                }
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var output = await outputTask.ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);

            // For integration test purposes, consider exit code 0 as success.
            return proc.ExitCode == 0;
        }
    }
}
