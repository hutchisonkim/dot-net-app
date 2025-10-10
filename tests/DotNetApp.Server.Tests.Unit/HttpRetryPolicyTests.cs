using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DotNetApp.Tests.Shared;

namespace DotNetApp.Server.Tests.Unit;

[Trait("Category", "Unit")]
public class HttpRetryPolicyTests
{
    // Custom HttpMessageHandler to track disposal of HttpResponseMessage
    private class DisposalTrackingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        public int CallCount { get; private set; }
        public int DisposalCount { get; private set; }

        public DisposalTrackingHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            var response = new DisposalTrackingResponse(_statusCode, this);
            return Task.FromResult<HttpResponseMessage>(response);
        }

        public void IncrementDisposalCount() => DisposalCount++;
    }

    private class DisposalTrackingResponse : HttpResponseMessage
    {
        private readonly DisposalTrackingHandler _handler;
        private bool _disposed;

        public DisposalTrackingResponse(HttpStatusCode statusCode, DisposalTrackingHandler handler) : base(statusCode)
        {
            _handler = handler;
            Content = new StringContent("test");
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;
                _handler.IncrementDisposalCount();
            }
            base.Dispose(disposing);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task WaitForSuccessAsync_WithSuccessfulResponse_ReturnsResponse()
    {
        // Arrange
        var handler = new DisposalTrackingHandler(HttpStatusCode.OK);
        using var client = new HttpClient(handler);

        // Act
        var result = await HttpRetryPolicy.WaitForSuccessAsync(
            () => client.GetAsync("http://localhost/test"),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100));

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessStatusCode);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal(0, handler.DisposalCount); // Success response should NOT be disposed

        // Clean up the returned response
        result.Dispose();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task WaitForSuccessAsync_WithNonSuccessResponse_DisposesResponse()
    {
        // Arrange
        var handler = new DisposalTrackingHandler(HttpStatusCode.InternalServerError);
        using var client = new HttpClient(handler);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await HttpRetryPolicy.WaitForSuccessAsync(
                () => client.GetAsync("http://localhost/test"),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(200));
        });

        // Assert - should have made multiple attempts and disposed all non-success responses
        Assert.True(handler.CallCount >= 3); // At least a few retries in 1 second with 200ms delay
        Assert.Equal(handler.CallCount, handler.DisposalCount); // All responses should be disposed
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task WaitForSuccessAsync_WithEventualSuccess_DisposesOnlyNonSuccessResponses()
    {
        // Arrange
        int callCount = 0;
        var handler = new DisposalTrackingHandler(HttpStatusCode.OK);
        
        // Override to return non-success for first 2 calls, then success
        Func<Task<HttpResponseMessage>> action = async () =>
        {
            await Task.CompletedTask;
            callCount++;
            if (callCount < 3)
            {
                var failResponse = new DisposalTrackingResponse(HttpStatusCode.ServiceUnavailable, handler);
                return failResponse;
            }
            else
            {
                var successResponse = new DisposalTrackingResponse(HttpStatusCode.OK, handler);
                return successResponse;
            }
        };

        // Act
        var result = await HttpRetryPolicy.WaitForSuccessAsync(
            action,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100));

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessStatusCode);
        Assert.Equal(3, callCount);
        Assert.Equal(2, handler.DisposalCount); // Only the 2 non-success responses should be disposed

        // Clean up the returned response
        result.Dispose();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task WaitForSuccessAsync_WithCancellation_StopsRetrying()
    {
        // Arrange
        var handler = new DisposalTrackingHandler(HttpStatusCode.InternalServerError);
        using var client = new HttpClient(handler);
        using var cts = new CancellationTokenSource();

        // Act
        var task = HttpRetryPolicy.WaitForSuccessAsync(
            () => client.GetAsync("http://localhost/test"),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMilliseconds(200),
            cts.Token);

        // Cancel after a short delay
        await Task.Delay(50);
        cts.Cancel();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
        
        // Should have disposed all attempted responses
        Assert.Equal(handler.CallCount, handler.DisposalCount);
    }
}
