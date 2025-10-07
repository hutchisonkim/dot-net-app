GitHub.RunnerTasks

This library contains the Docker-based runner orchestration used by the solution and tests.

What’s included
- DockerRunnerService: the single, supported implementation of IRunnerService (uses Docker.DotNet)
- RunnerManager: orchestration with retries and graceful stop/unregister semantics

Retired services (kept as stubs to prevent accidental use)
- DockerCliRunnerService
- DockerDotNetRunnerService
- DockerComposeRunnerService

Coverage
- The tests project for GitHub.RunnerTasks can emit cobertura XML at `tests/GitHub.RunnerTasks.Tests/coverage.cobertura.xml` when run with coverlet.
- Use the workspace-level `scripts/aggregate_coverage.ps1` to collect coverage and produce a combined HTML report.

Notes
- This project targets .NET 8 and depends on Docker.DotNet.
