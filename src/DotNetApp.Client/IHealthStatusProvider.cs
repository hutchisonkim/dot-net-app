using System.Threading;
using System.Threading.Tasks;

namespace DotNetApp.Client
{
    public interface IHealthStatusProvider
    {
        Task<string?> FetchStatusAsync(CancellationToken cancellationToken = default);
    }
}
