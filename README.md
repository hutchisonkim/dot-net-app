# DotNetApp

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

## Generated API Client
Contracts decorated with `[ApiContract]` and per-method `[Get]`, `[Post]`, `[Put]`, `[Delete]` generate typed clients at build via the `DotNetApp.CodeGen` source generator.

Features:
- Route param interpolation `{id}`
- Query string construction from uncaptured parameters
- Optional body parameter via `[Body]`
- Basic retry `[Retry(attempts, delayMs)]`

## Migration
Legacy `AddDotNetAppClient` is obsolete; use `AddPlatformApi(baseAddress)` (or the overload with `HttpMessageHandler` for tests).
