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

| <sub>Method</sub>  | <sub>Isolation</sub>  | <sub>Repeatability</sub>  | <sub>Speed</sub>  | <sub>Maintainability</sub>  | <sub>Average</sub>  |
|:--|:--:|:--:|:--:|:--:|:--:|
| <sub>StartWithRetries_WhenTransientFailureThenSuccess_LogsAndRetu...</sub> | 🟢 | 🟢 | 🟩 | 🟩 | 🟩 |
| <sub>StartWithRetries_WhenFailsThenSucceeds_AttemptsUntilSuccess</sub></sub> | 🟢 | 🟢 | 🟩 | 🟩 | 🟩 |
| <sub>StartWithRetries_WhenAlwaysFails_ReturnsFalseAfterMaxRetries</sub> | 🟢 | 🟢 | 🟩 | 🟩 | 🟩 |
| <sub>StartRunnerStackAsync_WithValidEnv_DelegatesToServiceAndRetur...</sub> | 🟢 | 🟢 | 🟩 | 🟩 | 🟩 |
| <sub>OrchestrateStart_WhenRegisterSucceeds_StartsContainers</sub> | 🟢 | 🟢 | 🟩 | 🟩 | 🟩 |
| <sub>OrchestrateStart_WhenRegisterAlwaysFails_DoesNotStartContainers</sub> | 🟢 | 🟢 | 🟩 | 🟩 | 🟩 |
| <sub>StartWithRetries_WhenExceptionsThenSuccess_RetriesAndReturns...</sub> | 🟢 | 🟢 | 🟩 | 🟩 | 🟩 |
| <sub>OrchestrateStart_WhenStartContainersReturnFalse_RecordsWarnin...</sub> | 🟩 | 🟩 | 🟡 | 🟩 | 🟩 |
| <sub>OrchestrateStart_WhenEnvMissingRepository_ThrowsArgumentExcep...</sub> | 🟢 | 🟢 | 🟩 | 🟩 | 🟩 |
| <sub>StartWithRetries_WhenCancelled_ThrowsTaskCanceledException</sub> | 🟩 | 🟩 | 🟡 | 🟩 | 🟡 |
| <sub>Integration_OrchestrateStartAndStop_WithFakeOrRealRunner_Work...</sub> | 🟥 | 🟠 | 🟥 | 🟠 | 🟥 |
| <sub>Constructor_WhenServiceIsNull_ThrowsArgumentNullException</sub> | 🟢 | 🟢 | 🟢 | 🟩 | 🟢 |
| <sub>StartWithRetries_WhenMaxRetriesIsNotPositive_ThrowsArgument...</sub> | 🟢 | 🟢 | 🟩 | 🟩 | 🟩 |
| <sub>StartWithRetries_WhenRegisterAlwaysThrows_ReturnsFalseAfterR...</sub> | 🟩 | 🟩 | 🟡 | 🟩 | 🟩 |
| <sub>OrchestrateStart_WhenEnvVarsIsNull_ThrowsArgumentNullException</sub> | 🟢 | 🟢 | 🟩 | 🟩 | 🟩 |
| <sub>OrchestrateStart_WhenStartContainersThrows_PropagatesException</sub> | 🟩 | 🟩 | 🟡 | 🟩 | 🟩 |
| <sub>OrchestrateStopAsync_DelegatesToServiceStop</sub> | 🟢 | 🟢 | 🟩 | 🟩 | 🟩 |
| <sub>StopContainersAsync_WithMockService_DelegatesToService</sub> | 🟢 | 🟢 | 🟩 | 🟩 | 🟩 |
| <sub>RunnerLogs_Contain_ListeningForJobs_IntegrationOrMock</sub> | 🟠 | 🟡 | 🟠 | 🟡 | 🟠 |
| <sub>DockerRunnerService_GatedIntegrationOrMock_Works</sub> | 🟥 | 🟠 | 🟥 | 🟠 | 🟥 |
| <sub>Client_Index_Loads_BlazorRuntime</sub> | 🔴 | 🟥 | 🔴 | 🟥 | 🔴 |
| <sub>Health_WhenCalled_ReturnsOkWithStatus</sub> | 🟢 | 🟢 | 🟩 | 🟩 | 🟢 |
| <sub>Index_WhenRendered_ContainsAppTitle</sub> | 🟩 | 🟩 | 🟡 | 🟡 | 🟡 |
| <sub>FetchStatusAsync_WhenCalled_ReturnsHealthy</sub> | 🟢 | 🟢 | 🟩 | 🟩 | 🟩 |
| <sub>ClientRootRequest_WhenServed_MatchesPublishedIndexHtml</sub> | 🟥 | 🟥 | 🔴 | 🟠 | 🟥 |
| <sub>PlatformApiClient_CallsApi_ReturnsHealthStatus</sub> | 🟡 | 🟩 | 🟠 | 🟩 | 🟡 |
| <sub>Health_WhenCalled_ReturnsMockedStatus</sub> | 🟩 | 🟩 | 🟠 | 🟩 | 🟡 |
| <sub>RootRequest_WhenFrontendConfigured_ReturnsFakeIndex</sub> | 🟩 | 🟩 | 🟠 | 🟩 | 🟡 |
| <sub>ClientRootRequest_WhenServed_MatchesExpectedIndex</sub> | 🟥 | 🟥 | 🔴 | 🟠 | 🟥 |
| <sub>All_Facts_And_Theories_Have_Category_Trait</sub> | 🟢 | 🟢 | 🟩 | 🟡 | 🟢 |


