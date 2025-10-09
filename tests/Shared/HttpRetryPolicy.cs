using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetApp.Tests.Shared;

public static class HttpRetryPolicy
{
    // Try the provided action until it returns a successful HttpResponseMessage or the timeout is reached.
    public static async Task<HttpResponseMessage> WaitForSuccessAsync(Func<Task<HttpResponseMessage>> action, TimeSpan timeout, TimeSpan retryDelay, CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        Exception? lastEx = null;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var res = await action();
                if (res != null && res.IsSuccessStatusCode) return res;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastEx = ex;
            }
            await Task.Delay(retryDelay, cancellationToken);
        }

        throw new TimeoutException($"HTTP retry timed out after {timeout}. Last exception: {lastEx?.Message}");
    }
}
