## Test methods found in repository

Below is a list of test methods discovered in the `tests/` folder, grouped by the relative file path that contains them.

### tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs
- StartWithRetries_WhenTransientFailureThenSuccess_LogsAndReturnsTrue
- StartWithRetries_WhenFailsThenSucceeds_AttemptsUntilSuccess
- StartWithRetries_WhenAlwaysFails_ReturnsFalseAfterMaxRetries
- StartRunnerStackAsync_WithValidEnv_DelegatesToServiceAndReturnsTrue
- OrchestrateStart_WhenRegisterSucceeds_StartsContainers
- OrchestrateStart_WhenRegisterAlwaysFails_DoesNotStartContainers
- StartWithRetries_WhenExceptionsThenSuccess_RetriesAndReturnsTrue
- OrchestrateStart_WhenStartContainersReturnFalse_RecordsWarningAndReturnsFalse
- OrchestrateStart_WhenEnvMissingRepository_ThrowsArgumentException
- StartWithRetries_WhenCancelled_ThrowsTaskCanceledException
- Integration_OrchestrateStartAndStop_WithFakeOrRealRunner_WorksBasedOnEnv
- Constructor_WhenServiceIsNull_ThrowsArgumentNullException
- StartWithRetries_WhenMaxRetriesIsNotPositive_ThrowsArgumentOutOfRange
- StartWithRetries_WhenRegisterAlwaysThrows_ReturnsFalseAfterRetries
- OrchestrateStart_WhenEnvVarsIsNull_ThrowsArgumentNullException
- OrchestrateStart_WhenStartContainersThrows_PropagatesException
- OrchestrateStopAsync_DelegatesToServiceStop

### tests/GitHub.Runner.Docker.Tests/StopContainersTests.cs
- StopContainersAsync_WithMockService_DelegatesToService

### tests/GitHub.Runner.Docker.Tests/RunnerLogsIntegrationTests.cs
- RunnerLogs_Contain_ListeningForJobs_IntegrationOrMock

### tests/GitHub.Runner.Docker.Tests/DockerDotNetRunnerServiceTests.cs
- DockerRunnerService_GatedIntegrationOrMock_Works

### tests/DotNetApp.Tests.E2E/PlaywrightTests.cs
- Client_Index_Loads_BlazorRuntime

### tests/DotNetApp.Server.Tests.Unit/StateControllerTests.cs
- Health_WhenCalled_ReturnsOkWithStatus

### tests/DotNetApp.Client.Tests.Unit/IndexTests.cs
- Index_WhenRendered_ContainsAppTitle

### tests/DotNetApp.Client.Tests.Unit/HealthStatusProviderTests.cs
- FetchStatusAsync_WhenCalled_ReturnsHealthy

### tests/DotNetApp.Client.Tests.Integration/ServeMatchesPublishedTests.cs
- ClientRootRequest_WhenServed_MatchesPublishedIndexHtml

### tests/DotNetApp.Client.Tests.Integration/ExampleApiIntegrationTests.cs
- PlatformApiClient_CallsApi_ReturnsHealthStatus

### tests/DotNetApp.Server.Tests.Integration/HealthEndpointIntegrationTests.cs
- Health_WhenCalled_ReturnsMockedStatus
- RootRequest_WhenFrontendConfigured_ReturnsFakeIndex

### tests/DotNetApp.Server.Tests.Integration/ServeFrontendFromBackendTests.cs
- ClientRootRequest_WhenServed_MatchesExpectedIndex

### tests/DotNetApp.Server.Tests.Unit/CategoryConventionsTests.cs
- All_Facts_And_Theories_Have_Category_Trait

---

### How to calculate scores

Scores follow xUnit / MSDN testing guidance: we weighted key qualities to produce a single fun score:

- Isolation (30%): can the test run without external services? Unit tests score high here.
- Repeatability / Determinism (30%): will the test produce the same result every run? Tests that mock/seed state score higher.
- Speed (20%): how fast is the test to run locally/CI? Quick unit tests score full points.
- Maintainability (20%): clarity, ease of update, and whether the test is brittle against UI or build layout changes.

Each test's short rationale maps those qualities into the final 0–100 score and a playful badge: 🏆 Platinum / 🥇 Gold / 🥈 Silver / 🥉 Bronze / 💩 Copper / 🐉 Legendary.

