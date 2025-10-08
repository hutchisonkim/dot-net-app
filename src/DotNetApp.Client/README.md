# DotNetApp.Client workspace

This README is colocated with `DotNetApp.Client.code-workspace`.

Open the workspace by opening `src/DotNetApp.Client/DotNetApp.Client.code-workspace` in VS Code.

Common tasks are in `.vscode/tasks.json`:

- Build: "Client: Build"
- Run: "Client: Run"
- Test: "Client: Test (Unit)"

PowerShell quick commands (from repo root):
```powershell
# build
dotnet build src/DotNetApp.Client/DotNetApp.Client.csproj -c Debug
# run
dotnet run --project src/DotNetApp.Client -c Debug
# unit tests
dotnet test tests/DotNetApp.Client.Tests.Unit/DotNetApp.Client.UnitTests.csproj -c Debug
```
