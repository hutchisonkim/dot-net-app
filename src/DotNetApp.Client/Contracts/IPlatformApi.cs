using System.Threading;
using System.Threading.Tasks;

namespace DotNetApp.Client.Contracts;

public interface IPlatformApi
{
    Task<HealthStatusDto?> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}

public sealed record HealthStatusDto(string? status);
