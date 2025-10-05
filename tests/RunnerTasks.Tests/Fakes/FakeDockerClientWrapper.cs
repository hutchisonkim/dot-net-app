using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace RunnerTasks.Tests.Fakes
{
    public class FakeDockerClientWrapper : IDockerClientWrapper
    {
        public IList<ImagesListResponse> Images { get; } = new List<ImagesListResponse>();
        public List<string> CreatedContainers { get; } = new List<string>();
        public bool StopCalled { get; private set; }

    public virtual Task CreateImageAsync(ImagesCreateParameters parameters, AuthConfig? authConfig, IProgress<JSONMessage> progress, CancellationToken cancellationToken)
        {
            // simulate success
            return Task.CompletedTask;
        }

    public virtual Task<IList<ImagesListResponse>> ListImagesAsync(ImagesListParameters parameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(Images);
        }

    public virtual Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters, CancellationToken cancellationToken)
        {
            var id = "fake-" + Guid.NewGuid().ToString("N");
            CreatedContainers.Add(id);
            return Task.FromResult(new CreateContainerResponse { ID = id });
        }

    public virtual Task<bool> StartContainerAsync(string id, ContainerStartParameters parameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public virtual Task<dynamic> GetContainerLogsAsync(string id, ContainerLogsParameters parameters, CancellationToken cancellationToken)
        {
            // return a stream that contains a small message
            var ms = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));
            return Task.FromResult<dynamic>(ms);
        }

    public virtual Task RemoveContainerAsync(string id, ContainerRemoveParameters parameters, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

    public virtual Task<ContainerExecCreateResponse> ExecCreateAsync(string containerId, ContainerExecCreateParameters parameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ContainerExecCreateResponse { ID = "exec-" + Guid.NewGuid().ToString("N") });
        }

        public virtual Task<dynamic> StartAndAttachExecAsync(string execId, bool hijack, CancellationToken cancellationToken)
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes("exec output"));
            return Task.FromResult<dynamic>(ms);
        }

    public virtual Task<ContainerExecInspectResponse> InspectExecAsync(string execId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ContainerExecInspectResponse { ExitCode = 0 });
        }

    public virtual Task<ContainerInspectResponse> InspectContainerAsync(string id, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ContainerInspectResponse { State = new ContainerState { Running = true } });
        }

        public virtual Task StopContainerAsync(string id, ContainerStopParameters parameters, CancellationToken cancellationToken)
        {
            StopCalled = true;
            return Task.CompletedTask;
        }

        public virtual Task StopContainerAsync(string id, CancellationToken cancellationToken)
        {
            StopCalled = true;
            return Task.CompletedTask;
        }

        public virtual Task CreateVolumeAsync(VolumesCreateParameters parameters, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        

        public virtual Task RemoveVolumeAsync(string name, bool force)
        {
            return Task.CompletedTask;
        }

    public virtual Task RemoveVolumeAsync(string name, bool force, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
