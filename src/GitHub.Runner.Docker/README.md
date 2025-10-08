GitHub.Runner.Docker

This library contains the Docker-focused runner orchestration used by the solution and tests.

Public types
- `DockerRunnerService` implements `IRunnerService` for Docker-based runners
- `RunnerManager` orchestrates registration, retries, start and stop

This project targets .NET 8 and depends on `Docker.DotNet`.
