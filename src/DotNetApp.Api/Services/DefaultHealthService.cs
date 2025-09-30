using DotNetApp.Core.Abstractions;
using DotNetApp.Core.Models;

namespace DotNetApp.Api.Services;

public class DefaultHealthService : IHealthService
{
    public Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(HealthStatus.Healthy.Status);
}
