using System.Threading;
using System.Threading.Tasks;
using GitHub.RunnerTasks.Tests.Fakes;
using Xunit;
using GitHub.RunnerTasks;

namespace GitHub.RunnerTasks.Tests
{
    public class StopContainersTests
    {
        [Fact]
        public async Task StopContainersAsync_WithContainerAndVolume_RemovesVolumeAndStopsContainer()
        {
            var fake = new FakeDockerClientWrapper();
            var svc = new DockerDotNetRunnerService(".", fake, new TestLogger<DockerDotNetRunnerService>());

            // simulate internal state
            svc.Test_SetInternalState("fakeid", "token");
            svc.Test_SetCreatedVolumeName("runner_data_123");

            var ok = await svc.StopContainersAsync(CancellationToken.None);

            Assert.True(ok);
            Assert.True(fake.StopCalled, "Expected StopContainerAsync to be called on fake");
            Assert.Equal("runner_data_123", fake.LastRemovedVolume);
        }

        [Fact]
        public async Task StopContainersAsync_WhenNoContainer_ReturnsTrue()
        {
            var fake = new FakeDockerClientWrapper();
            var svc = new DockerDotNetRunnerService(".", fake, new TestLogger<DockerDotNetRunnerService>());

            // ensure no container
            svc.Test_SetInternalState(null, null);

            var ok = await svc.StopContainersAsync(CancellationToken.None);

            Assert.True(ok);
            Assert.False(fake.StopCalled);
        }
    }
}
