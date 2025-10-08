using DotNetApp.Core.Abstractions;
using DotNetApp.Core.Models;

namespace DotNetApp.Server.Services;

public class DefaultHealthService : IHealthService
{
    public Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(HealthStatus.Healthy.Status);
}
