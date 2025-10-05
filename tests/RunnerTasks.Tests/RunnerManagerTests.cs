using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;

// Following MSDN guidance: Arrange/Act/Assert, small focused tests, one behavior per test.

namespace RunnerTasks.Tests
{
    public class RunnerManagerTests
    {
        private readonly Xunit.Abstractions.ITestOutputHelper _output;

        public RunnerManagerTests(Xunit.Abstractions.ITestOutputHelper output)
        {
            _output = output;
        }

    [Fact]
    public async Task StartWithRetries_WhenTransientFailureThenSuccess_LogsAndReturnsTrue()
        {
            // Arrange: create a fake service that fails once then succeeds to trigger retry logs
            var fake = new FakeRunnerService(new[] { false, true });
            var testLogger = new TestLogger<RunnerManager>(rollingCapacity: 50);
            var manager = new RunnerManager(fake, testLogger);

            // Act
            var ok = await manager.StartWithRetriesAsync("t", "owner/repo", "https://github.com", maxRetries: 3, baseDelayMs: 1);

            // Dump recent logs to the test output for inspection
            _output.WriteLine($"StartWithRetries result: {ok}");
            _output.WriteLine("--- Recent logs ---");
            foreach (var line in testLogger.GetLastMessages(20))
            {
                _output.WriteLine(line);
            }
            _output.WriteLine("--- End logs ---");

            // Assert
            Assert.True(ok);
            Assert.Equal(2, fake.RegisterCallCount);
        }

        [Fact]
        public async Task StartWithRetries_WhenFailsThenSucceeds_AttemptsUntilSuccess()
        {
            var fake = new FakeRunnerService(new[] { false, false, true });
            var manager = new RunnerManager(fake);

            var result = await manager.StartWithRetriesAsync("t", "owner/repo", "https://github.com", maxRetries: 5, baseDelayMs: 1);

            Assert.True(result);
            Assert.Equal(3, fake.RegisterCallCount);
        }

        [Fact]
        public async Task StartWithRetries_WhenAlwaysFails_ReturnsFalseAfterMaxRetries()
        {
            var fake = new FakeRunnerService(new[] { false, false, false, false });
            var manager = new RunnerManager(fake);

            var result = await manager.StartWithRetriesAsync("t", "owner/repo", "https://github.com", maxRetries: 4, baseDelayMs: 1);

            Assert.False(result);
            Assert.Equal(4, fake.RegisterCallCount);
        }

        [Fact]
        public async Task StartRunnerStackAsync_WithValidEnv_DelegatesToServiceAndReturnsTrue()
        {
            var fake = new FakeRunnerService(new[] { true });
            var manager = new RunnerManager(fake);
            var env = new[] { "GITHUB_REPOSITORY=hutchisonkim/dot-net-app", "RUNNER_NAME=runner01" };

            var started = await manager.StartRunnerStackAsync(env);
            var stopped = await manager.StopRunnerStackAsync();

            Assert.True(started);
            Assert.True(stopped);
            Assert.Equal(1, fake.StartCallCount);
            Assert.Equal(1, fake.StopCallCount);
            Assert.Equal(env, fake.LastStartedEnv);
        }

        [Fact]
    public async Task OrchestrateStart_WhenRegisterSucceeds_StartsContainers()
        {
            // Arrange
            var token = "t";
            var repo = "owner/repo";
            var url = "https://github.com";
            var env = new[] { "GITHUB_REPOSITORY=hutchisonkim/dot-net-app", "A=1" };

            var mock = new Mock<IRunnerService>(MockBehavior.Strict);
            mock.Setup(s => s.RegisterAsync(token, repo, url, It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true).Verifiable();
            mock.Setup(s => s.StartContainersAsync(env, It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true).Verifiable();

            var testLogger = new TestLogger<RunnerManager>();
            var manager = new RunnerManager(mock.Object, testLogger);

            // Act
            var ok = await manager.OrchestrateStartAsync(token, repo, url, env, maxRetries: 3, baseDelayMs: 1);

            // Assert
            Assert.True(ok);
            mock.Verify(s => s.RegisterAsync(token, repo, url, It.IsAny<System.Threading.CancellationToken>()), Times.Once);
            mock.Verify(s => s.StartContainersAsync(env, It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task OrchestrateStart_WhenRegisterAlwaysFails_DoesNotStartContainers()
        {
            // Arrange
            var token = "t";
            var repo = "owner/repo";
            var url = "https://github.com";
            var env = new[] { "GITHUB_REPOSITORY=hutchisonkim/dot-net-app", "A=1" };

            var mock = new Mock<IRunnerService>(MockBehavior.Strict);
            // Always return false (simulates failure)
            mock.Setup(s => s.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(false).Verifiable();
            // Start should never be called
            mock.Setup(s => s.StartContainersAsync(It.IsAny<string[]>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true);

            var testLogger = new TestLogger<RunnerManager>();
            var manager = new RunnerManager(mock.Object, testLogger);

            // Act
            var ok = await manager.OrchestrateStartAsync(token, repo, url, env, maxRetries: 3, baseDelayMs: 1);

            // Assert
            Assert.False(ok);
            mock.Verify(s => s.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()), Times.Exactly(3));
            mock.Verify(s => s.StartContainersAsync(It.IsAny<string[]>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task StartWithRetries_WhenExceptionsThenSuccess_RetriesAndReturnsTrue()
        {
            // Arrange
            var token = "t";
            var repo = "owner/repo";
            var url = "https://github.com";

            var mock = new Mock<IRunnerService>(MockBehavior.Strict);
            // First two calls throw, third returns true (longer transient sequence)
            mock.SetupSequence(s => s.RegisterAsync(token, repo, url, It.IsAny<System.Threading.CancellationToken>()))
                .ThrowsAsync(new System.Exception("transient1"))
                .ThrowsAsync(new System.Exception("transient2"))
                .ReturnsAsync(true);

            var testLogger = new TestLogger<RunnerManager>();
            var manager = new RunnerManager(mock.Object, testLogger);

            // Act
            var ok = await manager.StartWithRetriesAsync(token, repo, url, maxRetries: 3, baseDelayMs: 1);

            // Assert
            Assert.True(ok);
            mock.Verify(s => s.RegisterAsync(token, repo, url, It.IsAny<System.Threading.CancellationToken>()), Times.Exactly(3));
        }

        [Fact]
        public async Task OrchestrateStart_WhenStartContainersReturnFalse_RecordsWarningAndReturnsFalse()
        {
            // Arrange
            var token = "t";
            var repo = "owner/repo";
            var url = "https://github.com";
            var env = new[] { "GITHUB_REPOSITORY=hutchisonkim/dot-net-app" };

            var mock = new Mock<IRunnerService>(MockBehavior.Strict);
            mock.Setup(s => s.RegisterAsync(token, repo, url, It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true).Verifiable();
            // Simulate partial/compose failure
            mock.Setup(s => s.StartContainersAsync(env, It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(false).Verifiable();

            var testLogger = new TestLogger<RunnerManager>();
            var manager = new RunnerManager(mock.Object, testLogger);

            // Act
            var ok = await manager.OrchestrateStartAsync(token, repo, url, env, maxRetries: 2, baseDelayMs: 1);

            // Assert
            Assert.False(ok);
            mock.VerifyAll();
            Assert.True(testLogger.Contains(Microsoft.Extensions.Logging.LogLevel.Warning, "StartRunnerStackAsync returned false"));
        }

        [Fact]
        public async Task OrchestrateStart_WhenEnvMissingRepository_ThrowsArgumentException()
        {
            // Arrange
            var token = "t";
            var repo = "owner/repo";
            var url = "https://github.com";
            var env = new[] { "SOME_VAR=1" };

            var mock = new Mock<IRunnerService>(MockBehavior.Strict);
            var manager = new RunnerManager(mock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => manager.OrchestrateStartAsync(token, repo, url, env));
        }

        [Fact]
        public async Task StartWithRetries_WhenCancelled_ThrowsTaskCanceledException()
        {
            // Arrange
            var token = "t";
            var repo = "owner/repo";
            var url = "https://github.com";

            var mock = new Mock<IRunnerService>(MockBehavior.Strict);
            // Make RegisterAsync wait so we can cancel
            mock.Setup(s => s.RegisterAsync(token, repo, url, It.IsAny<System.Threading.CancellationToken>()))
                .Returns(async (string a, string b, string c, System.Threading.CancellationToken ct) =>
                {
                    await Task.Delay(500, ct);
                    return true;
                });

            var manager = new RunnerManager(mock.Object);
            using var cts = new System.Threading.CancellationTokenSource();

            // Act: cancel shortly after starting
            var task = manager.StartWithRetriesAsync(token, repo, url, maxRetries: 3, baseDelayMs: 1000, cancellationToken: cts.Token);
            cts.CancelAfter(50);

            // Assert
            await Assert.ThrowsAsync<System.Threading.Tasks.TaskCanceledException>(() => task);
        }

        [Fact]
    public async Task Integration_OrchestrateStartAndStop_WithFakeOrRealRunner_WorksBasedOnEnv()
        {
            // If RUN_INTEGRATION=1, run the real docker-compose integration. Otherwise run a mock-based integration
            // so the test passes in environments without Docker.
            if (string.Equals(Environment.GetEnvironmentVariable("RUN_INTEGRATION"), "1", StringComparison.OrdinalIgnoreCase))
            {
                var workingDir = System.IO.Path.GetFullPath("github-self-hosted-runner-docker");
                var svc = new DockerComposeRunnerService(workingDir, new TestLogger<DockerComposeRunnerService>());

                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

                var started = await svc.StartContainersAsync(new[] { "GITHUB_REPOSITORY=hutchisonkim/dot-net-app" }, cts.Token);
                Assert.True(started, "docker-compose up failed");

                // Registration may require a token; if not provided via env, skip the registration step.
                var regToken = Environment.GetEnvironmentVariable("RUNNER_REG_TOKEN");
                if (!string.IsNullOrEmpty(regToken))
                {
                    var registered = await svc.RegisterAsync(regToken, "hutchisonkim/dot-net-app", "https://github.com", cts.Token);
                    Assert.True(registered, "configure-runner.sh failed");
                }

                var stopped = await svc.StopContainersAsync(cts.Token);
                Assert.True(stopped, "docker-compose down failed");
            }
            else
            {
                // Mock-based integration: validate orchestration wiring using FakeRunnerService
                var fake = new FakeRunnerService(new[] { true });
                var testLogger = new TestLogger<RunnerManager>();
                var manager = new RunnerManager(fake, testLogger);

                var env = new[] { "GITHUB_REPOSITORY=hutchisonkim/dot-net-app" };
                var ok = await manager.OrchestrateStartAsync("token", "hutchisonkim/dot-net-app", "https://github.com", env, maxRetries: 1, baseDelayMs: 1);
                AssertWithLogs.True(ok, "OrchestrateStartAsync should succeed with FakeRunnerService", _output, testLogger);
                AssertWithLogs.Equal(1, fake.RegisterCallCount, "RegisterCallCount mismatch", _output, testLogger);
                AssertWithLogs.Equal(1, fake.StartCallCount, "StartCallCount mismatch", _output, testLogger);

                var stopped = await manager.OrchestrateStopAsync();
                AssertWithLogs.True(stopped, "OrchestrateStopAsync should succeed with FakeRunnerService", _output, testLogger);
                AssertWithLogs.Equal(1, fake.StopCallCount, "StopCallCount mismatch", _output, testLogger);
            }
        }

        [Fact]
        public void Constructor_WhenServiceIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RunnerManager(null!));
        }

        [Fact]
        public async Task StartWithRetries_WhenMaxRetriesIsNotPositive_ThrowsArgumentOutOfRange()
        {
            var fake = new FakeRunnerService(new[] { true });
            var manager = new RunnerManager(fake);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => manager.StartWithRetriesAsync("t", "r", "u", maxRetries: 0));
        }

        [Fact]
        public async Task StartWithRetries_WhenRegisterAlwaysThrows_ReturnsFalseAfterRetries()
        {
            var mock = new Mock<IRunnerService>(MockBehavior.Strict);
            mock.Setup(s => s.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var manager = new RunnerManager(mock.Object, new TestLogger<RunnerManager>());

            var result = await manager.StartWithRetriesAsync("t", "r", "u", maxRetries: 3, baseDelayMs: 1);

            Assert.False(result);
            mock.Verify(s => s.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()), Times.AtLeast(3));
        }

        [Fact]
        public async Task OrchestrateStart_WhenEnvVarsIsNull_ThrowsArgumentNullException()
        {
            var fake = new FakeRunnerService(new[] { true });
            var manager = new RunnerManager(fake);

            await Assert.ThrowsAsync<ArgumentNullException>(() => manager.OrchestrateStartAsync("t", "r", "u", null!));
        }

        [Fact]
        public async Task OrchestrateStart_WhenStartContainersThrows_PropagatesException()
        {
            var mock = new Mock<IRunnerService>(MockBehavior.Strict);
            mock.Setup(s => s.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true);
            mock.Setup(s => s.StartContainersAsync(It.IsAny<string[]>(), It.IsAny<System.Threading.CancellationToken>())).ThrowsAsync(new InvalidOperationException("up failed"));

            var manager = new RunnerManager(mock.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => manager.OrchestrateStartAsync("t", "r", "u", new[] { "GITHUB_REPOSITORY=foo/bar" }));
        }

        [Fact]
        public async Task OrchestrateStopAsync_DelegatesToServiceStop()
        {
            var mock = new Mock<IRunnerService>(MockBehavior.Strict);
            mock.Setup(s => s.StopContainersAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true).Verifiable();

            var manager = new RunnerManager(mock.Object);

            var ok = await manager.OrchestrateStopAsync();

            Assert.True(ok);
            mock.Verify();
        }
    }
}
