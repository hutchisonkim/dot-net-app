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
| <sub>StartWithRetries_WhenTransientFailureThenSuccess_LogsAndRetu...</sub> | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| <sub>StartWithRetries_WhenFailsThenSucceeds_AttemptsUntilSuccess</sub></sub> | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| <sub>StartWithRetries_WhenAlwaysFails_ReturnsFalseAfterMaxRetries</sub> | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| <sub>StartRunnerStackAsync_WithValidEnv_DelegatesToServiceAndRetur...</sub> | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| <sub>OrchestrateStart_WhenRegisterSucceeds_StartsContainers</sub> | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| <sub>OrchestrateStart_WhenRegisterAlwaysFails_DoesNotStartContainers</sub> | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| <sub>StartWithRetries_WhenExceptionsThenSuccess_RetriesAndReturns...</sub> | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| <sub>OrchestrateStart_WhenStartContainersReturnFalse_RecordsWarnin...</sub> | 🟩 85 | 🟩 85 | 🟡 75 | 🟩 80 | 🟩 82 |
| <sub>OrchestrateStart_WhenEnvMissingRepository_ThrowsArgumentExcep...</sub> | 🟢 90 | 🟢 95 | 🟩 85 | 🟩 85 | 🟩 89 |
| <sub>StartWithRetries_WhenCancelled_ThrowsTaskCanceledException</sub> | 🟩 80 | 🟩 80 | 🟡 70 | 🟩 80 | 🟡 78 |
| <sub>Integration_OrchestrateStartAndStop_WithFakeOrRealRunner_Work...</sub> | 🟥 30 | 🟠 50 | 🟥 30 | 🟠 60 | 🟥 43 |
| <sub>Constructor_WhenServiceIsNull_ThrowsArgumentNullException</sub> | 🟢 95 | 🟢 95 | 🟢 95 | 🟩 90 | 🟢 94 |
| <sub>StartWithRetries_WhenMaxRetriesIsNotPositive_ThrowsArgument...</sub> | 🟢 90 | 🟢 90 | 🟩 85 | 🟩 85 | 🟩 88 |
| <sub>StartWithRetries_WhenRegisterAlwaysThrows_ReturnsFalseAfterR...</sub> | 🟩 85 | 🟩 85 | 🟡 75 | 🟩 80 | 🟩 82 |
| <sub>OrchestrateStart_WhenEnvVarsIsNull_ThrowsArgumentNullException</sub> | 🟢 90 | 🟢 90 | 🟩 85 | 🟩 85 | 🟩 88 |
| <sub>OrchestrateStart_WhenStartContainersThrows_PropagatesException</sub> | 🟩 85 | 🟩 85 | 🟡 75 | 🟩 80 | 🟩 82 |
| <sub>OrchestrateStopAsync_DelegatesToServiceStop</sub> | 🟢 90 | 🟢 90 | 🟩 85 | 🟩 85 | 🟩 88 |
| <sub>StopContainersAsync_WithMockService_DelegatesToService</sub> | 🟢 90 | 🟢 90 | 🟩 80 | 🟩 80 | 🟩 85 |
| <sub>RunnerLogs_Contain_ListeningForJobs_IntegrationOrMock</sub> | 🟠 60 | 🟡 70 | 🟠 50 | 🟡 70 | 🟠 63 |
| <sub>DockerRunnerService_GatedIntegrationOrMock_Works</sub> | 🟥 40 | 🟠 50 | 🟥 40 | 🟠 60 | 🟥 48 |
| <sub>Client_Index_Loads_BlazorRuntime</sub> | 🔴 20 | 🟥 30 | 🔴 10 | 🟥 40 | 🔴 25 |
| <sub>Health_WhenCalled_ReturnsOkWithStatus</sub> | 🟢 95 | 🟢 95 | 🟩 90 | 🟩 90 | 🟢 93 |
| <sub>Index_WhenRendered_ContainsAppTitle</sub> | 🟩 85 | 🟩 85 | 🟡 70 | 🟡 75 | 🟡 79 |
| <sub>FetchStatusAsync_WhenCalled_ReturnsHealthy</sub> | 🟢 90 | 🟢 90 | 🟩 85 | 🟩 85 | 🟩 88 |
| <sub>ClientRootRequest_WhenServed_MatchesPublishedIndexHtml</sub> | 🟥 30 | 🟥 40 | 🔴 20 | 🟠 60 | 🟥 38 |
| <sub>PlatformApiClient_CallsApi_ReturnsHealthStatus</sub> | 🟡 70 | 🟩 80 | 🟠 60 | 🟩 80 | 🟡 73 |
| <sub>Health_WhenCalled_ReturnsMockedStatus</sub> | 🟩 80 | 🟩 85 | 🟠 65 | 🟩 80 | 🟡 78 |
| <sub>RootRequest_WhenFrontendConfigured_ReturnsFakeIndex</sub> | 🟩 80 | 🟩 85 | 🟠 65 | 🟩 80 | 🟡 78 |
| <sub>ClientRootRequest_WhenServed_MatchesExpectedIndex</sub> | 🟥 30 | 🟥 40 | 🔴 20 | 🟠 60 | 🟥 38 |
| <sub>All_Facts_And_Theories_Have_Category_Trait</sub> | 🟢 95 | 🟢 95 | 🟩 90 | 🟡 85 | 🟢 91 |


