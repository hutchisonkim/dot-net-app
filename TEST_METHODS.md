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

### Test scoring

The table below documents a suggested score (0–100) for each test method across the four qualities, plus the arithmetic average (rounded to nearest integer). These are heuristic estimates based on the test's style (unit vs integration/E2E), use of mocks, and expected runtime.

| Test method | File | Isolation (30%) | Repeatability (30%) | Speed (20%) | Maintainability (20%) | Average |
|---|---:|---:|---:|---:|---:|---:|
| StartWithRetries_WhenTransientFailureThenSuccess_LogsAndReturnsTrue | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 90 | 90 | 80 | 80 | 85 |
| StartWithRetries_WhenFailsThenSucceeds_AttemptsUntilSuccess | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 90 | 90 | 80 | 80 | 85 |
| StartWithRetries_WhenAlwaysFails_ReturnsFalseAfterMaxRetries | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 90 | 90 | 80 | 80 | 85 |
| StartRunnerStackAsync_WithValidEnv_DelegatesToServiceAndReturnsTrue | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 90 | 90 | 80 | 80 | 85 |
| OrchestrateStart_WhenRegisterSucceeds_StartsContainers | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 90 | 90 | 80 | 80 | 85 |
| OrchestrateStart_WhenRegisterAlwaysFails_DoesNotStartContainers | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 90 | 90 | 80 | 80 | 85 |
| StartWithRetries_WhenExceptionsThenSuccess_RetriesAndReturnsTrue | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 90 | 90 | 80 | 80 | 85 |
| OrchestrateStart_WhenStartContainersReturnFalse_RecordsWarningAndReturnsFalse | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 85 | 85 | 75 | 80 | 82 |
| OrchestrateStart_WhenEnvMissingRepository_ThrowsArgumentException | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 90 | 95 | 85 | 85 | 89 |
| StartWithRetries_WhenCancelled_ThrowsTaskCanceledException | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 80 | 80 | 70 | 80 | 78 |
| Integration_OrchestrateStartAndStop_WithFakeOrRealRunner_WorksBasedOnEnv | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 30 | 50 | 30 | 60 | 43 |
| Constructor_WhenServiceIsNull_ThrowsArgumentNullException | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 95 | 95 | 95 | 90 | 94 |
| StartWithRetries_WhenMaxRetriesIsNotPositive_ThrowsArgumentOutOfRange | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 90 | 90 | 85 | 85 | 88 |
| StartWithRetries_WhenRegisterAlwaysThrows_ReturnsFalseAfterRetries | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 85 | 85 | 75 | 80 | 82 |
| OrchestrateStart_WhenEnvVarsIsNull_ThrowsArgumentNullException | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 90 | 90 | 85 | 85 | 88 |
| OrchestrateStart_WhenStartContainersThrows_PropagatesException | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 85 | 85 | 75 | 80 | 82 |
| OrchestrateStopAsync_DelegatesToServiceStop | tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs | 90 | 90 | 85 | 85 | 88 |
| StopContainersAsync_WithMockService_DelegatesToService | tests/GitHub.Runner.Docker.Tests/StopContainersTests.cs | 90 | 90 | 80 | 80 | 85 |
| RunnerLogs_Contain_ListeningForJobs_IntegrationOrMock | tests/GitHub.Runner.Docker.Tests/RunnerLogsIntegrationTests.cs | 60 | 70 | 50 | 70 | 63 |
| DockerRunnerService_GatedIntegrationOrMock_Works | tests/GitHub.Runner.Docker.Tests/DockerDotNetRunnerServiceTests.cs | 40 | 50 | 40 | 60 | 48 |
| Client_Index_Loads_BlazorRuntime | tests/DotNetApp.Tests.E2E/PlaywrightTests.cs | 20 | 30 | 10 | 40 | 25 |
| Health_WhenCalled_ReturnsOkWithStatus | tests/DotNetApp.Server.Tests.Unit/StateControllerTests.cs | 95 | 95 | 90 | 90 | 93 |
| Index_WhenRendered_ContainsAppTitle | tests/DotNetApp.Client.Tests.Unit/IndexTests.cs | 85 | 85 | 70 | 75 | 79 |
| FetchStatusAsync_WhenCalled_ReturnsHealthy | tests/DotNetApp.Client.Tests.Unit/HealthStatusProviderTests.cs | 90 | 90 | 85 | 85 | 88 |
| ClientRootRequest_WhenServed_MatchesPublishedIndexHtml | tests/DotNetApp.Client.Tests.Integration/ServeMatchesPublishedTests.cs | 30 | 40 | 20 | 60 | 38 |
| PlatformApiClient_CallsApi_ReturnsHealthStatus | tests/DotNetApp.Client.Tests.Integration/ExampleApiIntegrationTests.cs | 70 | 80 | 60 | 80 | 73 |
| Health_WhenCalled_ReturnsMockedStatus | tests/DotNetApp.Server.Tests.Integration/HealthEndpointIntegrationTests.cs | 80 | 85 | 65 | 80 | 78 |
| RootRequest_WhenFrontendConfigured_ReturnsFakeIndex | tests/DotNetApp.Server.Tests.Integration/HealthEndpointIntegrationTests.cs | 80 | 85 | 65 | 80 | 78 |
| ClientRootRequest_WhenServed_MatchesExpectedIndex | tests/DotNetApp.Server.Tests.Integration/ServeFrontendFromBackendTests.cs | 30 | 40 | 20 | 60 | 38 |
| All_Facts_And_Theories_Have_Category_Trait | tests/DotNetApp.Server.Tests.Unit/CategoryConventionsTests.cs | 95 | 95 | 90 | 85 | 91 |

Notes:
- These scores are estimates to help prioritize test improvements (make flaky tests more deterministic, speed up slow integration tests by mocking, etc.).
- If you'd like, I can export this table as CSV or JSON, or add a short maintenance note per test explaining why the score was chosen.


