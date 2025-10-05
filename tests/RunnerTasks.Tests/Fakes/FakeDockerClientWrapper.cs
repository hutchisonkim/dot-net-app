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

        public Task CreateImageAsync(ImagesCreateParameters parameters, AuthConfig? authConfig, IProgress<JSONMessage> progress, CancellationToken cancellationToken)
        {
            // simulate success
            return Task.CompletedTask;
        }

        public Task<IList<ImagesListResponse>> ListImagesAsync(ImagesListParameters parameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(Images);
        }

        public Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters, CancellationToken cancellationToken)
        {
            var id = "fake-" + Guid.NewGuid().ToString("N");
            CreatedContainers.Add(id);
            return Task.FromResult(new CreateContainerResponse { ID = id });
        }

        public Task<bool> StartContainerAsync(string id, ContainerStartParameters parameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<Stream> GetContainerLogsAsync(string id, ContainerLogsParameters parameters, CancellationToken cancellationToken)
        {
            // return a stream that contains a small message
            var ms = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));
            return Task.FromResult<Stream>(ms);
        }

        public Task RemoveContainerAsync(string id, ContainerRemoveParameters parameters, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
