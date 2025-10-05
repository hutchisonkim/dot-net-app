using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace RunnerTasks.Tests.Fakes
{
    public class FakeDockerClientWrapperThrowsExecCreate : FakeDockerClientWrapper
    {
        public override Task<ContainerExecCreateResponse> ExecCreateAsync(string containerId, ContainerExecCreateParameters parameters, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("exec create failed");
        }
    }
}
