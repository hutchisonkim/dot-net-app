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
        [Fact]
        public async Task Test_RetryLogic_SucceedsAfterRetries()
        {
            var fake = new FakeRunnerService(new[] { false, false, true });
            var manager = new RunnerManager(fake);

            var result = await manager.StartWithRetriesAsync("t", "owner/repo", "https://github.com", maxRetries: 5, baseDelayMs: 1);

            Assert.True(result);
            Assert.Equal(3, fake.RegisterCallCount);
        }

        [Fact]
        public async Task Test_RetryLogic_FailsAllAttempts()
        {
            var fake = new FakeRunnerService(new[] { false, false, false, false });
            var manager = new RunnerManager(fake);

            var result = await manager.StartWithRetriesAsync("t", "owner/repo", "https://github.com", maxRetries: 4, baseDelayMs: 1);

            Assert.False(result);
            Assert.Equal(4, fake.RegisterCallCount);
        }

        [Fact]
        public async Task Test_StartStop_CallsContainerMethods()
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
    public async Task OrchestrateStart_CallsRegisterThenStart_WhenRegisterSucceeds()
        {
            // Arrange
            var token = "t";
            var repo = "owner/repo";
            var url = "https://github.com";
            var env = new[] { "GITHUB_REPOSITORY=hutchisonkim/dot-net-app", "A=1" };

            var mock = new Mock<IRunnerService>(MockBehavior.Strict);
            mock.Setup(s => s.RegisterAsync(token, repo, url, It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true).Verifiable();
            mock.Setup(s => s.StartContainersAsync(env, It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true).Verifiable();

            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<RunnerManager>>();
            var manager = new RunnerManager(mock.Object, loggerMock.Object);

            // Act
            var ok = await manager.OrchestrateStartAsync(token, repo, url, env, maxRetries: 3, baseDelayMs: 1);

            // Assert
            Assert.True(ok);
            mock.Verify(s => s.RegisterAsync(token, repo, url, It.IsAny<System.Threading.CancellationToken>()), Times.Once);
            mock.Verify(s => s.StartContainersAsync(env, It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task OrchestrateStart_DoesNotStart_WhenRegisterFailsAfterRetries()
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

            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<RunnerManager>>();
            var manager = new RunnerManager(mock.Object, loggerMock.Object);

            // Act
            var ok = await manager.OrchestrateStartAsync(token, repo, url, env, maxRetries: 3, baseDelayMs: 1);

            // Assert
            Assert.False(ok);
            mock.Verify(s => s.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()), Times.Exactly(3));
            mock.Verify(s => s.StartContainersAsync(It.IsAny<string[]>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task StartWithRetries_TreatsExceptionsAsTransientAndRetries()
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

            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<RunnerManager>>();
            var manager = new RunnerManager(mock.Object, loggerMock.Object);

            // Act
            var ok = await manager.StartWithRetriesAsync(token, repo, url, maxRetries: 3, baseDelayMs: 1);

            // Assert
            Assert.True(ok);
            mock.Verify(s => s.RegisterAsync(token, repo, url, It.IsAny<System.Threading.CancellationToken>()), Times.Exactly(3));
        }

        [Fact]
        public async Task OrchestrateStart_ReturnsFalse_WhenStartContainersFail()
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

            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<RunnerManager>>();
            var manager = new RunnerManager(mock.Object, loggerMock.Object);

            // Act
            var ok = await manager.OrchestrateStartAsync(token, repo, url, env, maxRetries: 2, baseDelayMs: 1);

            // Assert
            Assert.False(ok);
            mock.VerifyAll();
            // Verify that the logger recorded a Warning with the expected message by inspecting recorded invocations.
            var found = false;
            foreach (var inv in loggerMock.Invocations)
            {
                if (inv.Method.Name == "Log"
                    && inv.Arguments.Count >= 3
                    && inv.Arguments[0] is Microsoft.Extensions.Logging.LogLevel level
                    && level == Microsoft.Extensions.Logging.LogLevel.Warning)
                {
                    var state = Convert.ToString(inv.Arguments[2]) ?? string.Empty;
                    if (state.Contains("StartRunnerStackAsync returned false"))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.True(found, "Expected a Warning log entry containing 'StartRunnerStackAsync returned false'");
        }

        [Fact]
        public async Task OrchestrateStart_ThrowsArgumentException_WhenEnvMissingRepository()
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
        public async Task StartWithRetries_CanBeCancelled()
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

        [Fact(Skip = "Integration test placeholder - set RUN_INTEGRATION=1 to run")]
        public async Task Integration_StartsDockerComposeAndRegistersRunner()
        {
            await Task.CompletedTask;
        }
    }
}
