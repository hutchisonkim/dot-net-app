# Contributing

## MSDN / xUnit naming and project organization reminders
- Project names use the convention: `Examples.{Name}.{TestType}` (e.g., `Examples.Chess.Unit`).
- Test class names should be `{ClassName}Tests` and test methods should follow `{MethodName}_{Scenario}_{Expected}` when practical.
- Use `[Trait("Category","Integration")]` or `[Trait("Category","UI")]` to categorize tests for CI filtering.

## Rules
- Always follow MSDN and xUnit conventions;
- Avoid creating new documentation files;
- Avoid modifying existing documentation files;
- Avoid writing comments in code and project files;
- - Except for comment summaries in public APIs;
- - Except for placeholder comments like `// TODO: Implement this method`;
- Never cheat a test: Always let the test fail if the functionality is not implemented or broken;

# Troubleshooting

This repository uses GitHub Actions to run CI and a set of workflows to help troubleshoot specific example projects.

Workflows available for manual dispatch:
- `chess-unit-tests.yml` — runs `tests/Examples.Chess.Unit` (xUnit)
- `chess-integration-tests.yml` — runs `tests/Examples.Chess.Integration` (xUnit, Integration)
- `chess-ui-tests.yml` — runs `tests/Examples.Chess.UI` (UI / placeholder)
- `chess-policyunit-tests.yml` — runs `tests/Examples.Chess.PolicyUnit` (policy unit tests)
- `chess-all-tests.yml` — runs a combined build + all Chess test projects

How to run (example for Chess unit tests):
```bash
gh workflow run .github/workflows/chess-unit-tests.yml -b main
```