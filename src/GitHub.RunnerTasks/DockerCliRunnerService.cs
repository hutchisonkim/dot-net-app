using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.RunnerTasks
{
    [Obsolete("DockerCliRunnerService is retired. Use DockerRunnerService instead.", error: false)]
    public class DockerCliRunnerService : IRunnerService
    {
        public DockerCliRunnerService(string workingDirectory, object? logger = null) { }
        public Task<bool> RegisterAsync(string token, string ownerRepo, string githubUrl, CancellationToken cancellationToken) => throw new NotSupportedException("DockerCliRunnerService is retired. Use DockerRunnerService.");
        public Task<bool> StartContainersAsync(string[] envVars, CancellationToken cancellationToken) => throw new NotSupportedException("DockerCliRunnerService is retired. Use DockerRunnerService.");
        public Task<bool> StopContainersAsync(CancellationToken cancellationToken) => throw new NotSupportedException("DockerCliRunnerService is retired. Use DockerRunnerService.");
        public Task<bool> UnregisterAsync(CancellationToken cancellationToken) => throw new NotSupportedException("DockerCliRunnerService is retired. Use DockerRunnerService.");
    }
}
