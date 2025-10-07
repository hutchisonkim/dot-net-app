using System.Threading;
using System.Threading.Tasks;
using Xunit;
using GitHub.Runner.Docker;

namespace GitHub.Runner.Docker.Tests
{
    public class StopContainersTests
    {
        [Fact]
        public async Task StopContainersAsync_WithMockService_DelegatesToService()
        {
            var fake = new FakeRunnerService(new[] { true });
            var manager = new RunnerManager(fake);
            var ok = await manager.OrchestrateStopAsync(CancellationToken.None);
            Assert.True(ok);
            Assert.Equal(1, fake.StopCallCount);
        }

        [Fact]
        public async Task StopContainersAsync_WhenNoOps_ReturnsTrue()
        {
            var fake = new FakeRunnerService(new[] { true });
            var manager = new RunnerManager(fake);
            var ok = await manager.OrchestrateStopAsync(CancellationToken.None);
            Assert.True(ok);
            Assert.Equal(1, fake.StopCallCount);
        }
    }
}
