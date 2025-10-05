using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace RunnerTasks.Tests.Fakes
{
    public class FakeDockerClientWrapper_StartReturnsFalse : FakeDockerClientWrapper
    {
        public override Task<bool> StartContainerAsync(string id, ContainerStartParameters parameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }
}
