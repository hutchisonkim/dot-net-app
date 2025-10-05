using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace RunnerTasks.Tests.Fakes
{
    // exec inspect returns non-zero exit code, but logs stream contains 'Listening for Jobs'
    public class FakeDockerClientWrapper_ExecInspectNonZero_LogsListening : FakeDockerClientWrapper
    {
        public override Task<ContainerExecInspectResponse> InspectExecAsync(string execId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ContainerExecInspectResponse { ExitCode = 2 });
        }

        public override Task<Stream> StartAndAttachExecAsync(string execId, bool hijack, CancellationToken cancellationToken)
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes("some log... Listening for Jobs ...done"));
            return Task.FromResult<Stream>(ms);
        }
    }
}
