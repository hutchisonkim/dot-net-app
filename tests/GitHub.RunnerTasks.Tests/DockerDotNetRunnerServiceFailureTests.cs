using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using Xunit;
using GitHub.RunnerTasks.Tests.Fakes;
using GitHub.RunnerTasks;

namespace GitHub.RunnerTasks.Tests
{
    public class DockerDotNetRunnerServiceFailureTests
    {
        [Fact]
        public void DockerClientWrapper_Constructor_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DockerClientWrapper(null!));
        }

        [Fact]
        public async Task StopContainersAsync_StopThrows_ReturnsFalse()
        {
            // fake that throws on stop
            var fake = new FakeStopThrows();
            var svc = new DockerDotNetRunnerService(".", fake, new TestLogger<DockerDotNetRunnerService>());
            svc.Test_SetInternalState("cid-1", "token");

            var result = await svc.StopContainersAsync(CancellationToken.None);

            Assert.False(result);
        }

        [Fact]
        public async Task UnregisterAsync_AttachReadError_StillClearsTokenAndStopsContainer()
        {
            var fake = new FakeAttachReadError();
            var svc = new DockerDotNetRunnerService(".", fake, new TestLogger<DockerDotNetRunnerService>());

            // create container via fake
            var create = await fake.CreateContainerAsync(new CreateContainerParameters { Image = "img" }, CancellationToken.None);
            svc.Test_SetInternalState(create.ID, "token123");

            var ok = await svc.UnregisterAsync(CancellationToken.None);

            Assert.True(ok);
            var state = svc.Test_GetInternalState();
            Assert.Null(state.lastRegistrationToken);
        }

        // helpers
        private class FakeStopThrows : FakeDockerClientWrapper
        {
            public override Task StopContainerAsync(string id, ContainerStopParameters parameters, CancellationToken cancellationToken)
            {
                throw new Exception("stop failed");
            }

            public override Task StopContainerAsync(string id, CancellationToken cancellationToken)
            {
                throw new Exception("stop failed");
            }
        }

        private class BadStream
        {
            public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                throw new Exception("read error");
            }
        }

        private class FakeAttachReadError : FakeDockerClientWrapper
        {
            public override Task<ContainerExecCreateResponse> ExecCreateAsync(string containerId, ContainerExecCreateParameters parameters, CancellationToken cancellationToken)
            {
                return Task.FromResult(new ContainerExecCreateResponse { ID = "exec-1" });
            }

            public override Task<dynamic> StartAndAttachExecAsync(string execId, bool hijack, CancellationToken cancellationToken)
            {
                return Task.FromResult<dynamic>(new BadStream());
            }

            public override Task<ContainerExecInspectResponse> InspectExecAsync(string execId, CancellationToken cancellationToken)
            {
                return Task.FromResult(new ContainerExecInspectResponse { ExitCode = 0 });
            }
        }
    }
}
