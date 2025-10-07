using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace GitHub.RunnerTasks
{
    /// <summary>
    /// Lightweight Docker.DotNet implementation: finds a docker image (built by docker-compose), creates a container
    /// with the given env, streams logs into the provided logger, and performs registration by starting a one-off
    /// container to run the configure script.
    /// Note: This is intended for gated integration tests only.
    /// </summary>
    public partial class DockerDotNetRunnerService : IRunnerService
    {
        private readonly string _workingDirectory;
        private readonly ILogger<DockerDotNetRunnerService>? _logger;
        private readonly DockerClient _client;
        private readonly IDockerClientWrapper? _clientWrapper;
    private string? _imageTagInUse = null;
    private string? _containerId = null;
    private string? _createdVolumeName = null;
    private readonly TimeSpan _containerStartTimeout = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _logWaitTimeout = TimeSpan.FromSeconds(120);
        private string? _lastRegistrationToken;

        public DockerDotNetRunnerService(string workingDirectory, ILogger<DockerDotNetRunnerService>? logger = null)
        {
            _workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
            _logger = logger;

            var dockerUri = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? new Uri("npipe://./pipe/docker_engine")
                : new Uri("unix:///var/run/docker.sock");

            _client = new DockerClientConfiguration(dockerUri).CreateClient();
            _clientWrapper = new DockerClientWrapper(_client);
        }

        // NOTE: CLI-specific helpers and fallback behavior have been removed from this DotNet-based implementation.
        // There is a separate DockerCliRunnerService class that implements CLI behavior.

        // For tests: allow injecting a custom wrapper
        public DockerDotNetRunnerService(string workingDirectory, IDockerClientWrapper clientWrapper, ILogger<DockerDotNetRunnerService>? logger = null)
        {
            _workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
            _logger = logger;
            var dockerUri = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? new Uri("npipe://./pipe/docker_engine")
                : new Uri("unix:///var/run/docker.sock");
            _client = new DockerClientConfiguration(dockerUri).CreateClient();
            _clientWrapper = clientWrapper ?? throw new ArgumentNullException(nameof(clientWrapper));
        }

        public async Task<bool> StartContainersAsync(string[] envVars, CancellationToken cancellationToken)
        {
            var projectName = Path.GetFileName(_workingDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var guess = $"{projectName}_github-runner";
            var guessAlt = $"{projectName}-github-runner";
            CreateContainerParameters? createParams = null;

            try
            {
                Console.WriteLine($"Searching for image matching: {guess} or {guessAlt}");
                _logger?.LogInformation("Searching for image matching {Guess}", guess);
                var images = _clientWrapper != null
                    ? await _clientWrapper.ListImagesAsync(new ImagesListParameters { All = true }, cancellationToken).ConfigureAwait(false)
                    : await _client.Images.ListImagesAsync(new ImagesListParameters { All = true }, cancellationToken).ConfigureAwait(false);
                var found = images.FirstOrDefault(i => (i.RepoTags ?? Array.Empty<string>()).Any(t => t.StartsWith(guess, StringComparison.OrdinalIgnoreCase) || t.StartsWith(guessAlt, StringComparison.OrdinalIgnoreCase) || t.Contains("github-runner", StringComparison.OrdinalIgnoreCase)));
                Console.WriteLine($"Found images count: {images?.Count ?? 0}");
                if (found != null)
                {
                    var tag = (found.RepoTags ?? Array.Empty<string>()).First();
                    Console.WriteLine($"Using image tag: {tag}");
                    _imageTagInUse = tag;

                    // Create a dedicated volume and mount it to both /runner and /actions-runner to mirror docker-compose
                    var volumeName = "runner_data_" + Guid.NewGuid().ToString("n");
                    _createdVolumeName = volumeName;
                    try
                    {
                        if (_clientWrapper != null) await _clientWrapper.CreateVolumeAsync(new VolumesCreateParameters { Name = volumeName }, cancellationToken).ConfigureAwait(false);
                        else await _client.Volumes.CreateAsync(new VolumesCreateParameters { Name = volumeName }, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: failed to create volume {volumeName}: {ex.Message}");
                        _logger?.LogWarning(ex, "Failed to create volume {Volume}; proceeding without explicit volume", volumeName);
                        _createdVolumeName = null;
                    }

                    // Create container that stays alive (tail -f /dev/null) so we can exec into it programmatically
                    createParams = new CreateContainerParameters
                    {
                        Image = tag,
                        Name = $"runner-test-{Guid.NewGuid():N}",
                        Cmd = new[] { "tail", "-f", "/dev/null" },
                        Env = envVars?.ToList() ?? new System.Collections.Generic.List<string>(),
                        HostConfig = new HostConfig
                        {
                            AutoRemove = false, // we'll remove after stopping
                            Mounts = !string.IsNullOrEmpty(_createdVolumeName)
                                ? new System.Collections.Generic.List<Mount>
                                {
                                    new Mount { Type = "volume", Source = _createdVolumeName, Target = "/runner" },
                                    new Mount { Type = "volume", Source = _createdVolumeName, Target = "/actions-runner" }
                                }
                                : null
                        }
                    };

                    // container creation/start will be performed after image discovery (common path below)
                }
                else
                {
                    // No image found via Docker.DotNet -- this DotNet-based implementation does not fall back to the docker CLI.
                    _logger?.LogWarning("No docker image found matching {Guess} or {GuessAlt} via Docker.DotNet; not attempting CLI fallback in this implementation.", guess, guessAlt);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "StartContainersAsync failed using Docker.DotNet");
                return false;
            }

            if (createParams == null)
            {
                _logger?.LogError("No create parameters available for container creation");
                return false;
            }

            // Create the container
            CreateContainerResponse created;
            try
            {
                created = _clientWrapper != null
                    ? await _clientWrapper.CreateContainerAsync(createParams, cancellationToken).ConfigureAwait(false)
                    : await _client.Containers.CreateContainerAsync(createParams, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create container");
                return false;
            }

            _containerId = created.ID;
            _logger?.LogInformation("Created container with ID: {Id}", _containerId);

            // Start the container
            var started = _clientWrapper != null
                ? await _clientWrapper.StartContainerAsync(_containerId, new ContainerStartParameters(), cancellationToken).ConfigureAwait(false)
                : await _client.Containers.StartContainerAsync(_containerId, new ContainerStartParameters(), cancellationToken).ConfigureAwait(false);
            if (!started)
            {
                _logger?.LogError("Failed to start container {Id}", _containerId);
                try { await _client.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters { Force = true }, cancellationToken).ConfigureAwait(false); } catch { }
                _containerId = null;
                return false;
            }

            // Wait briefly for the container to reach running state
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.Elapsed < _containerStartTimeout)
                {
                    var inspect = _clientWrapper != null
                        ? await _clientWrapper.InspectContainerAsync(_containerId, cancellationToken).ConfigureAwait(false)
                        : await _client.Containers.InspectContainerAsync(_containerId, cancellationToken).ConfigureAwait(false);
                    if (inspect.State != null && inspect.State.Running)
                    {
                        break;
                    }
                    await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Error while waiting for container running state");
            }

            return true;
        }

        public async Task<bool> RegisterAsync(string token, string ownerRepo, string githubUrl, CancellationToken cancellationToken)
        {
            _lastRegistrationToken = token;
            if (string.IsNullOrEmpty(_imageTagInUse))
            {
                // No tracked image available via Docker.DotNet -- run a one-off container to execute registration.
                _logger?.LogInformation("No image tag set; falling back to RunOneOffContainerAsync for registration");
                try
                {
                    var env = new[] { $"GITHUB_TOKEN={token}", $"GITHUB_URL={githubUrl}", $"GITHUB_REPOSITORY={ownerRepo}" };
                    // Use the same image name used by the CLI implementation for parity
                    var image = "github-self-hosted-runner-docker-github-runner:latest";
                    var cmd = new[] { "/usr/local/bin/configure-runner.sh" };
                    var ok = await RunOneOffContainerAsync(image, env, cmd, cancellationToken).ConfigureAwait(false);
                    return ok;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "RunOneOffContainerAsync fallback failed during RegisterAsync");
                    return false;
                }
            }

            // Exec configure script inside the running container (created by StartContainersAsync)
            if (string.IsNullOrEmpty(_containerId))
            {
                _logger?.LogError("No running container to exec into for registration");
                return false;
            }

            var runnerUrl = (githubUrl ?? "").TrimEnd('/') + "/" + ownerRepo;
            var runnerName = "runner-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var labelsFromEnv = Environment.GetEnvironmentVariable("RUNNER_LABELS");
            if (string.IsNullOrWhiteSpace(labelsFromEnv)) labelsFromEnv = "self-hosted"; // default label

            var envList = new System.Collections.Generic.List<string>
            {
                $"GITHUB_TOKEN={token}",
                $"GITHUB_URL={githubUrl}",
                $"GITHUB_REPOSITORY={ownerRepo}",
                $"RUNNER_LABELS={labelsFromEnv}"
            };

                var configArgs = new System.Collections.Generic.List<string>
            {
                "/actions-runner/config.sh",
                "--url",
                runnerUrl,
                "--token",
                token,
                "--name",
                runnerName,
                    "--labels",
                    labelsFromEnv,
                "--work",
                "_work",
                "--ephemeral"
            };

            try
            {
                var execCreate = _clientWrapper != null
                    ? await _clientWrapper.ExecCreateAsync(_containerId, new ContainerExecCreateParameters
                    {
                        AttachStdout = true,
                        AttachStderr = true,
                        User = "github-runner",
                        Cmd = configArgs.ToArray(),
                        Env = envList
                    }, cancellationToken).ConfigureAwait(false)
                    : await _client.Containers.ExecCreateContainerAsync(_containerId, new ContainerExecCreateParameters
                    {
                        AttachStdout = true,
                        AttachStderr = true,
                        User = "github-runner",
                        Cmd = configArgs.ToArray(),
                        Env = envList
                    }, cancellationToken).ConfigureAwait(false);

                using (var stream = _clientWrapper != null
                    ? await _clientWrapper.StartAndAttachExecAsync(execCreate.ID, false, cancellationToken).ConfigureAwait(false)
                    : (dynamic)await _client.Containers.StartAndAttachContainerExecAsync(execCreate.ID, false, cancellationToken).ConfigureAwait(false))
                {
                    var buffer = new byte[1024];
                    try
                    {
                        while (true)
                        {
                            var type = stream.GetType();
                            var hasReadOutput = type.GetMethod("ReadOutputAsync", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(CancellationToken) }) != null;
                            if (hasReadOutput)
                            {
                                dynamic d = stream;
                                var res = await d.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
                                bool eof = false; int count = 0;
                                try { eof = (bool)res.EOF; } catch { }
                                try { count = (int)res.Count; } catch { try { count = (int)((long)res.Count); } catch { } }
                                if (eof) break;
                                if (count > 0)
                                {
                                    try { var s = Encoding.UTF8.GetString(buffer, 0, count); var trimmed = s.TrimEnd(); if (_logger != null) Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_logger, "[configure exec] {Line}", trimmed); } catch { }
                                }
                            }
                            else
                            {
                                var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                                if (read == 0) break;
                                try { var s = Encoding.UTF8.GetString(buffer, 0, read); var trimmed = s.TrimEnd(); if (_logger != null) Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_logger, "[configure exec] {Line}", trimmed); } catch { }
                            }
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception exStream)
                    {
                        _logger?.LogDebug(exStream, "Error streaming configure exec output");
                    }
                }

                // Exec the runner process (run.sh) directly as github-runner to start the listener
                var startExec = _clientWrapper != null
                    ? await _clientWrapper.ExecCreateAsync(_containerId, new ContainerExecCreateParameters
                    {
                        AttachStdout = true,
                        AttachStderr = true,
                        User = "github-runner",
                        Cmd = new[] { "/actions-runner/run.sh" }
                    }, cancellationToken).ConfigureAwait(false)
                    : await _client.Containers.ExecCreateContainerAsync(_containerId, new ContainerExecCreateParameters
                    {
                        AttachStdout = true,
                        AttachStderr = true,
                        User = "github-runner",
                        Cmd = new[] { "/actions-runner/run.sh" }
                    }, cancellationToken).ConfigureAwait(false);

                // Attach to the start exec so we can surface startup errors quickly and stream logs
                using (var startStreamObj = _clientWrapper != null
                    ? await _clientWrapper.StartAndAttachExecAsync(startExec.ID, false, cancellationToken).ConfigureAwait(false)
                    : (dynamic)await _client.Containers.StartAndAttachContainerExecAsync(startExec.ID, false, cancellationToken).ConfigureAwait(false))
                {
                    var buffer = new byte[1024];
                    var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var startType = startStreamObj.GetType();
                            var hasReadOutput = startType.GetMethod("ReadOutputAsync", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(CancellationToken) }) != null;
                            dynamic dstart = startStreamObj;
                            if (hasReadOutput)
                            {
                                while (true)
                                {
                                    var res = await dstart.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
                                    bool eof = false; int count = 0;
                                    try { eof = (bool)res.EOF; } catch { }
                                    try { count = (int)res.Count; } catch { try { count = (int)((long)res.Count); } catch { } }
                                    if (eof) break;
                                    if (count > 0) { try { var s = Encoding.UTF8.GetString(buffer, 0, count); var trimmed = s.TrimEnd(); if (_logger != null) Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_logger, "[start exec] {Line}", trimmed); } catch { } }
                                }
                            }
                            else
                            {
                                while (true)
                                {
                                    var read = await dstart.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                                    int r = (int)read;
                                    if (r == 0) break;
                                    try { var s = Encoding.UTF8.GetString(buffer, 0, r); var trimmed = s.TrimEnd(); if (_logger != null) Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_logger, "[start exec] {Line}", trimmed); } catch { }
                                }
                            }
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex)
                        {
                            _logger?.LogDebug(ex, "Error streaming start exec output");
                        }
                        finally { tcs.TrySetResult(true); }
                    }, cancellationToken);

                    // Wait briefly for the start exec to produce output or complete
                    await Task.WhenAny(tcs.Task, Task.Delay(2000, cancellationToken)).ConfigureAwait(false);
                }

                // Inspect the start exec result to detect immediate failures (non-zero exit code) and monitor container logs for 'Listening for Jobs'
                try
                {
                    var execInspect = _clientWrapper != null
                        ? await _clientWrapper.InspectExecAsync(startExec.ID, cancellationToken).ConfigureAwait(false)
                        : await _client.Containers.InspectContainerExecAsync(startExec.ID, cancellationToken).ConfigureAwait(false);
                    if (execInspect != null)
                    {
                        try
                        {
                            var exit = execInspect.ExitCode;
                            if (exit != 0)
                            {
                                _logger?.LogWarning("run.sh exec returned non-zero exit code: {Code}", exit);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogDebug(ex, "Error reading exec inspect ExitCode");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Could not inspect run exec");
                }

                // Monitor container logs for 'Listening for Jobs' to confirm runner started
                var foundListening = false;
                var tailParams = new ContainerLogsParameters { ShowStdout = true, ShowStderr = true, Follow = true, Tail = "all" };
                using (var logsObj = _clientWrapper != null
                    ? await _clientWrapper.GetContainerLogsAsync(_containerId, tailParams, cancellationToken).ConfigureAwait(false)
                    : await _client.Containers.GetContainerLogsAsync(_containerId, tailParams, cancellationToken).ConfigureAwait(false))
                {
                    var buffer = new byte[1024];
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    dynamic dlogs = logsObj;
                    var logsType = logsObj.GetType();
                    var hasReadOutput = logsType.GetMethod("ReadOutputAsync", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(CancellationToken) }) != null;
                    while (sw.Elapsed < _logWaitTimeout)
                    {
                        if (hasReadOutput)
                        {
                            var res = await dlogs.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
                            int count = 0; bool eof = false;
                            try { count = (int)res.Count; } catch { try { count = (int)((long)res.Count); } catch { } }
                            try { eof = (bool)res.EOF; } catch { }
                            if (count > 0)
                            {
                                var s = Encoding.UTF8.GetString(buffer, 0, count);
                                { var trimmed = s.TrimEnd(); if (_logger != null) Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_logger, "[container logs] {Line}", trimmed); }
                                if (s.IndexOf("Listening for Jobs", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    foundListening = true;
                                    break;
                                }
                            }
                            else if (eof)
                            {
                                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            var read = await dlogs.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                            int r = (int)read;
                            if (r > 0)
                            {
                                var s = Encoding.UTF8.GetString(buffer, 0, r);
                                { var trimmed = s.TrimEnd(); if (_logger != null) Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_logger, "[container logs] {Line}", trimmed); }
                                if (s.IndexOf("Listening for Jobs", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    foundListening = true;
                                    break;
                                }
                            }
                            else
                            {
                                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                }

                if (!foundListening)
                {
                    _logger?.LogWarning("Did not see 'Listening for Jobs' in container logs within timeout");
                }

                return foundListening;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Registration exec path failed using Docker.DotNet");
                return false;
            }
        }

        public async Task<bool> RunOneOffContainerAsync(string image, string[]? env, string[] cmd, CancellationToken cancellationToken)
        {
            try
            {
                // Ensure image exists (try to pull if not present)
                try
                {
                    var imgs = _clientWrapper != null
                        ? await _clientWrapper.ListImagesAsync(new ImagesListParameters { All = true }, cancellationToken).ConfigureAwait(false)
                        : await _client.Images.ListImagesAsync(new ImagesListParameters { All = true }, cancellationToken).ConfigureAwait(false);
                    var found = imgs.FirstOrDefault(i => (i.RepoTags ?? Array.Empty<string>()).Any(t => string.Equals(t, image, StringComparison.OrdinalIgnoreCase)));
                    if (found == null)
                    {
                        // parse image:tag
                        var parts = image.Split(':');
                        var fromImage = parts[0];
                        var tag = parts.Length > 1 ? parts[1] : "latest";
                        try
                        {
                            if (_clientWrapper != null)
                            {
                                await _clientWrapper.CreateImageAsync(new ImagesCreateParameters { FromImage = fromImage, Tag = tag }, null, new Progress<JSONMessage>(), cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                await _client.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = fromImage, Tag = tag }, null, new Progress<JSONMessage>(), cancellationToken).ConfigureAwait(false);
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                var name = $"oneoff-{Guid.NewGuid():N}";
                var createParams = new CreateContainerParameters
                {
                    Image = image,
                    Name = name,
                    Cmd = cmd,
                    Env = env?.ToList() ?? new System.Collections.Generic.List<string>(),
                    HostConfig = new HostConfig { AutoRemove = true }
                };

                var created = _clientWrapper != null
                    ? await _clientWrapper.CreateContainerAsync(createParams, cancellationToken).ConfigureAwait(false)
                    : await _client.Containers.CreateContainerAsync(createParams, cancellationToken).ConfigureAwait(false);
                var started = _clientWrapper != null
                    ? await _clientWrapper.StartContainerAsync(created.ID, new ContainerStartParameters(), cancellationToken).ConfigureAwait(false)
                    : await _client.Containers.StartContainerAsync(created.ID, new ContainerStartParameters(), cancellationToken).ConfigureAwait(false);
                if (!started)
                {
                    try { await _client.Containers.RemoveContainerAsync(created.ID, new ContainerRemoveParameters { Force = true }, cancellationToken).ConfigureAwait(false); } catch { }
                    return false;
                }

                // Attach to logs until complete
                using var stream = _clientWrapper != null
                    ? await _clientWrapper.GetContainerLogsAsync(created.ID, new ContainerLogsParameters { ShowStdout = true, ShowStderr = true, Follow = false, Tail = "all" }, cancellationToken).ConfigureAwait(false)
                    : await _client.Containers.GetContainerLogsAsync(created.ID, new ContainerLogsParameters { ShowStdout = true, ShowStderr = true, Follow = false, Tail = "all" }, cancellationToken).ConfigureAwait(false);
                var buffer = new byte[4096];
                while (true)
                {
                    var type = stream.GetType();
                    var hasReadOutput = type.GetMethod("ReadOutputAsync", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(CancellationToken) }) != null;
                    if (hasReadOutput)
                    {
                        dynamic d = stream;
                        var res = await d.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
                        if ((bool)res.EOF) break;
                        var count = (int)res.Count;
                        if (count > 0) { var msg = System.Text.Encoding.UTF8.GetString(buffer, 0, count); if (_logger != null) Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_logger, "[oneoff] {Line}", msg); }
                    }
                    else
                    {
                        var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                        if (read == 0) break;
                        { var msg = System.Text.Encoding.UTF8.GetString(buffer, 0, read); if (_logger != null) Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_logger, "[oneoff] {Line}", msg); }
                    }
                }

                // Remove container if still present (AutoRemove may handle it)
                try { if (_clientWrapper != null) await _clientWrapper.RemoveContainerAsync(created.ID, new ContainerRemoveParameters { Force = true }, cancellationToken).ConfigureAwait(false); else await _client.Containers.RemoveContainerAsync(created.ID, new ContainerRemoveParameters { Force = true }, cancellationToken).ConfigureAwait(false); } catch { }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "RunOneOffContainerAsync failed");
                return false;
            }
        }

        // CLI fallbacks have been removed from this DotNet-based implementation.



        public async Task<bool> StopContainersAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_containerId))
            {
                // CLI fallback cleanup disabled for now — avoid invoking docker CLI from the DotNet runner path.
                _logger?.LogInformation("CLI fallback cleanup disabled in DockerDotNetRunnerService.StopContainersAsync; skipping docker CLI cleanup.");
                return true;
            }

            // Unregister asynchronously (separated API) - leave in place to call from OrchestrateStopAsync
            try
            {
                await UnregisterAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "UnregisterAsync failed during StopContainersAsync");
            }

            try
            {
                if (_clientWrapper != null)
                {
                    // use the wrapper when present so tests can inject fakes
                    await _clientWrapper.StopContainerAsync(_containerId, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await _client.Containers.StopContainerAsync(_containerId, new ContainerStopParameters { WaitBeforeKillSeconds = 5 }, cancellationToken).ConfigureAwait(false);
                }
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

            // Try remove created volume if we created one
            if (!string.IsNullOrEmpty(_createdVolumeName))
            {
                try
                {
                    if (_clientWrapper != null) await _clientWrapper.RemoveVolumeAsync(_createdVolumeName, true).ConfigureAwait(false);
                    else await _client.Volumes.RemoveAsync(_createdVolumeName, true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to remove created volume {Volume}", _createdVolumeName);
                }
                finally
                {
                    _createdVolumeName = null;
                }
            }

            return true;
        }

        public async Task<bool> UnregisterAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_lastRegistrationToken) || string.IsNullOrEmpty(_containerId)) return true;

            try
            {
                var execParams = new ContainerExecCreateParameters
                {
                    AttachStdout = true,
                    AttachStderr = true,
                    User = "github-runner",
                    Cmd = new[] { "/actions-runner/config.sh", "remove", "--unattended", "--token", _lastRegistrationToken },
                    Env = new System.Collections.Generic.List<string> { $"GITHUB_TOKEN={_lastRegistrationToken}" }
                };

                ContainerExecCreateResponse exec;
                if (_clientWrapper != null)
                {
                    exec = await _clientWrapper.ExecCreateAsync(_containerId, execParams, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    exec = await _client.Containers.ExecCreateContainerAsync(_containerId, execParams, cancellationToken).ConfigureAwait(false);
                }

                using var streamObj = _clientWrapper != null
                    ? await _clientWrapper.StartAndAttachExecAsync(exec.ID, false, cancellationToken).ConfigureAwait(false)
                    : (dynamic)await _client.Containers.StartAndAttachContainerExecAsync(exec.ID, false, cancellationToken).ConfigureAwait(false);

                var buffer = new byte[1024];
                try
                {
                    while (true)
                    {
                        var type = streamObj.GetType();
                        var hasReadOutput = type.GetMethod("ReadOutputAsync", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(CancellationToken) }) != null;
                        dynamic dstream = streamObj;
                        if (hasReadOutput)
                        {
                            var res = await dstream.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                            bool eof = false; int count = 0;
                            try { eof = (bool)res.EOF; } catch { }
                            try { count = (int)res.Count; } catch { try { count = (int)((long)res.Count); } catch { } }
                            if (count > 0)
                            {
                                var s = Encoding.UTF8.GetString(buffer, 0, count);
                                { var trimmed = s.TrimEnd(); if (_logger != null) Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_logger, "[unregister exec] {Line}", trimmed); }
                            }
                            if (eof) break;
                        }
                        else
                        {
                            var read = await dstream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                            int r = (int)read;
                            if (r == 0) break;
                            var s = Encoding.UTF8.GetString(buffer, 0, r);
                            { var trimmed = s.TrimEnd(); if (_logger != null) Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_logger, "[unregister exec] {Line}", trimmed); }
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Error streaming unregister exec");
                }

                // Try to stop the container after unregister
                try
                {
                    if (_clientWrapper != null)
                    {
                        await _clientWrapper.StopContainerAsync(_containerId, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await _client.Containers.StopContainerAsync(_containerId, new ContainerStopParameters { WaitBeforeKillSeconds = 5 }, cancellationToken).ConfigureAwait(false);
                    }

                    _logger?.LogInformation("Stopped container {Id}", _containerId);
                    _containerId = null;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to run unregister script inside container {Id}", _containerId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create/start unregister exec using Docker.DotNet");
            }
            finally
            {
                _lastRegistrationToken = null;
            }

            return true;
        }

        // Test hooks moved to partial class in DockerDotNetRunnerService.TestHooks.cs to keep this file lean.

        // CLI helpers were intentionally removed from this DotNet-based implementation.
    }

}
