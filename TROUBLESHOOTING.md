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