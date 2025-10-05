# DotNetApp

## Local CI Acceleration / Self-Hosted Runner (WSL)

You can speed up iterative CI test runs by installing a Linux (Ubuntu) GitHub Actions runner inside WSL on your Windows machine.

Documentation & script:

`scripts/README.setup-wsl-github-runner.md`

Quick start (will prompt for repo + token if omitted):

```
pwsh -File .\scripts\setup-wsl-github-runner.ps1 -Service
```

VS Code tasks are provided (Run Task → "Setup WSL GitHub Runner ..."). Use the uninstall task to remove the runner safely. The setup enables systemd in WSL, installs dependencies, registers the runner with labels `self-hosted,linux,x64,local`, and optionally runs it as a service.

---

## Test Suite Notes

E2E Playwright tests are tagged with `Category=E2E`.
Run all non‑E2E tests:
```
dotnet test -c Debug --no-build --filter Category!=E2E
```

To enable E2E tests locally, install Playwright browsers first (once per machine):
```
# From repository root after a build
pwsh tests/DotNetApp.E2ETests/bin/Debug/net8.0/playwright.ps1 install
```
Then run including E2E:
```
dotnet test -c Debug --no-build
```

### Test Categories

Common category filters (all use `Trait("Category", ...)`):

```
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=E2E"
dotnet test --filter "Category=CodeGen"            # Source generator focused tests
dotnet test --filter "Category=Unit|Category=CodeGen"  # Combine fast suites
dotnet test --filter "Category!=E2E"                # Everything except slow E2E
```

### Conventions

All `[Fact]` / `[Theory]` tests (except intentionally skipped placeholders) must declare at least one `Category` trait. A convention test will fail the build if a test is missing a category.

### Code Coverage

Collect cross-platform code coverage (lcov, opencover, cobertura):

```
dotnet test DotNetApp.sln -c Debug --collect "XPlat Code Coverage" --filter "Category!=E2E"
```

Outputs (per test project) are written under `TestResults/<guid>/` with the chosen formats. For a unified lcov summary you can optionally use report generation tools (e.g. `reportgenerator`) in CI.

Exclude patterns configured: xUnit + FluentAssertions assemblies, generated/compiler code, EF Migrations, auto-properties.

### CI Coverage & Badges

GitHub Actions workflow (`.github/workflows/ci.yml`) runs tests (excluding `Category=E2E`), gathers coverage, and produces:
* HTML report (artifact: `coverage-report`)
* Markdown summary comment on pull requests
* Badge images (artifact: `coverage-badges`) named `badge_branchcoverage.svg`, `badge_linecoverage.svg`, etc.

To surface a badge in the README you can manually download and commit one, or publish via Pages. Example (if committed under `docs/coverage/badge_linecoverage.svg`):

```
![Line Coverage](docs/coverage/badge_linecoverage.svg)
```

To auto-publish badges to a `gh-pages` branch, add a subsequent job that pushes the `coverage-report` directory to `gh-pages` (not included by default to avoid unintended branch writes).

When running on `main`, the workflow now deploys the HTML coverage report to GitHub Pages. Once Pages is enabled for this repository, you can view it at:

`https://<owner>.github.io/<repo>/index.html` (replace with actual owner/repo). If the site is not yet enabled, enable Pages (Source: GitHub Actions) in repository settings.

Aggregating local coverage
-------------------------
If you run tests locally and want a single HTML report that combines coverage from multiple test projects, use the helper script:

```powershell
# From repository root
pwsh .\scripts\aggregate_coverage.ps1
```

The script will search for `coverage.cobertura.xml` files created by running tests with coverlet (cobertura format) and will run ReportGenerator to produce `artifacts/coverage-report/index.html`.


## Generated API Client
Contracts decorated with `[ApiContract]` and per-method `[Get]`, `[Post]`, `[Put]`, `[Delete]` generate typed clients at build via the `DotNetApp.CodeGen` source generator.

Features:
- Route param interpolation `{id}`
- Query string construction from uncaptured parameters
- Optional body parameter via `[Body]`
- Basic retry `[Retry(attempts, delayMs)]`

## Migration
Legacy `AddDotNetAppClient` is obsolete; use `AddPlatformApi(baseAddress)` (or the overload with `HttpMessageHandler` for tests).
