## DotNetApp

A .NET 8 sample that pairs an ASP.NET Core API with a Blazor WebAssembly client, backed by unit/integration/E2E tests, Docker tooling, and GitHub Actions.

![Test Coverage](https://hutchisonkim.github.io/dot-net-app/coverage-summary.svg)

## Features

 - [x] Programmatic Docker orchestration library (`GitHub.GitHub.Runner`) used by tests to manage runners/containers.
- [x] Self-hosted GitHub runner stack (Docker-based) with VS Code tasks to start/stop locally.
- [x] GitHub Actions workflows for robust self-hosted CI, diagnostics, and publishing coverage to GitHub Pages.
- [x] Unit tests (xUnit, bUnit) for client components and services.
- [x] Integration tests for API and client (WebApplicationFactory, shared fixtures, HTTP retry helpers).
- [x] Optional Playwright E2E tests (gated by RUN_E2E) to validate the client loads and runs.
- [x] Code coverage aggregation with HTML and SVG outputs (coverage badge embedded above).

## TODO

- [ ] Split the monorepo into three coordinated repositories and wire CI to consume them:
	- App repo (API + Blazor client)
	- GitHub.GitHub.Runner repo (programmatic Docker orchestration; publish as a NuGet package and reference from tests)
	- Self-hosted runner stack repo (Docker Compose + images; referenced via submodule or multi-checkout in CI)
- [ ] Apply programmatic orchestration end-to-end: replace remaining script/compose invocations with `GitHub.GitHub.Runner` across integration/E2E tests and local dev, and provide a single entry point (CLI or tool) for start/stop/teardown.
- [ ] Increase and enforce test coverage to 100% line/branch/method across API, client, and GitHub.Runner; gate in CI using coverlet outputs plus summary thresholds.
- [ ] Enforce branch protections and PR-only merges to protect `main`.