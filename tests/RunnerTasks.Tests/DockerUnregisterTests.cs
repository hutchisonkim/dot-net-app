using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

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
            // set internal state via reflection (since fields are private)
            var fi = typeof(DockerDotNetRunnerService).GetField("_containerId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fi.SetValue(svc, create.ID);
            var ft = typeof(DockerDotNetRunnerService).GetField("_lastRegistrationToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ft.SetValue(svc, "token123");

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
            var fi = typeof(DockerDotNetRunnerService).GetField("_containerId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fi.SetValue(svc, create.ID);
            var ft = typeof(DockerDotNetRunnerService).GetField("_lastRegistrationToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ft.SetValue(svc, "token123");

            var ok = await svc.UnregisterAsync(CancellationToken.None);

            Assert.True(ok);
            // token should be cleared
            var val = ft.GetValue(svc) as string;
            Assert.Null(val);
        }
    }
}
