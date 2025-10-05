using System.Threading;
using System.Threading.Tasks;
using RunnerTasks.Tests.Fakes;
using Xunit;

namespace RunnerTasks.Tests
{
    public class DockerOneOffTests
    {
        [Fact]
        public async Task RunOneOffContainerAsync_WithFakeClient_ReturnsTrue()
        {
            var fake = new FakeDockerClientWrapper();
            // ensure there's no images; the code will call CreateImageAsync which is a no-op
            var svc = new DockerDotNetRunnerService(".", fake, new TestLogger<DockerDotNetRunnerService>());

            var ok = await svc.RunOneOffContainerAsync("alpine:latest", new[] { "FOO=BAR" }, new[] { "echo", "hi" }, CancellationToken.None);
            Assert.True(ok);
        }
    }
}
