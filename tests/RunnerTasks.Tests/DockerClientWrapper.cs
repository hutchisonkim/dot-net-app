using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace RunnerTasks.Tests
{
    public interface IDockerClientWrapper
    {
        Task<IList<ImagesListResponse>> ListImagesAsync(ImagesListParameters parameters, CancellationToken cancellationToken);
    Task CreateImageAsync(ImagesCreateParameters parameters, AuthConfig? authConfig, IProgress<JSONMessage> progress, CancellationToken cancellationToken);
        Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters, CancellationToken cancellationToken);
        Task<bool> StartContainerAsync(string id, ContainerStartParameters parameters, CancellationToken cancellationToken);
        Task<Stream> GetContainerLogsAsync(string id, ContainerLogsParameters parameters, CancellationToken cancellationToken);
        Task RemoveContainerAsync(string id, ContainerRemoveParameters parameters, CancellationToken cancellationToken);
    }

    public class DockerClientWrapper : IDockerClientWrapper
    {
        private readonly DockerClient _client;

        public DockerClientWrapper(DockerClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<IList<ImagesListResponse>> ListImagesAsync(ImagesListParameters parameters, CancellationToken cancellationToken)
        {
            return await _client.Images.ListImagesAsync(parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task CreateImageAsync(ImagesCreateParameters parameters, AuthConfig? authConfig, IProgress<JSONMessage> progress, CancellationToken cancellationToken)
        {
            await _client.Images.CreateImageAsync(parameters, authConfig, progress, cancellationToken).ConfigureAwait(false);
        }

        public async Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters, CancellationToken cancellationToken)
        {
            return await _client.Containers.CreateContainerAsync(parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> StartContainerAsync(string id, ContainerStartParameters parameters, CancellationToken cancellationToken)
        {
            return await _client.Containers.StartContainerAsync(id, parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Stream> GetContainerLogsAsync(string id, ContainerLogsParameters parameters, CancellationToken cancellationToken)
        {
            return await _client.Containers.GetContainerLogsAsync(id, parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task RemoveContainerAsync(string id, ContainerRemoveParameters parameters, CancellationToken cancellationToken)
        {
            await _client.Containers.RemoveContainerAsync(id, parameters, cancellationToken).ConfigureAwait(false);
        }
    }
}
