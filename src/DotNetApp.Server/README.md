# DotNetApp.Api workspace (assembly: DotNetApp.Server)

This README sits next to the `DotNetApp.Api.code-workspace` so it appears when opening the workspace.

The API project file keeps the path/name `src/DotNetApp.Api/DotNetApp.Api.csproj` but its AssemblyName/RootNamespace have been changed to `DotNetApp.Server`.

Open the workspace by opening `src/DotNetApp.Api/DotNetApp.Api.code-workspace` in VS Code.

Common tasks are available in `.vscode/tasks.json`:

- Build: "API: Build"
- Run: "API: Run"
- Test: "API: Test (Unit)"

PowerShell quick commands (from repo root):
```powershell
# build
dotnet build src/DotNetApp.Api/DotNetApp.Api.csproj -c Debug
# run
dotnet run --project src/DotNetApp.Api -c Debug
# unit tests
dotnet test tests/DotNetApp.Api.UnitTests/DotNetApp.Api.UnitTests.csproj -c Debug
```
