using System;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace GitHub.RunnerTasks.Tests.Fakes
{
    public class FakeDockerClientWrapper_CreateContainerThrows : FakeDockerClientWrapper
    {
        public override Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("create container failed");
        }
    }
}
