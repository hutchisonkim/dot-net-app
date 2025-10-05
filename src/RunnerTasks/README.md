RunnerTasks

This library contains the programmatic Docker-based runner orchestration used by the test suites.

Purpose
- Replace CLI/script-based docker flows with programmatic Docker.DotNet flows.
- Provide an adapter (`IDockerClientWrapper`) so tests can inject fakes for negative-path testing.

Test hooks
- To make tests robust without reflection, a small set of test helper methods are intentionally exposed on some classes (e.g. `DockerDotNetRunnerService.Test_SetInternalState`).
- These helpers are intended for test use only and are acceptable to remain public for the short term. If you prefer stronger encapsulation, we can switch them back to `internal` and rely on `InternalsVisibleTo`.

Coverage
- The tests project for RunnerTasks emits cobertura XML at `tests/RunnerTasks.Tests/coverage.cobertura.xml` when run with coverlet.
- Use the workspace-level `scripts/aggregate_coverage.ps1` to collect all coverage XML outputs and produce a combined HTML report (ReportGenerator required or installed as a dotnet tool).

Notes
- This project targets .NET 8 and depends on Docker.DotNet. It's designed to be used only within test contexts and gated integration runs.
