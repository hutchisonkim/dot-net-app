using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    public class DockerDotNetRunnerService : IRunnerService
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

        private async Task<bool> TryBuildImageWithDockerCli(string tag, CancellationToken cancellationToken)
        {
            // Create a temp dir for docker build context
            var tmp = Path.Combine(Path.GetTempPath(), "runner-image-build-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            try
            {
                // Write a minimal Dockerfile that downloads and extracts the runner
                var lines = new[]
                {
                    "FROM ubuntu:20.04",
                    "RUN apt-get update && apt-get install -y curl ca-certificates tar gzip sudo openssl && rm -rf /var/lib/apt/lists/*",
                    "RUN useradd -m -s /bin/bash github-runner",
                    "WORKDIR /actions-runner",
                    "ARG RUNNER_VERSION=2.328.0",
                    "RUN curl -L -o actions-runner.tar.gz https://github.com/actions/runner/releases/download/v2.328.0/actions-runner-linux-x64-2.328.0.tar.gz && tar xzf actions-runner.tar.gz --strip-components=0 || true",
                    "USER github-runner",
                    "CMD [\"/bin/bash\", \"-c\", \"tail -f /dev/null\"]"
                };

                var dockerfile = string.Join(System.Environment.NewLine, lines);
                File.WriteAllText(Path.Combine(tmp, "Dockerfile"), dockerfile);

                // Run docker build and stream output live so we can see progress/errors as they happen
                var psi = new System.Diagnostics.ProcessStartInfo("docker", $"build --progress=plain -t {tag} .")
                {
                    WorkingDirectory = tmp,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null)
                {
                    Console.WriteLine("Failed to start 'docker' process. Is Docker installed and on PATH?");
                    return false;
                }

                var stdoutSb = new System.Text.StringBuilder();
                var stderrSb = new System.Text.StringBuilder();

                proc.OutputDataReceived += (s, e) => { if (e.Data != null) { stdoutSb.AppendLine(e.Data); Console.WriteLine(e.Data); } };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) { stderrSb.AppendLine(e.Data); Console.Error.WriteLine(e.Data); } };

                // Begin reading streams
                try
                {
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                }
                catch { /* best-effort */ }

                try
                {
                    await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    try { if (!proc.HasExited) proc.Kill(true); } catch { }
                    Console.WriteLine("Docker build was cancelled.");
                    return false;
                }

                if (proc.ExitCode != 0)
                {
                    Console.WriteLine("--- docker build stdout (summary) ---");
                    Console.WriteLine(stdoutSb.ToString());
                    Console.WriteLine("--- docker build stderr (summary) ---");
                    Console.WriteLine(stderrSb.ToString());
                    Console.WriteLine($"docker build exited with code {proc.ExitCode}");
                    return false;
                }

                Console.WriteLine($"Successfully built runner image {tag}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Exception during docker build");
                return false;
            }
            finally
            {
                try { Directory.Delete(tmp, true); } catch { }
            }
        }

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
                    Console.WriteLine("No image found via Docker.DotNet; attempting to build a minimal runner image via docker CLI");
                    _logger?.LogInformation("No image found via Docker.DotNet; attempting to build a minimal runner image via docker CLI");
                    // Attempt to build a minimal image programmatically via docker CLI
                    var wantedTag = "github-self-hosted-runner-docker-github-runner:latest";
                    try
                    {
                        var built = await TryBuildImageWithDockerCli(wantedTag, cancellationToken).ConfigureAwait(false);
                        Console.WriteLine($"TryBuildImageWithDockerCli returned: {built}");
                        if (!built)
                        {
                            _logger?.LogWarning("Failed to build runner image via docker CLI");
                            return false;
                        }

                        // Re-fetch images and find the newly built image
                        var images2 = _clientWrapper != null
                            ? await _clientWrapper.ListImagesAsync(new ImagesListParameters { All = true }, cancellationToken).ConfigureAwait(false)
                            : await _client.Images.ListImagesAsync(new ImagesListParameters { All = true }, cancellationToken).ConfigureAwait(false);
                        var found2 = images2.FirstOrDefault(i => (i.RepoTags ?? Array.Empty<string>()).Any(t => string.Equals(t, wantedTag, StringComparison.OrdinalIgnoreCase)));
                        if (found2 == null)
                        {
                            Console.WriteLine("Built image not present after build step");
                            _logger?.LogWarning("Built image not present after build step");
                            return false;
                        }

                        var tag = (found2.RepoTags ?? Array.Empty<string>()).First();
                        Console.WriteLine($"Found built image tag: {tag}");
                        _imageTagInUse = tag;

                        // prepare createParams for the built image
                        createParams = new CreateContainerParameters
                        {
                            Image = _imageTagInUse,
                            Name = $"runner-test-{Guid.NewGuid():N}",
                            Cmd = new[] { "tail", "-f", "/dev/null" },
                            Env = envVars?.ToList() ?? new System.Collections.Generic.List<string>(),
                            HostConfig = new HostConfig { AutoRemove = false }
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception while attempting to build runner image: {ex}");
                        _logger?.LogWarning(ex, "Exception while attempting to build runner image");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StartContainersAsync: Docker.DotNet path failed with exception: {ex}");
                _logger?.LogWarning(ex, "Docker.DotNet path failed; attempting docker CLI fallback");
                // Attempt to perform the same actions using the docker CLI as a fallback
                try
                {
                    var ok = await TryStartContainersWithDockerCli(envVars, cancellationToken).ConfigureAwait(false);
                    if (ok) return true;
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"StartContainersAsync: docker CLI fallback failed: {ex2}");
                    _logger?.LogWarning(ex2, "docker CLI fallback failed");
                }

                return false;
            }

            if (createParams == null)
            {
                _logger?.LogError("No create parameters available for container creation");
                return false;
            }

            // Create the container
            var created = _clientWrapper != null
                ? await _clientWrapper.CreateContainerAsync(createParams, cancellationToken).ConfigureAwait(false)
                : await _client.Containers.CreateContainerAsync(createParams, cancellationToken).ConfigureAwait(false);
            _containerId = created.ID;
            Console.WriteLine($"Created container with ID: {_containerId}");

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
                _logger?.LogWarning("No image available to run registration via Docker.DotNet; falling back to docker CLI");
                // Fallback: use docker CLI to run configure-runner.sh inside a container
                // Run a one-off container programmatically to execute the configure script
                return await RunOneOffContainerAsync("github-self-hosted-runner-docker-github-runner:latest",
                    new[] { $"GITHUB_TOKEN={token}", $"GITHUB_URL={githubUrl}", $"GITHUB_REPOSITORY={ownerRepo}" },
                    new[] { "/usr/local/bin/configure-runner.sh" }, cancellationToken).ConfigureAwait(false);
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
                _logger?.LogWarning(ex, "Registration exec path failed; attempting docker CLI exec into existing container or one-off container fallback");

                // If we have a container created via the docker CLI path, try to run the configure script using the docker CLI
                try
                {
                    if (!string.IsNullOrEmpty(_containerId))
                    {
                        // configArgs was defined earlier and contains the script path as the first element; pass the remaining args
                        var cliArgs = configArgs.Skip(1).ToArray();
                        var cliOk = await RunConfigureInContainerWithDockerCli(_containerId, cliArgs, cancellationToken).ConfigureAwait(false);
                        if (cliOk) return true;
                    }
                }
                catch (Exception ex2)
                {
                    _logger?.LogDebug(ex2, "docker CLI exec attempt failed; falling back to one-off container");
                }

                return await RunOneOffContainerAsync("github-self-hosted-runner-docker-github-runner:latest",
                    new[] { $"GITHUB_TOKEN={token}", $"GITHUB_URL={githubUrl}", $"GITHUB_REPOSITORY={ownerRepo}" },
                    new[] { "/usr/local/bin/configure-runner.sh" }, cancellationToken).ConfigureAwait(false);
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

        // Fallback: use docker CLI to create and start a container if Docker.DotNet fails
        private async Task<bool> TryStartContainersWithDockerCli(string[] envVars, CancellationToken cancellationToken)
        {
            // Determine image name to use
            var projectName = Path.GetFileName(_workingDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var wantedTag = "github-self-hosted-runner-docker-github-runner:latest";

            try
            {
                // Attempt to see if image exists locally via 'docker images'
                var psiCheck = new System.Diagnostics.ProcessStartInfo("docker", $"images -q {wantedTag}") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                using var pcheck = System.Diagnostics.Process.Start(psiCheck);
                if (pcheck == null) { Console.WriteLine("docker CLI not available"); return false; }
                var id = await pcheck.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                await pcheck.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(id))
                {
                    Console.WriteLine($"Image {wantedTag} not found locally -- attempting to build via docker CLI");
                    var built = await TryBuildImageWithDockerCli(wantedTag, cancellationToken).ConfigureAwait(false);
                    if (!built) return false;
                }

                // Create a container using docker run (detached) with a volume mount for runner data
                var volumeName = "runner_data_cli_" + Guid.NewGuid().ToString("n");
                Console.WriteLine($"Creating docker volume {volumeName}");
                var psiVol = new System.Diagnostics.ProcessStartInfo("docker", $"volume create {volumeName}") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                using (var pvol = System.Diagnostics.Process.Start(psiVol))
                {
                    if (pvol == null) { Console.WriteLine("Failed to start docker to create volume"); return false; }
                    var vout = await pvol.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                    await pvol.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                    Console.WriteLine(vout.Trim());
                }

                // Ensure RUNNER_LABELS is present in envVars (docker container expects it)
                var envListMutable = envVars?.ToList() ?? new System.Collections.Generic.List<string>();
                if (!envListMutable.Any(e => e.StartsWith("RUNNER_LABELS=", StringComparison.OrdinalIgnoreCase)))
                {
                    envListMutable.Add($"RUNNER_LABELS=self-hosted");
                }

                // Build docker run command
                var envArgs = string.Join(' ', envListMutable.Select(e => $"-e \"{e}\""));
                // Mount the same volume to both /actions-runner and /runner to match docker-compose behavior
                var runCmd = $"run -d --name runner-cli-{Guid.NewGuid():N} -v {volumeName}:/actions-runner -v {volumeName}:/runner {envArgs} {wantedTag} tail -f /dev/null";
                Console.WriteLine($"Running: docker {runCmd}");
                var psiRun = new System.Diagnostics.ProcessStartInfo("docker", runCmd) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                using (var prun = System.Diagnostics.Process.Start(psiRun))
                {
                    if (prun == null) { Console.WriteLine("Failed to start docker run process"); return false; }
                    var rout = await prun.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                    var rerr = await prun.StandardError.ReadToEndAsync().ConfigureAwait(false);
                    await prun.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                    if (prun.ExitCode != 0)
                    {
                        Console.WriteLine("docker run failed:");
                        Console.WriteLine(rerr);
                        return false;
                    }
                    _containerId = rout.Trim();
                    Console.WriteLine($"Started container via docker CLI: {_containerId}");
                    _createdVolumeName = volumeName;
                    _imageTagInUse = wantedTag;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TryStartContainersWithDockerCli exception: {ex}");
                return false;
            }
        }


        public async Task<bool> StopContainersAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_containerId))
            {
                // No tracked container from Docker.DotNet path; attempt to clean up any CLI-created containers/volumes
                try
                {
                    // Stop and remove containers named runner-cli-*
                    var psiList = new System.Diagnostics.ProcessStartInfo("docker", "ps -a --filter \"name=runner-cli\" --format \"{{.ID}}\"") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                    using (var pList = System.Diagnostics.Process.Start(psiList))
                    {
                        if (pList != null)
                        {
                            var outStr = await pList.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                            await pList.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                            var ids = outStr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                            foreach (var id in ids)
                            {
                                try
                                {
                                    var psiRm = new System.Diagnostics.ProcessStartInfo("docker", $"rm -f {id}") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                                    using var pRm = System.Diagnostics.Process.Start(psiRm);
                                    if (pRm != null)
                                    {
                                        var rout = await pRm.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                                        var rerr = await pRm.StandardError.ReadToEndAsync().ConfigureAwait(false);
                                        await pRm.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                                    }
                                }
                                catch { }
                            }
                        }
                    }

                    // Remove volumes named runner_data_cli_*
                    var psiVols = new System.Diagnostics.ProcessStartInfo("docker", "volume ls --format \"{{.Name}}\"") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                    using (var pVols = System.Diagnostics.Process.Start(psiVols))
                    {
                        if (pVols != null)
                        {
                            var vout = await pVols.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                            await pVols.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                            var vols = vout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.StartsWith("runner_data_cli_") || s.Equals("runner_data", StringComparison.OrdinalIgnoreCase) || s.StartsWith("github-self-hosted-runner-docker_runner_data")).ToArray();
                            foreach (var vol in vols)
                            {
                                try
                                {
                                    var psiVolRm = new System.Diagnostics.ProcessStartInfo("docker", $"volume rm {vol}") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                                    using var pVolRm = System.Diagnostics.Process.Start(psiVolRm);
                                    if (pVolRm != null)
                                    {
                                        var rout = await pVolRm.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                                        var rerr = await pVolRm.StandardError.ReadToEndAsync().ConfigureAwait(false);
                                        await pVolRm.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error during docker CLI cleanup of runner-cli containers/volumes");
                }

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
                _logger?.LogWarning(ex, "Failed to create/start unregister exec; attempting docker CLI unregister");
                try
                {
                    if (!string.IsNullOrEmpty(_containerId) && !string.IsNullOrEmpty(_lastRegistrationToken))
                    {
                        var args = new[] { "remove", "--unattended", "--token", _lastRegistrationToken };
                        var cliOk = await RunConfigureInContainerWithDockerCli(_containerId, args, cancellationToken).ConfigureAwait(false);
                        if (!cliOk) _logger?.LogWarning("docker CLI unregister returned false");
                    }
                }
                catch (Exception ex2)
                {
                    _logger?.LogDebug(ex2, "docker CLI unregister attempt failed");
                }
            }
            finally
            {
                _lastRegistrationToken = null;
            }

            return true;
        }

        // Test helpers: allow tests to set internal state without reflection
        public void Test_SetInternalState(string? containerId, string? lastRegistrationToken)
        {
            _containerId = containerId;
            _lastRegistrationToken = lastRegistrationToken;
        }

        public (string? containerId, string? lastRegistrationToken) Test_GetInternalState()
        {
            return (_containerId, _lastRegistrationToken);
        }

        public void Test_SetImageTag(string? tag)
        {
            _imageTagInUse = tag;
        }

        // Test helper: allow tests to set the created volume name so StopContainersAsync attempts removal
        public void Test_SetCreatedVolumeName(string? name)
        {
            _createdVolumeName = name;
        }

        public void Test_SetLogWaitTimeout(TimeSpan t)
        {
            // allow tests to shorten the waiting period
            // reflection-friendly helper
            typeof(DockerDotNetRunnerService).GetField("_logWaitTimeout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(this, t);
        }

        private async Task<bool> RunConfigureInContainerWithDockerCli(string containerId, string[] args, CancellationToken cancellationToken)
        {
            try
            {
                var joinedArgs = string.Join(' ', args.Select(a => a.Contains(' ') ? '"' + a + '"' : a));
                // run as the github-runner user to avoid configure script refusing to run as root
                var cmd = $"exec -u github-runner {containerId} /actions-runner/config.sh {joinedArgs}";
                Console.WriteLine($"Running docker {cmd}");
                var psi = new System.Diagnostics.ProcessStartInfo("docker", cmd) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                using var p = System.Diagnostics.Process.Start(psi);
                if (p == null) { Console.WriteLine("Failed to start docker exec"); return false; }

                p.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                p.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
                try { p.BeginOutputReadLine(); p.BeginErrorReadLine(); } catch { }
                await p.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                Console.WriteLine($"docker exec exit code: {p.ExitCode}");
                return p.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RunConfigureInContainerWithDockerCli exception: {ex}");
                return false;
            }
        }
    }

}
