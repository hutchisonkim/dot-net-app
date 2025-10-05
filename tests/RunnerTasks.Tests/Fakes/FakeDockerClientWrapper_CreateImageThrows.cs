using System;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace RunnerTasks.Tests.Fakes
{
    public class FakeDockerClientWrapper_CreateImageThrows : FakeDockerClientWrapper
    {
        public override Task CreateImageAsync(ImagesCreateParameters parameters, AuthConfig? authConfig, IProgress<JSONMessage> progress, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("create image failed");
        }
    }
}
