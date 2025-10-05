using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace RunnerTasks
{
    public interface IDockerClientWrapper
    {
        Task<IList<ImagesListResponse>> ListImagesAsync(ImagesListParameters parameters, CancellationToken cancellationToken);
        Task CreateImageAsync(ImagesCreateParameters parameters, AuthConfig? authConfig, IProgress<JSONMessage> progress, CancellationToken cancellationToken);
        Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters, CancellationToken cancellationToken);
        Task<bool> StartContainerAsync(string id, ContainerStartParameters parameters, CancellationToken cancellationToken);
        Task<dynamic> GetContainerLogsAsync(string id, ContainerLogsParameters parameters, CancellationToken cancellationToken);
        Task RemoveContainerAsync(string id, ContainerRemoveParameters parameters, CancellationToken cancellationToken);
        Task<ContainerExecCreateResponse> ExecCreateAsync(string containerId, ContainerExecCreateParameters parameters, CancellationToken cancellationToken);
        Task<dynamic> StartAndAttachExecAsync(string execId, bool hijack, CancellationToken cancellationToken);
        Task<ContainerExecInspectResponse> InspectExecAsync(string execId, CancellationToken cancellationToken);
        Task<ContainerInspectResponse> InspectContainerAsync(string id, CancellationToken cancellationToken);
        Task StopContainerAsync(string id, ContainerStopParameters parameters, CancellationToken cancellationToken);
        Task StopContainerAsync(string id, CancellationToken cancellationToken);
        Task CreateVolumeAsync(VolumesCreateParameters parameters, CancellationToken cancellationToken);
        Task RemoveVolumeAsync(string name, bool force, CancellationToken cancellationToken);
        Task RemoveVolumeAsync(string name, bool force);
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

        public async Task<dynamic> GetContainerLogsAsync(string id, ContainerLogsParameters parameters, CancellationToken cancellationToken)
        {
            return await _client.Containers.GetContainerLogsAsync(id, parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task RemoveContainerAsync(string id, ContainerRemoveParameters parameters, CancellationToken cancellationToken)
        {
            await _client.Containers.RemoveContainerAsync(id, parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ContainerExecCreateResponse> ExecCreateAsync(string containerId, ContainerExecCreateParameters parameters, CancellationToken cancellationToken)
        {
            return await _client.Containers.ExecCreateContainerAsync(containerId, parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task<dynamic> StartAndAttachExecAsync(string execId, bool hijack, CancellationToken cancellationToken)
        {
            return await _client.Containers.StartAndAttachContainerExecAsync(execId, hijack, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ContainerExecInspectResponse> InspectExecAsync(string execId, CancellationToken cancellationToken)
        {
            return await _client.Containers.InspectContainerExecAsync(execId, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ContainerInspectResponse> InspectContainerAsync(string id, CancellationToken cancellationToken)
        {
            return await _client.Containers.InspectContainerAsync(id, cancellationToken).ConfigureAwait(false);
        }

        public async Task StopContainerAsync(string id, ContainerStopParameters parameters, CancellationToken cancellationToken)
        {
            await _client.Containers.StopContainerAsync(id, parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task StopContainerAsync(string id, CancellationToken cancellationToken)
        {
            await _client.Containers.StopContainerAsync(id, new ContainerStopParameters { WaitBeforeKillSeconds = 5 }, cancellationToken).ConfigureAwait(false);
        }

        public async Task CreateVolumeAsync(VolumesCreateParameters parameters, CancellationToken cancellationToken)
        {
            await _client.Volumes.CreateAsync(parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task RemoveVolumeAsync(string name, bool force, CancellationToken cancellationToken)
        {
            await _client.Volumes.RemoveAsync(name, force).ConfigureAwait(false);
        }

        public async Task RemoveVolumeAsync(string name, bool force)
        {
            await _client.Volumes.RemoveAsync(name, force).ConfigureAwait(false);
        }
    }
}
