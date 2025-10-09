# Test files rename plan

This file lists proposed shorter, MSDN/xUnit-friendly filenames for test source files under `tests/`, with git-compatible rename commands you can run to apply them.

Notes:
- File names should match the public class name inside the file. When performing these renames you will also need to update the class name and/or namespace where applicable.
- Integration vs Unit scope is already expressed by the project folder (e.g. `DotNetApp.Client.Tests.Integration`). I shorten filenames to be concise while keeping intent.
- Where suggested, consider splitting or merging helper files to improve discoverability and reuse. See the "Split / merge suggestions" section below.

## Mappings (current -> proposed)

Client Integration
- tests/DotNetApp.Client.Tests.Integration/ExampleApiIntegrationTests.cs -> tests/DotNetApp.Client.Tests.Integration/ExampleApiTests.cs
- tests/DotNetApp.Client.Tests.Integration/ServeMatchesPublishedTests.cs -> tests/DotNetApp.Client.Tests.Integration/ServeMatchesTests.cs

Client Unit
- tests/DotNetApp.Client.Tests.Unit/HealthStatusProviderTests.cs -> tests/DotNetApp.Client.Tests.Unit/HealthProviderTests.cs
- tests/DotNetApp.Client.Tests.Unit/IndexTests.cs -> tests/DotNetApp.Client.Tests.Unit/IndexTests.cs
- tests/DotNetApp.Client.Tests.Unit/PlatformApiClientTests.cs -> tests/DotNetApp.Client.Tests.Unit/PlatformClientTests.cs
- tests/DotNetApp.Client.Tests.Unit/ServiceCollectionExtensionsTests.cs -> tests/DotNetApp.Client.Tests.Unit/ServiceCollectionTests.cs
- tests/DotNetApp.Client.Tests.Unit/TestHelpers.cs -> (split) tests/DotNetApp.Client.Tests.Unit/TestBuilders.cs, TestMocks.cs, TestAssertions.cs

Server Integration
- tests/DotNetApp.Server.Tests.Integration/HealthEndpointIntegrationTests.cs -> tests/DotNetApp.Server.Tests.Integration/HealthEndpointTests.cs
- tests/DotNetApp.Server.Tests.Integration/ServeFrontendFromBackendTests.cs -> tests/DotNetApp.Server.Tests.Integration/ServeFrontendTests.cs

Server Unit
- tests/DotNetApp.Server.Tests.Unit/CategoryConventionsTests.cs -> tests/DotNetApp.Server.Tests.Unit/CategoryConventionsTests.cs
- tests/DotNetApp.Server.Tests.Unit/DefaultHealthServiceTests.cs -> tests/DotNetApp.Server.Tests.Unit/DefaultHealthServiceTests.cs
- tests/DotNetApp.Server.Tests.Unit/HealthStatusTests.cs -> tests/DotNetApp.Server.Tests.Unit/HealthStatusTests.cs
- tests/DotNetApp.Server.Tests.Unit/StateControllerTests.cs -> tests/DotNetApp.Server.Tests.Unit/StateControllerTests.cs

E2E
- tests/DotNetApp.Tests.E2E/PlaywrightSharedFixture.cs -> tests/DotNetApp.Tests.E2E/PlaywrightFixture.cs
- tests/DotNetApp.Tests.E2E/PlaywrightTests.cs -> tests/DotNetApp.Tests.E2E/PlaywrightTests.cs

GitHub.Runner.Docker tests
- tests/GitHub.Runner.Docker.Tests/AssertWithLogs.cs -> tests/GitHub.Runner.Docker.Tests/LoggingAssertions.cs
- tests/GitHub.Runner.Docker.Tests/DockerDotNetRunnerServiceTests.cs -> tests/GitHub.Runner.Docker.Tests/DockerRunnerServiceTests.cs
- tests/GitHub.Runner.Docker.Tests/FakeRunnerService.cs -> tests/GitHub.Runner.Docker.Tests/FakeRunner.cs
- tests/GitHub.Runner.Docker.Tests/RunnerLogsIntegrationTests.cs -> tests/GitHub.Runner.Docker.Tests/RunnerLogsTests.cs
- tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs -> tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs
- tests/GitHub.Runner.Docker.Tests/StopContainersTests.cs -> tests/GitHub.Runner.Docker.Tests/ContainerStopTests.cs
- tests/GitHub.Runner.Docker.Tests/TestLogger.cs -> tests/GitHub.Runner.Docker.Tests/LoggingTestHelpers.cs

Shared
- tests/Shared/DockerComposeCollection.cs -> tests/Shared/DockerComposeFixture.cs
- tests/Shared/HttpRetry.cs -> tests/Shared/HttpRetryPolicy.cs

## Git commands (one-liners)
Run these from the repository root to apply each rename. They use `git mv` so git tracks renames.

```bash
# Client Integration
git mv "tests/DotNetApp.Client.Tests.Integration/ExampleApiIntegrationTests.cs" "tests/DotNetApp.Client.Tests.Integration/ExampleApiTests.cs"
git mv "tests/DotNetApp.Client.Tests.Integration/ServeMatchesPublishedTests.cs" "tests/DotNetApp.Client.Tests.Integration/ServeMatchesTests.cs"

# Client Unit
git mv "tests/DotNetApp.Client.Tests.Unit/HealthStatusProviderTests.cs" "tests/DotNetApp.Client.Tests.Unit/HealthProviderTests.cs"
git mv "tests/DotNetApp.Client.Tests.Unit/PlatformApiClientTests.cs" "tests/DotNetApp.Client.Tests.Unit/PlatformClientTests.cs"
git mv "tests/DotNetApp.Client.Tests.Unit/ServiceCollectionExtensionsTests.cs" "tests/DotNetApp.Client.Tests.Unit/ServiceCollectionTests.cs"
# For TestHelpers -> split: create new files and move relevant code manually

# Server Integration
git mv "tests/DotNetApp.Server.Tests.Integration/HealthEndpointIntegrationTests.cs" "tests/DotNetApp.Server.Tests.Integration/HealthEndpointTests.cs"
git mv "tests/DotNetApp.Server.Tests.Integration/ServeFrontendFromBackendTests.cs" "tests/DotNetApp.Server.Tests.Integration/ServeFrontendTests.cs"

# GitHub.Runner.Docker.Tests
git mv "tests/GitHub.Runner.Docker.Tests/AssertWithLogs.cs" "tests/GitHub.Runner.Docker.Tests/LoggingAssertions.cs"
git mv "tests/GitHub.Runner.Docker.Tests/DockerDotNetRunnerServiceTests.cs" "tests/GitHub.Runner.Docker.Tests/DockerRunnerServiceTests.cs"
git mv "tests/GitHub.Runner.Docker.Tests/FakeRunnerService.cs" "tests/GitHub.Runner.Docker.Tests/FakeRunner.cs"
git mv "tests/GitHub.Runner.Docker.Tests/RunnerLogsIntegrationTests.cs" "tests/GitHub.Runner.Docker.Tests/RunnerLogsTests.cs"
git mv "tests/GitHub.Runner.Docker.Tests/StopContainersTests.cs" "tests/GitHub.Runner.Docker.Tests/ContainerStopTests.cs"
git mv "tests/GitHub.Runner.Docker.Tests/TestLogger.cs" "tests/GitHub.Runner.Docker.Tests/LoggingTestHelpers.cs"

# Shared
git mv "tests/Shared/DockerComposeCollection.cs" "tests/Shared/DockerComposeFixture.cs"
git mv "tests/Shared/HttpRetry.cs" "tests/Shared/HttpRetryPolicy.cs"
```

## Split / merge suggestions (manual steps)
- Split `tests/DotNetApp.Client.Tests.Unit/TestHelpers.cs` into:
  - `TestBuilders.cs` (object builders / fixture data)
  - `TestMocks.cs` (preconfigured mocks / fakes)
  - `TestAssertions.cs` (reusable assertions)

- Merge logging helpers in `tests/GitHub.Runner.Docker.Tests` into `LoggingTestHelpers.cs` and/or `TestDoubles.cs`.

## Post-rename checklist
- Update any class names to match the new file names where applicable.
- Update namespaces if you move files between folders.
- Run `dotnet build` and `dotnet test` for the test projects.
- If CI references specific filenames, update CI configuration accordingly.

---

If you'd like, I can apply these renames automatically (perform `git mv`, update class names and namespaces, run build/tests) on a new branch and push it. Reply "apply renames" to proceed.
