using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RunnerTasks;

namespace RunnerTasks.Tests
{
    public class DockerUnregisterTests
    {
        [Fact]
        public async Task UnregisterAsync_WhenExecSucceeds_StopsContainerAndClearsToken()
        {
            var fake = new RunnerTasks.Tests.Fakes.FakeDockerClientWrapper();
            var svc = new DockerDotNetRunnerService(".", fake, null);

            // simulate that a container was created and token stored
            var create = await fake.CreateContainerAsync(new Docker.DotNet.Models.CreateContainerParameters { Image = "img" }, CancellationToken.None);
            // set internal state via test helper
            svc.Test_SetInternalState(create.ID, "token123");

            var ok = await svc.UnregisterAsync(CancellationToken.None);

            Assert.True(ok);
            Assert.True(fake.StopCalled, "Expected StopContainerAsync to be called on fake");
        }

        [Fact]
        public async Task UnregisterAsync_WhenExecCreateThrows_StillClearsTokenAndReturnsTrue()
        {
            var fake = new RunnerTasks.Tests.Fakes.FakeDockerClientWrapperThrowsExecCreate();
            var svc = new DockerDotNetRunnerService(".", fake, null);

            var create = await fake.CreateContainerAsync(new Docker.DotNet.Models.CreateContainerParameters { Image = "img" }, CancellationToken.None);
            svc.Test_SetInternalState(create.ID, "token123");

            var ok = await svc.UnregisterAsync(CancellationToken.None);

            Assert.True(ok);
            // token should be cleared
            var state = svc.Test_GetInternalState();
            Assert.Null(state.lastRegistrationToken);
        }
    }
}
