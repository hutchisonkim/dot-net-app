# DotNetApp

DotNetApp is a demo starter built to showcase clean, maintainable **.NET 8** patterns while following **MSDN best practices** and **xUnit testing guidelines**.

## Features

✔️ Basic .NET 8 Web API + Blazor WebAssembly app template  
✔️ Unit tests (xUnit, bUnit), integration tests, and optional E2E tests (Playwright)  
✔️ GitHub Actions workflows for deployment and diagnostics  
✔️ Self-hosted GitHub Actions Runner + Helper CLI for private repo workflow runs  
✔️ GitHub Pages publishing for code coverage reports  
🚧 End-to-end programmatic orchestration  
🚧 Full code coverage tracking

## Coverage

![Test Coverage](https://hutchisonkim.github.io/dot-net-app/coverage-summary.svg)

### Scoring Estimates

| Color | Meaning | Score Range |
|:------|:--------|:-----------:|
| 🟢 | Excellent | ≥ 90 |
| 🟩 | Very Good | 80–89 |
| 🟡 | Okay | 70–79 |
| 🟠 | Needs Work | 50–69 |
| 🟥 | Poor | 30–49 |
| 🔴 | Very Poor | < 30 |

---

### Test Quality Breakdown

| Method | File | Isolation | Repeatability | Speed | Maintainability | Average |
|:--|:--|:--:|:--:|:--:|:--:|:--:|
| StartWithRetries_WhenTransientFailureThenSuccess_LogsAndReturnsTrue | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| StartWithRetries_WhenFailsThenSucceeds_AttemptsUntilSuccess | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| StartWithRetries_WhenAlwaysFails_ReturnsFalseAfterMaxRetries | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| StartRunnerStackAsync_WithValidEnv_DelegatesToServiceAndReturnsTrue | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| OrchestrateStart_WhenRegisterSucceeds_StartsContainers | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| OrchestrateStart_WhenRegisterAlwaysFails_DoesNotStartContainers | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| StartWithRetries_WhenExceptionsThenSuccess_RetriesAndReturnsTrue | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| OrchestrateStart_WhenStartContainersReturnFalse_RecordsWarningAndReturnsFalse | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟩 85 | 🟩 85 | 🟡 75 | 🟩 80 | 🟩 82 |
| OrchestrateStart_WhenEnvMissingRepository_ThrowsArgumentException | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟢 90 | 🟢 95 | 🟩 85 | 🟩 85 | 🟩 89 |
| StartWithRetries_WhenCancelled_ThrowsTaskCanceledException | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟩 80 | 🟩 80 | 🟡 70 | 🟩 80 | 🟡 78 |
| Integration_OrchestrateStartAndStop_WithFakeOrRealRunner_WorksBasedOnEnv | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟥 30 | 🟠 50 | 🟥 30 | 🟠 60 | 🟥 43 |
| Constructor_WhenServiceIsNull_ThrowsArgumentNullException | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟢 95 | 🟢 95 | 🟢 95 | 🟩 90 | 🟢 94 |
| StartWithRetries_WhenMaxRetriesIsNotPositive_ThrowsArgumentOutOfRange | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟢 90 | 🟢 90 | 🟩 85 | 🟩 85 | 🟩 88 |
| StartWithRetries_WhenRegisterAlwaysThrows_ReturnsFalseAfterRetries | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟩 85 | 🟩 85 | 🟡 75 | 🟩 80 | 🟩 82 |
| OrchestrateStart_WhenEnvVarsIsNull_ThrowsArgumentNullException | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟢 90 | 🟢 90 | 🟩 85 | 🟩 85 | 🟩 88 |
| OrchestrateStart_WhenStartContainersThrows_PropagatesException | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟩 85 | 🟩 85 | 🟡 75 | 🟩 80 | 🟩 82 |
| OrchestrateStopAsync_DelegatesToServiceStop | `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs` | 🟢 90 | 🟢 90 | 🟩 85 | 🟩 85 | 🟩 88 |
| StopContainersAsync_WithMockService_DelegatesToService | `tests/GitHub.Runner.Docker.Tests/StopContainersTests.cs` | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| RunnerLogs_Contain_ListeningForJobs_IntegrationOrMock | `tests/GitHub.Runner.Docker.Tests/RunnerLogsIntegrationTests.cs` | 🟠 60 | 🟡 70 | 🟠 50 | 🟡 70 | 🟠 63 |
| DockerRunnerService_GatedIntegrationOrMock_Works | `tests/GitHub.Runner.Docker.Tests/DockerDotNetRunnerServiceTests.cs` | 🟥 40 | 🟠 50 | 🟥 40 | 🟠 60 | 🟥 48 |
| Client_Index_Loads_BlazorRuntime | `tests/DotNetApp.Tests.E2E/PlaywrightTests.cs` | 🔴 20 | 🟥 30 | 🔴 10 | 🟥 40 | 🔴 25 |
| Health_WhenCalled_ReturnsOkWithStatus | `tests/DotNetApp.Server.Tests.Unit/StateControllerTests.cs` | 🟢 95 | 🟢 95 | 🟩 90 | 🟩 90 | 🟢 93 |
| Index_WhenRendered_ContainsAppTitle | `tests/DotNetApp.Client.Tests.Unit/IndexTests.cs` | 🟩 85 | 🟩 85 | 🟡 70 | 🟡 75 | 🟡 79 |
| FetchStatusAsync_WhenCalled_ReturnsHealthy | `tests/DotNetApp.Client.Tests.Unit/HealthStatusProviderTests.cs` | 🟢 90 | 🟢 90 | 🟩 85 | 🟩 85 | 🟩 88 |
| ClientRootRequest_WhenServed_MatchesPublishedIndexHtml | `tests/DotNetApp.Client.Tests.Integration/ServeMatchesPublishedTests.cs` | 🟥 30 | 🟥 40 | 🔴 20 | 🟠 60 | 🟥 38 |
| PlatformApiClient_CallsApi_ReturnsHealthStatus | `tests/DotNetApp.Client.Tests.Integration/ExampleApiIntegrationTests.cs` | 🟡 70 | 🟩 80 | 🟠 60 | 🟩 80 | 🟡 73 |
| Health_WhenCalled_ReturnsMockedStatus | `tests/DotNetApp.Server.Tests.Integration/HealthEndpointIntegrationTests.cs` | 🟩 80 | 🟩 85 | 🟠 65 | 🟩 80 | 🟡 78 |
| RootRequest_WhenFrontendConfigured_ReturnsFakeIndex | `tests/DotNetApp.Server.Tests.Integration/HealthEndpointIntegrationTests.cs` | 🟩 80 | 🟩 85 | 🟠 65 | 🟩 80 | 🟡 78 |
| ClientRootRequest_WhenServed_MatchesExpectedIndex | `tests/DotNetApp.Server.Tests.Integration/ServeFrontendFromBackendTests.cs` | 🟥 30 | 🟥 40 | 🔴 20 | 🟠 60 | 🟥 38 |
| All_Facts_And_Theories_Have_Category_Trait | `tests/DotNetApp.Server.Tests.Unit/CategoryConventionsTests.cs` | 🟢 95 | 🟢 95 | 🟩 90 | 🟡 85 | 🟢 91 |

*Generated by project agent*
