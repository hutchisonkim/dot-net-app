using System.Threading;
using System.Threading.Tasks;

namespace DotNetApp.Core.Abstractions
{
    public interface IHealthService
    {
        Task<string> GetStatusAsync(CancellationToken cancellationToken = default);
    }
}
