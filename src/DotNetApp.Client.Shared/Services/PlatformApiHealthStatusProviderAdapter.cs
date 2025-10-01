using System.Threading;
using System.Threading.Tasks;
using DotNetApp.Client.Shared.Contracts;
using DotNetApp.Core.Abstractions;

namespace DotNetApp.Client.Services;

// Adapter to keep existing IHealthStatusProvider-based callers working while migrating.
public sealed class PlatformApiHealthStatusProviderAdapter : IHealthStatusProvider
{
    private readonly IPlatformApi _api;
    public PlatformApiHealthStatusProviderAdapter(IPlatformApi api) => _api = api;
    public async Task<string?> FetchStatusAsync(CancellationToken cancellationToken = default)
    {
        var dto = await _api.GetHealthStatusAsync(cancellationToken);
        return dto?.status;
    }
}
