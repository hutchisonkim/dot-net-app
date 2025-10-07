using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace GitHub.RunnerTasks.Tests.Fakes
{
    public class FakeDockerClientWrapper_RunExecDotnetNotFound : FakeDockerClientWrapper
    {
        private readonly string _message = "/actions-runner/run.sh: line 45: dotnet: not found\n";

        public override Task<ContainerExecCreateResponse> ExecCreateAsync(string containerId, ContainerExecCreateParameters parameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ContainerExecCreateResponse { ID = "exec-dotnet-notfound" });
        }

        public override Task<dynamic> StartAndAttachExecAsync(string execId, bool hijack, CancellationToken cancellationToken)
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(_message));
            return Task.FromResult<dynamic>(ms);
        }

        public override Task<ContainerExecInspectResponse> InspectExecAsync(string execId, CancellationToken cancellationToken)
        {
            // non-zero exit code to indicate failure
            return Task.FromResult(new ContainerExecInspectResponse { ExitCode = 127 });
        }

        public override Task<dynamic> GetContainerLogsAsync(string id, ContainerLogsParameters parameters, CancellationToken cancellationToken)
        {
            // Return logs that include the dotnet not found message
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(_message));
            return Task.FromResult<dynamic>(ms);
        }
    }
}
