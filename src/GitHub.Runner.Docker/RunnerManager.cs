using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GitHub.Runner.Docker
{
    public interface IRunnerService
    {
        Task<bool> RegisterAsync(string token, string ownerRepo, string githubUrl, CancellationToken cancellationToken);
        Task<bool> StartContainersAsync(string[] envVars, CancellationToken cancellationToken);
        Task<bool> StopContainersAsync(CancellationToken cancellationToken);
        Task<bool> UnregisterAsync(CancellationToken cancellationToken);
    }

    public class RunnerManager
    {
        private readonly IRunnerService _service;
        private readonly ILogger<RunnerManager>? _logger;

        public RunnerManager(IRunnerService service, ILogger<RunnerManager>? logger = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger;
        }

        public async Task<bool> StartWithRetriesAsync(string token, string ownerRepo, string githubUrl, int maxRetries = 5, int baseDelayMs = 200, CancellationToken cancellationToken = default)
        {
            if (maxRetries <= 0) throw new ArgumentOutOfRangeException(nameof(maxRetries));
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool ok;
                try
                {
                    _logger?.LogDebug("Attempt {Attempt} to register runner for {Repo}", attempt, ownerRepo);
                    ok = await _service.RegisterAsync(token, ownerRepo, githubUrl, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogDebug("Registration attempt {Attempt} cancelled", attempt);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "RegisterAsync threw an exception on attempt {Attempt}", attempt);
                    ok = false;
                }

                if (ok) return true;

                var delay = baseDelayMs * attempt;
                try
                {
                    _logger?.LogDebug("Waiting {Delay}ms before retry", delay);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogDebug("Delay cancelled before next attempt");
                    throw;
                }
            }
            return false;
        }

        public Task<bool> StartRunnerStackAsync(string[] envVars, CancellationToken cancellationToken = default) =>
            _service.StartContainersAsync(envVars, cancellationToken);

        public Task<bool> StopRunnerStackAsync(CancellationToken cancellationToken = default) =>
            _service.StopContainersAsync(cancellationToken);

        public async Task<bool> OrchestrateStartAsync(string token, string ownerRepo, string githubUrl, string[] envVars, int maxRetries = 5, int baseDelayMs = 200, CancellationToken cancellationToken = default)
        {
            if (envVars == null)
            {
                _logger?.LogError("envVars is null");
                throw new ArgumentNullException(nameof(envVars));
            }

            if (Array.FindIndex(envVars, e => e != null && e.StartsWith("GITHUB_REPOSITORY=", StringComparison.OrdinalIgnoreCase)) < 0)
            {
                _logger?.LogError("envVars missing required GITHUB_REPOSITORY entry");
                throw new ArgumentException("envVars must contain GITHUB_REPOSITORY=...", nameof(envVars));
            }

            _logger?.LogInformation("Starting orchestration: repo={Repo}", ownerRepo);

            var registered = await StartWithRetriesAsync(token, ownerRepo, githubUrl, maxRetries, baseDelayMs, cancellationToken).ConfigureAwait(false);
            if (!registered)
            {
                _logger?.LogWarning("Registration failed after {MaxRetries} attempts", maxRetries);
                return false;
            }

            _logger?.LogInformation("Registration succeeded; starting containers");
            var started = await StartRunnerStackAsync(envVars, cancellationToken).ConfigureAwait(false);
            if (!started)
            {
                _logger?.LogWarning("StartRunnerStackAsync returned false");
            }
            else
            {
                _logger?.LogInformation("Runner containers started");
            }

            return started;
        }

        public async Task<bool> OrchestrateStopAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _service.UnregisterAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "UnregisterAsync threw an exception; proceeding to stop containers");
            }

            return await StopRunnerStackAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
