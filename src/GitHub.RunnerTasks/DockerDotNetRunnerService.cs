using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.RunnerTasks
{
    [Obsolete("DockerDotNetRunnerService is retired. Use DockerRunnerService instead.", error: false)]
    public partial class DockerDotNetRunnerService : IRunnerService
    {
        public DockerDotNetRunnerService(string workingDirectory, object? logger = null) { }
        public DockerDotNetRunnerService(string workingDirectory, object clientWrapper, object? logger = null) { }
        public Task<bool> RegisterAsync(string token, string ownerRepo, string githubUrl, CancellationToken cancellationToken) => throw new NotSupportedException("DockerDotNetRunnerService is retired. Use DockerRunnerService.");
        public Task<bool> StartContainersAsync(string[] envVars, CancellationToken cancellationToken) => throw new NotSupportedException("DockerDotNetRunnerService is retired. Use DockerRunnerService.");
        public Task<bool> StopContainersAsync(CancellationToken cancellationToken) => throw new NotSupportedException("DockerDotNetRunnerService is retired. Use DockerRunnerService.");
        public Task<bool> UnregisterAsync(CancellationToken cancellationToken) => throw new NotSupportedException("DockerDotNetRunnerService is retired. Use DockerRunnerService.");
    }
}
