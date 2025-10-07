using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace GitHub.RunnerTasks.Tests.Fakes
{
    public class FakeDockerClientWrapper_VolumeCreateThrows : FakeDockerClientWrapper
    {
        public override Task CreateVolumeAsync(VolumesCreateParameters parameters, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("volume create failed");
        }
    }
}
