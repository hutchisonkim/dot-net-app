using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace GitHub.RunnerTasks.Tests.Fakes
{
    public class FakeDockerClientWrapper_AttachEof : FakeDockerClientWrapper
    {
        public override Task<dynamic> StartAndAttachExecAsync(string execId, bool hijack, CancellationToken cancellationToken)
        {
            var ms = new MemoryStream(); // empty stream => immediate EOF
            return Task.FromResult<dynamic>(ms);
        }
    }
}
