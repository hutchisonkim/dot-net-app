# DotNetApp.Server workspace

This README sits next to the `DotNetApp.Server` project and documents common commands.

The API project now lives at `src/DotNetApp.Server/DotNetApp.Server.csproj` and the assembly/namespace is `DotNetApp.Server`.

Open the workspace by opening the `src/DotNetApp.Server` folder in VS Code.

Common tasks are available in `.vscode/tasks.json`:

- Build: "API: Build"
- Run: "API: Run"
- Test: "API: Test (Unit)"

PowerShell quick commands (from repo root):
```powershell
# build
dotnet build src/DotNetApp.Server/DotNetApp.Server.csproj -c Debug
# run
dotnet run --project src/DotNetApp.Server -c Debug
# unit tests
dotnet test tests/DotNetApp.Server.Tests.Unit/DotNetApp.Server.UnitTests.csproj -c Debug
```
