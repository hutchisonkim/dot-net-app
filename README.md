## DotNetApp

A .NET 8 sample that pairs an ASP.NET Core API with a Blazor WebAssembly client, backed by unit/integration/E2E tests, Docker tooling, and GitHub Actions.

![Test Coverage](https://hutchisonkim.github.io/dot-net-app/coverage-summary.svg)

## Features

- [x] ASP.NET Core API with a health endpoint (`GET /api/state/health`) and Swagger/OpenAPI.
- [x] Blazor WebAssembly client that displays API health using DI and a typed API client.
- [x] DI extension (`AddPlatformApi`) registering a typed `HttpClient` and `IPlatformApi` adapter.
- [x] API can serve the Blazor static assets directly in dev/integration via a pluggable asset configurator.
- [x] Unit tests (xUnit, bUnit) for client components and services.
- [x] Integration tests for API and client (WebApplicationFactory, shared fixtures, HTTP retry helpers).
- [x] Optional Playwright E2E tests (gated by RUN_E2E) to validate the client loads and runs.
- [x] Dockerfiles for API, Client, and test runners plus a CI-focused docker-compose.
- [x] Programmatic Docker orchestration library (`RunnerTasks`) used by tests to manage runners/containers.
- [x] GitHub Actions workflows for robust self-hosted CI, diagnostics, and publishing coverage to GitHub Pages.
- [x] Code coverage aggregation with HTML and SVG outputs (coverage badge embedded above).
- [x] Self-hosted GitHub runner stack (Docker-based) with VS Code tasks to start/stop locally.

