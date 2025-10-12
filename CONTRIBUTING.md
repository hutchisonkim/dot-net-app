# Contributing and local troubleshooting

This repository uses GitHub Actions to run CI and a set of manual workflows to help troubleshoot specific example projects.

## Running the mini CI jobs (manual)

There are manual (workflow_dispatch) workflows under `.github/workflows/` that run tests for the Chess example projects. These are intentionally manual so you can trigger them when troubleshooting a specific project or feature.

Workflows available (manual dispatch):
- `chess-unit-tests.yml` — runs `tests/Examples.Chess.Unit` (xUnit)
- `chess-integration-tests.yml` — runs `tests/Examples.Chess.Integration` (xUnit, Integration)
- `chess-ui-tests.yml` — runs `tests/Examples.Chess.UI` (UI / placeholder)
- `chess-policyunit-tests.yml` — runs `tests/Examples.Chess.PolicyUnit` (policy unit tests)
- `chess-all-tests.yml` — runs a combined build + all Chess test projects

How to run a manual workflow:
1. Open the repository's Actions page in GitHub.
2. Select a workflow (e.g., "Chess — Unit Tests").
3. Click "Run workflow" and select a branch, then click the green button to trigger.

Or use GitHub CLI:

```bash
# List workflows
gh workflow list

# Run a specific workflow by file name
gh workflow run .github/workflows/chess-unit-tests.yml -b main
```

## MSDN / xUnit naming and project organization reminders
- Project names use the convention: `Examples.{Name}.{TestType}` (e.g., `Examples.Chess.Unit`).
- Test class names should be `{ClassName}Tests` and test methods should follow `{MethodName}_{Scenario}_{Expected}` when practical.
- Use `[Trait("Category","Integration")]` or `[Trait("Category","UI")]` to categorize tests for CI filtering.

## Project debt & issue suggestions
While modernizing the example and test layout, I noted a few places that do not fully follow MSDN/xUnit or repository conventions. These are suggestions for follow-up issues to clean up debt:

1) Add Playwright-based E2E for UI tests
   - Problem: UI tests are placeholders (xUnit) and lack a real browser-based E2E runner.
   - Suggestion: Add Playwright project, test examples, and CI job to run UI/E2E tests in a container.

2) Consolidate test package versions / central package management
   - Problem: Some test projects previously defined explicit package versions; repository uses central package management in `Directory.Packages.props`.
   - Suggestion: Ensure all test projects omit explicit versions on `PackageReference` and rely on `Directory.Packages.props`.

3) Add sample launch configurations for debugging Blazor examples locally
   - Problem: No repo-level VS Code launch/debug configs for the example projects.
   - Suggestion: Provide `/.vscode/launch.json` snippets to run `src/Examples/Chess` and `src/Examples/Pong` locally with proper port mappings.

4) Remove or archive old `examples/` folders
   - Problem: Original `examples/` folders still exist (not used by the solution). They can cause confusion.
   - Suggestion: Archive or empty the files (you requested deletes be manual). Optionally add a migration note.

5) Update solution to include example tests in CI matrix
   - Problem: CI runs are generic; examples have dedicated workflows but the main CI doesn't include example test breakouts.
   - Suggestion: Add separate CI jobs for example unit/integration/UI tests with appropriate runners.

6) Uniform test naming and additional unit test coverage
   - Problem: Example test methods are placeholders; add specific method naming per MSDN/xUnit guidance and expand coverage.
   - Suggestion: Replace placeholders with real tests following `{Method}_{Scenario}_{Expected}` pattern.

