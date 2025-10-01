using System.Threading;
using System.Threading.Tasks;
using DotNetApp.CodeGen;

namespace DotNetApp.Client.Shared.Contracts;

[ApiContract("api/state")] // base path shared by endpoints in StateController
public interface IPlatformApi
{
    [Get("health")] Task<HealthStatusDto?> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}

public sealed record HealthStatusDto(string? status);
