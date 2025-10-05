using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace RunnerTasks.Tests
{
    /// <summary>
    /// Lightweight Docker.DotNet implementation: finds a docker image (built by docker-compose), creates a container
    /// with the given env, streams logs into the provided logger, and performs registration by starting a one-off
    /// container to run the configure script.
    /// Note: This is intended for gated integration tests only.
    /// </summary>
    public class DockerDotNetRunnerService : IRunnerService
    {
        private readonly string _workingDirectory;
        private readonly ILogger<DockerDotNetRunnerService>? _logger;
        private readonly DockerClient _client;
        private string? _imageTagInUse;
        private string? _containerId;

        public DockerDotNetRunnerService(string workingDirectory, ILogger<DockerDotNetRunnerService>? logger = null)
        {
            _workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
            _logger = logger;

            var dockerUri = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? new Uri("npipe://./pipe/docker_engine")
                : new Uri("unix:///var/run/docker.sock");

            _client = new DockerClientConfiguration(dockerUri).CreateClient();
        }

        public async Task<bool> StartContainersAsync(string[] envVars, CancellationToken cancellationToken)
        {
            var projectName = Path.GetFileName(_workingDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var guess = $"{projectName}_github-runner";

            _logger?.LogInformation("Searching for image matching {Guess}", guess);
            var images = await _client.Images.ListImagesAsync(new ImagesListParameters { All = true }, cancellationToken).ConfigureAwait(false);
            var found = images.FirstOrDefault(i => (i.RepoTags ?? Array.Empty<string>()).Any(t => t.StartsWith(guess, StringComparison.OrdinalIgnoreCase)));
            if (found == null)
            {
                _logger?.LogError("No image found matching {Guess}. Did you run docker-compose build/up?", guess);
                return false;
            }

            var tag = (found.RepoTags ?? Array.Empty<string>()).First();
            _imageTagInUse = tag;

            var createParams = new CreateContainerParameters
            {
                Image = tag,
                Name = $"runner-test-{Guid.NewGuid():N}",
                Env = envVars?.ToList() ?? new System.Collections.Generic.List<string>(),
                HostConfig = new HostConfig { AutoRemove = true }
            };

            var created = await _client.Containers.CreateContainerAsync(createParams, cancellationToken).ConfigureAwait(false);
            _containerId = created.ID;

            var started = await _client.Containers.StartContainerAsync(_containerId, new ContainerStartParameters(), cancellationToken).ConfigureAwait(false);
            if (!started)
            {
                _logger?.LogError("Failed to start container {Id}", _containerId);
                return false;
            }

            var logParams = new ContainerLogsParameters { ShowStdout = true, ShowStderr = true, Follow = true, Tail = "all" };
            var stream = await _client.Containers.GetContainerLogsAsync(_containerId, logParams, cancellationToken).ConfigureAwait(false);

            // Create simple forwarding streams that write incoming bytes to the logger immediately.
            var stdoutForward = new ActionStream(b => {
                try { var s = Encoding.UTF8.GetString(b); _logger?.LogInformation("[container stdout] {Line}", s.TrimEnd()); } catch { }
            });
            var stderrForward = new ActionStream(b => {
                try { var s = Encoding.UTF8.GetString(b); _logger?.LogWarning("[container stderr] {Line}", s.TrimEnd()); } catch { }
            });

            _ = Task.Run(async () =>
            {
                var buffer = new byte[2048];
                try
                {
                    while (true)
                    {
                        int read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                        if (read == 0) break;
                        try
                        {
                            var s = Encoding.UTF8.GetString(buffer, 0, read);
                            _logger?.LogInformation("[container logs] {Line}", s.TrimEnd());
                        }
                        catch { }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { _logger?.LogDebug(ex, "Error streaming container logs"); }
                finally
                {
                    try { stdoutForward.Dispose(); } catch { }
                    try { stderrForward.Dispose(); } catch { }
                    try { stream.Dispose(); } catch { }
                }
            }, cancellationToken);

            return true;
        }

        public async Task<bool> RegisterAsync(string token, string ownerRepo, string githubUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_imageTagInUse))
            {
                _logger?.LogError("No image available to run registration");
                return false;
            }

            var createParams = new CreateContainerParameters
            {
                Image = _imageTagInUse,
                Name = $"runner-register-{Guid.NewGuid():N}",
                Cmd = new[] { "/usr/local/bin/configure-runner.sh" },
                Env = new System.Collections.Generic.List<string> { $"GITHUB_TOKEN={token}", $"GITHUB_URL={githubUrl}", $"GITHUB_REPOSITORY={ownerRepo}" },
                HostConfig = new HostConfig { AutoRemove = true }
            };

            var created = await _client.Containers.CreateContainerAsync(createParams, cancellationToken).ConfigureAwait(false);
            var id = created.ID;
            var started = await _client.Containers.StartContainerAsync(id, new ContainerStartParameters(), cancellationToken).ConfigureAwait(false);
            if (!started)
            {
                _logger?.LogError("Failed to start registration container {Id}", id);
                return false;
            }

            var logs = await _client.Containers.GetContainerLogsAsync(id, new ContainerLogsParameters { ShowStdout = true, ShowStderr = true, Follow = true }, cancellationToken).ConfigureAwait(false);
            var regStdout = new ActionStream(b => { try { var s = Encoding.UTF8.GetString(b); _logger?.LogInformation("[register stdout] {Line}", s.TrimEnd()); } catch { } });
            var regStderr = new ActionStream(b => { try { var s = Encoding.UTF8.GetString(b); _logger?.LogWarning("[register stderr] {Line}", s.TrimEnd()); } catch { } });
            try
            {
                var buffer = new byte[1024];
                while (true)
                {
                    int read = await logs.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    if (read == 0) break;
                    try
                    {
                        var s = Encoding.UTF8.GetString(buffer, 0, read);
                        _logger?.LogInformation("[register logs] {Line}", s.TrimEnd());
                    }
                    catch { }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error streaming registration logs");
            }
            finally
            {
                try { regStdout.Dispose(); } catch { }
                try { regStderr.Dispose(); } catch { }
                try { logs.Dispose(); } catch { }
            }

            var waited = await _client.Containers.WaitContainerAsync(id, cancellationToken).ConfigureAwait(false);
            _logger?.LogInformation("Registration container exited with status {Status}", waited.StatusCode);
            return waited.StatusCode == 0;
        }

        public async Task<bool> StopContainersAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_containerId)) return true;

            try
            {
                await _client.Containers.StopContainerAsync(_containerId, new ContainerStopParameters { WaitBeforeKillSeconds = 5 }, cancellationToken).ConfigureAwait(false);
            }
            catch (DockerApiException)
            {
                // already removed or docker error
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error stopping container {Id}", _containerId);
                return false;
            }

            _logger?.LogInformation("Stopped container {Id}", _containerId);
            _containerId = null;
            return true;
        }
    }

    // Minimal stream that writes incoming byte arrays to an Action<byte[]> then discards.
    // Docker.DotNet CopyOutputToAsync expects Streams for stdout/stderr; we provide a thin implementation.
    internal class ActionStream : System.IO.Stream
    {
        private readonly Action<byte[]> _onData;
        private bool _disposed;

        public ActionStream(Action<byte[]> onData)
        {
            _onData = onData ?? throw new ArgumentNullException(nameof(onData));
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, System.IO.SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ActionStream));
            if (count == 0) return;
            if (offset == 0 && count == buffer.Length)
            {
                _onData(buffer);
            }
            else
            {
                var tmp = new byte[count];
                Array.Copy(buffer, offset, tmp, 0, count);
                _onData(tmp);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
