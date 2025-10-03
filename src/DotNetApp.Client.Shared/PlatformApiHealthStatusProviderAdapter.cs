using System.Threading;
using System.Threading.Tasks;
using DotNetApp.Client.Shared.Contracts;

namespace DotNetApp.Client.Shared;

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
