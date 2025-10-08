# Test-suite summary (selected test methods across the solution)

This document summarizes the test methods found across the solution (unit / integration / E2E / infra tests), their location, dependencies, pertinence, and recommended placement in the solution. Links point to the source files in the workspace.

---

## Unit tests

- [`DotNetApp.Server.Tests.Unit.CategoryConventionsTests.All_Facts_And_Theories_Have_Category_Trait`](tests/DotNetApp.Server.Tests.Unit/CategoryConventionsTests.cs) — [tests/DotNetApp.Server.Tests.Unit/CategoryConventionsTests.cs](tests/DotNetApp.Server.Tests.Unit/CategoryConventionsTests.cs)  
  - What it does: scans the assembly for [Fact]/[Theory] methods and enforces that each has a `[Trait("Category", ...)]` attribute.  
  - Dependencies: xUnit attributes and reflection (no runtime services).  
  - Pertinence: enforces test categorization policy (useful for CI filtering).  
  - Placement: belongs in the unit-tests assembly for the API (`tests/DotNetApp.Server.Tests.Unit`) as a convention test.
  - Score: 90/100 — 🥇 Gold
    - Quick rationale: Highly isolated and deterministic (reflection-based), very fast, and enforces a team policy that helps CI categorization. Good coverage for a convention test.

- [`DotNetApp.Client.Tests.Unit.IndexTests.Index_WhenRendered_ContainsAppTitle`](tests/DotNetApp.Client.Tests.Unit/IndexTests.cs) — [tests/DotNetApp.Client.Tests.Unit/IndexTests.cs](tests/DotNetApp.Client.Tests.Unit/IndexTests.cs)  
  - What it does: bUnit component test that renders the Blazor `Index` page and asserts it contains the app title.  
  - Dependencies: bUnit, a fake HTTP handler registered via `ctx.Services.AddPlatformApi(...)` (test-only DI).  
  - Pertinence: verifies basic client UI rendering in isolation; fast and appropriate as a unit test.  
  - Placement: Unit tests for the client; lives next to other bUnit tests.
  - Score: 85/100 — 🥈 Silver
    - Quick rationale: Fast and isolated thanks to bUnit and DI fakes; small dependency surface. Slight maintenance cost if component structure changes often.

- [`DotNetApp.Client.Tests.Unit.IndexTests.Index_Renders_Welcome`](tests/DotNetApp.Client.Tests.Unit/IndexTests.cs) — [tests/DotNetApp.Client.Tests.Unit/IndexTests.cs](tests/DotNetApp.Client.Tests.Unit/IndexTests.cs)  
  - What it does: same intent as the bUnit test above (variations exist in multiple test projects).  
  - Dependencies: bUnit and test DI.  
  - Pertinence / Placement: client unit-test suite.
  - Score: 85/100 — 🥈 Silver
    - Quick rationale: Same as the other client unit test — well-scoped, quick, and deterministic; keeps UI regressions small and visible.

---

## Integration tests

- [`DotNetApp.Server.Tests.Integration.ServeFrontendFromBackendTests.ClientRootRequest_WhenServed_MatchesExpectedIndex`](tests/DotNetApp.Server.Tests.Integration/ServeFrontendFromBackendTests.cs) — [tests/DotNetApp.Server.Tests.Integration/ServeFrontendFromBackendTests.cs](tests/DotNetApp.Server.Tests.Integration/ServeFrontendFromBackendTests.cs)  
  - What it does: starts the app (WebApplicationFactory / test host or local static host fixture), requests `/` and compares served HTML to an expected index (searches for published index or falls back to razor source). Asserts title/content matches expectations.  
  - Dependencies: [`DotNetApp.Tests.Shared.LocalStaticFrontendFixture`](tests/Shared/LocalStaticFrontendFixture.cs), HTTP client helpers, optional Docker compose collection when running end-to-end.  
  - Pertinence: validates that the API correctly serves the client static assets (important integration concern).  
  - Placement: Integration test assembly (`tests/DotNetApp.Server.Tests.Integration`) — correct.
  - Score: 75/100 — 🥉 Bronze
    - Quick rationale: Valuable integration coverage (ensures API serves client assets), but depends on published artifacts or runtime resolution and is more brittle/slower than unit tests.

- [`DotNetApp.Server.Tests.Integration.HealthEndpointIntegrationTests.Health_WhenCalled_ReturnsMockedStatus`](tests/DotNetApp.Server.Tests.Integration/HealthEndpointIntegrationTests.cs) — [tests/DotNetApp.Server.Tests.Integration/HealthEndpointIntegrationTests.cs](tests/DotNetApp.Server.Tests.Integration/HealthEndpointIntegrationTests.cs)  
  - What it does: uses WebApplicationFactory to call `/api/state/health` and asserts the mocked health service returns the expected payload.  
  - Dependencies: Microsoft.AspNetCore.Mvc.Testing, a fake `IHealthService` registered in the test DI.  
  - Pertinence: validates pipeline/DI wiring and endpoint behavior without full external dependencies — appropriate for integration tests.
  - Score: 85/100 — 🥈 Silver
    - Quick rationale: High value integration test because it checks routing/DI and uses fakes to remain deterministic. Slightly heavier than pure unit tests but robust.

- [`DotNetApp.Client.Tests.Integration.ServeMatchesPublishedTests` helpers] — [tests/DotNetApp.Client.Tests.Integration/ServeMatchesPublishedTests.cs](tests/DotNetApp.Client.Tests.Integration/ServeMatchesPublishedTests.cs)  
  - What it does: helper logic to locate published client index (bin/Debug/net8.0/wwwroot or source wwwroot) and normalize HTML for comparisons.  
  - Dependencies: file system, test HTTP clients.  
  - Pertinence: supports integration assertions that verify published assets are served unchanged; belongs in client integration tests.
  - Score: 70/100 — 🥉 Copper
    - Quick rationale: Helpers are necessary but rely on filesystem layout and build outputs, which can make them brittle across environments. Useful but not self-contained.

- [`DotNetApp.Client.Tests.Integration.ExampleApiIntegrationTests.PlatformApiClient_CallsApi_ReturnsHealthStatus`](tests/DotNetApp.Client.Tests.Integration/ExampleApiIntegrationTests.cs) — [tests/DotNetApp.Client.Tests.Integration/ExampleApiIntegrationTests.cs](tests/DotNetApp.Client.Tests.Integration/ExampleApiIntegrationTests.cs)  
  - What it does: uses a test WebApplicationFactory factory, creates an HTTP client and a platform API client wrapper, calls the health API and asserts the returned DTO has expected "Healthy" status.  
  - Dependencies: WebApplicationFactory<Program>, client wrapper (app contracts), integration test infra.  
  - Pertinence: smoke/integration test confirming client → API contract.
  - Score: 80/100 — 🥉 Bronze+
    - Quick rationale: Good contract test between client wrapper and API; relies on WebApplicationFactory but remains deterministic and informative.

---

## End-to-end (E2E) tests

- [`DotNetApp.Tests.E2E.PlaywrightTests.Client_Index_Loads_BlazorRuntime`](tests/DotNetApp.Tests.E2E/PlaywrightTests.cs) — [tests/DotNetApp.Tests.E2E/PlaywrightTests.cs](tests/DotNetApp.Tests.E2E/PlaywrightTests.cs)  
  - What it does: uses Playwright (shared browser fixture) to navigate to the client index and assert the Blazor runtime script is present in the loaded HTML.  
  - Dependencies: [`DotNetApp.Tests.E2E.PlaywrightSharedFixture`](tests/DotNetApp.Tests.E2E/PlaywrightSharedFixture.cs) (or the equivalent in `tests/DotNetApp.Tests.E2E`), Microsoft.Playwright, Dockerized browser dependencies when run in CI images.  
  - Pertinence: validates the real browser environment bootstraps the Blazor client — high value but expensive; gated by RUN_E2E.  
  - Placement: E2E tests project; correctly gated by build flag and isolated in its own project.
  - Score: 65/100 — 🐉 Legendary (E2E)
    - Quick rationale: Extremely valuable for real-world validation but heavy, slower, and more likely to be flaky in CI. Keep gated and run on stable runners.

- Playwright fixture(s): [`DotNetApp.Tests.E2E.PlaywrightSharedFixture`](tests/DotNetApp.Tests.E2E/PlaywrightSharedFixture.cs) — [tests/DotNetApp.Tests.E2E/PlaywrightSharedFixture.cs](tests/DotNetApp.Tests.E2E/PlaywrightSharedFixture.cs)  
  - What it does: creates a single Playwright instance and browser for the test collection (IAsyncLifetime).  
  - Dependencies: Microsoft.Playwright.
  - Score: 75/100 — 🥉 Bronze
    - Quick rationale: The fixture itself is well-scoped and reuse-friendly but still carries the Playwright dependency cost.

- Docker E2E runner dockerfile & helper: [docker/Dockerfile.tests.e2e](docker/Dockerfile.tests.e2e) and [docker/docker-compose.yml](docker/docker-compose.yml) — these are used to build the E2E runner image, install PW libs, and provide a script `/usr/local/bin/run-e2e.sh` invoked when RUN_E2E=1.

---

## Runner / Infrastructure tests (Docker / GitHub.Runner)

- [`GitHub.Runner.Docker.Tests.RunnerLogsIntegrationTests.RunnerLogs_Contain_ListeningForJobs_IntegrationOrMock`](tests/GitHub.Runner.Docker.Tests/RunnerLogsIntegrationTests.cs) — [tests/GitHub.Runner.Docker.Tests/RunnerLogsIntegrationTests.cs](tests/GitHub.Runner.Docker.Tests/RunnerLogsIntegrationTests.cs)  
  - What it does: attempts an integration path to exercise the Docker-based runner, verifies registration and the presence of "Listening for Jobs" in runner logs (searches container volumes), and uses fakes/mocks when the environment isn't available to keep CI stable.  
  - Dependencies: Docker.DotNet client (for live path), Docker engine socket, test helpers [`FakeRunnerService`](tests/GitHub.Runner.Docker.Tests/FakeRunnerService.cs), `TestLogger<T>` and log helpers.  
  - Pertinence: verifies the Docker-based self-hosted runner logging and behavior; important for infra-level confidence.  
  - Score: 72/100 — 🥉 Bronze
    - Quick rationale: Useful infra test but environment-dependent; the mock/fallback path improves determinism for CI.

- [`GitHub.Runner.Docker.Tests.FakeRunnerService`](tests/GitHub.Runner.Docker.Tests/FakeRunnerService.cs) — [tests/GitHub.Runner.Docker.Tests/FakeRunnerService.cs](tests/GitHub.Runner.Docker.Tests/FakeRunnerService.cs)  
  - Test helper implementing `IRunnerService` used to simulate register/start/stop semantics without actually invoking Docker.  
  - Score: 88/100 — 🥈 Silver+
    - Quick rationale: Very useful test double — isolated, fast, and eases reliable testing of higher-level orchestration.

- Test logging helpers:
  - [`GitHub.Runner.Docker.Tests.TestLogger<T>`](tests/GitHub.Runner.Docker.Tests/TestLogger.cs) — [tests/GitHub.Runner.Docker.Tests/TestLogger.cs](tests/GitHub.Runner.Docker.Tests/TestLogger.cs)  
  - [`GitHub.Runner.Docker.Tests.AssertWithLogs`](tests/GitHub.Runner.Docker.Tests/AssertWithLogs.cs) — [tests/GitHub.Runner.Docker.Tests/AssertWithLogs.cs](tests/GitHub.Runner.Docker.Tests/AssertWithLogs.cs)  
  - Purpose: capture structured logs and present last N lines on assertion failures to ease diagnosing integration test failures.
  - Score: 90/100 — 🏆 Platinum
    - Quick rationale: Excellent for observability and diagnosing CI flakes; low cost and high maintenance payoff.

---

## Project averages (based on tests summarized above)

- `DotNetApp.Server.Tests.Unit` — Average: 90/100 🥇
- `DotNetApp.Client.Tests.Unit` — Average: 85/100 🥈
- `DotNetApp.Server.Tests.Integration` — Average: 80/100 🥉
- `DotNetApp.Client.Tests.Integration` — Average: 75/100 🥉
- `DotNetApp.Tests.E2E` — Average: 70/100 🥉
- `tests/GitHub.Runner.Docker.Tests` — Average: 83/100 🥈

Additional tests present but not individually summarized above (quick inventory):

- `tests/DotNetApp.Server.Tests.Unit/StateControllerTests.cs`
- `tests/DotNetApp.Client.Tests.Unit/HealthStatusProviderTests.cs`
- `tests/GitHub.Runner.Docker.Tests/DockerDotNetRunnerServiceTests.cs`
- `tests/GitHub.Runner.Docker.Tests/RunnerManagerTests.cs`
- `tests/GitHub.Runner.Docker.Tests/StopContainersTests.cs`
- `tests/GitHub.Runner.Docker.Tests/AssertWithLogs.cs` (helper)

If you'd like, I can expand the summary to include those files (quick scan + one-line descriptions) and then recompute averages from the fuller list.

### How the scores were calculated

Scores follow xUnit / MSDN testing guidance: we weighted key qualities to produce a single fun score:

- Isolation (30%): can the test run without external services? Unit tests score high here.
- Repeatability / Determinism (30%): will the test produce the same result every run? Tests that mock/seed state score higher.
- Speed (20%): how fast is the test to run locally/CI? Quick unit tests score full points.
- Maintainability (20%): clarity, ease of update, and whether the test is brittle against UI or build layout changes.

Each test's short rationale maps those qualities into the final 0–100 score and a playful badge: Platinum / Gold / Silver / Bronze / Copper / Legendary (E2E).

### Short notes & next steps

- If you'd like, I can produce a small script to auto-scan the repository's test projects and calculate these scores programmatically (heuristic-based: uses attributes, references, and presence of Playwright/Docker) and then refresh `test-suite_summary.md` automatically.
- I can also create a shared library for the duplicated category convention tests as suggested earlier and wire it into the unit test projects.

---

## Notes on classification and dependencies

- Unit tests (bUnit, xUnit) live in projects named `*.Unit*` and keep external dependencies mocked/faked. Examples: [tests/DotNetApp.Client.Tests.Unit/IndexTests.cs](tests/DotNetApp.Client.Tests.Unit/IndexTests.cs), [tests/DotNetApp.Server.Tests.Unit/CategoryConventionsTests.cs](tests/DotNetApp.Server.Tests.Unit/CategoryConventionsTests.cs). These are correctly placed for fast runs and developer feedback.

- Integration tests use `WebApplicationFactory`, test containers or local static host fixtures and often depend on published client artifacts being present (search logic in [tests/DotNetApp.Client.Tests.Integration/ServeMatchesPublishedTests.cs](tests/DotNetApp.Client.Tests.Integration/ServeMatchesPublishedTests.cs)). They belong in `*.Tests.Integration` projects and often require longer execution or special CI services.

- E2E tests use Playwright and are gated via build-time flag `RunE2E` / Docker/RUN_E2E. Playwright fixtures are compiled conditionally (`#if RUN_E2E`) so local/CI runs that don't enable E2E avoid pulling heavy dependencies. See: [tests/DotNetApp.Tests.E2E/PlaywrightSharedFixture.cs](tests/DotNetApp.Tests.E2E/PlaywrightSharedFixture.cs) and [docker/Dockerfile.tests.e2e](docker/Dockerfile.tests.e2e).

- Runner/infrastructure tests interact with Docker (Docker.DotNet) and therefore are split between integration-style (live Docker) and mock paths to keep CI stable. See: [tests/GitHub.Runner.Tests/RunnerLogsIntegrationTests.cs](tests/GitHub.Runner.Tests/RunnerLogsIntegrationTests.cs) and [tests/GitHub.Runner.Tests/FakeRunnerService.cs](tests/GitHub.Runner.Tests/FakeRunnerService.cs).

---

## Top 3 recommendations (actionable)

1. Consolidate test-convention checks and reduce duplication
   - Rationale: multiple `CategoryConventionsTests` exist across test projects. Centralize the convention checker into a single reusable test library (or shared test helper) referenced by all test projects to avoid subtle drifts. Files: [tests/DotNetApp.Server.Tests.Unit/CategoryConventionsTests.cs](tests/DotNetApp.Server.Tests.Unit/CategoryConventionsTests.cs), [tests/DotNetApp.Server.Tests.Unit/CategoryConventionsTests.cs](tests/DotNetApp.Server.Tests.Unit/CategoryConventionsTests.cs).

2. Gate heavy E2E dependencies and provide robust CI artifacts
   - Rationale: Playwright/browser tooling is heavy. Continue gating with `RunE2E` and provide a reproducible CI artifact pipeline that publishes built client assets (so Docker E2E images use published assets via `compose.ci.yml`) to keep E2E deterministic. Files: [docker/Dockerfile.tests.e2e](docker/Dockerfile.tests.e2e), [docker/compose.ci.yml](docker/compose.ci.yml), [tests/DotNetApp.Tests.E2E/PlaywrightSharedFixture.cs](tests/DotNetApp.Tests.E2E/PlaywrightSharedFixture.cs).

3. Harden integration tests that rely on Docker/host resources
   - Rationale: tests that read Docker volumes or require engine sockets (e.g., `RunnerLogsIntegrationTests`) should have a deterministic retry/backoff and a documented local run mode. Where feasible, prefer the mock path in CI and run live Docker-only on dedicated integration runners. Files: [tests/GitHub.Runner.Tests/RunnerLogsIntegrationTests.cs](tests/GitHub.Runner.Tests/RunnerLogsIntegrationTests.cs), [tests/GitHub.Runner.Tests/FakeRunnerService.cs](tests/GitHub.Runner.Tests/FakeRunnerService.cs).

---
