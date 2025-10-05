using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RunnerTasks.Tests
{
    public class FakeRunnerService : IRunnerService
    {
        private readonly Queue<bool> _registerResults;
        public int RegisterCallCount { get; private set; } = 0;
        public int StartCallCount { get; private set; } = 0;
        public int StopCallCount { get; private set; } = 0;
        public int UnregisterCallCount { get; private set; } = 0;
    public string[] LastStartedEnv { get; private set; } = Array.Empty<string>();

        public FakeRunnerService(IEnumerable<bool> registerResults)
        {
            _registerResults = new Queue<bool>(registerResults ?? Array.Empty<bool>());
        }

        public Task<bool> RegisterAsync(string token, string ownerRepo, string githubUrl, System.Threading.CancellationToken cancellationToken)
        {
            RegisterCallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            if (_registerResults.Count > 0)
            {
                return Task.FromResult(_registerResults.Dequeue());
            }
            return Task.FromResult(false);
        }

        public Task<bool> StartContainersAsync(string[] envVars, System.Threading.CancellationToken cancellationToken)
        {
            StartCallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            LastStartedEnv = envVars;
            return Task.FromResult(true);
        }

        public Task<bool> StopContainersAsync(System.Threading.CancellationToken cancellationToken)
        {
            StopCallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }

        public Task<bool> UnregisterAsync(System.Threading.CancellationToken cancellationToken)
        {
            UnregisterCallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }
    }
}
