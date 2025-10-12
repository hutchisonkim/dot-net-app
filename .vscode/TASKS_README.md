# VS Code Tasks for Chess CI Tests

Use the included VS Code tasks to run the Chess test projects quickly.

Run: Ctrl+Shift+P -> Tasks: Run Task -> choose one of the tasks:

- Chess: Unit Tests (Examples.Chess.Unit)
- Chess: Integration Tests (Examples.Chess.Integration)
- Chess: UI Tests (Examples.Chess.UI)
- Chess: Policy Unit Tests (Examples.Chess.PolicyUnit)
- Chess: All Tests (sequence) — runs the above tasks in sequence

Terminal equivalents (PowerShell):

```powershell
dotnet test ./tests/Examples.Chess.Unit/Examples.Chess.Unit.csproj -c Release
dotnet test ./tests/Examples.Chess.Integration/Examples.Chess.Integration.csproj -c Release
```

Notes:
- Tasks use the `Release` configuration to match CI behavior.
- If you prefer `no-build` or other flags for faster runs, edit `.vscode/tasks.json` and add the appropriate `dotnet test` arguments.
