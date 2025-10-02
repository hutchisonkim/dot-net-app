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


## Generated API Client
Contracts decorated with `[ApiContract]` and per-method `[Get]`, `[Post]`, `[Put]`, `[Delete]` generate typed clients at build via the `DotNetApp.CodeGen` source generator.

Features:
- Route param interpolation `{id}`
- Query string construction from uncaptured parameters
- Optional body parameter via `[Body]`
- Basic retry `[Retry(attempts, delayMs)]`

## Migration
Legacy `AddDotNetAppClient` is obsolete; use `AddPlatformApi(baseAddress)` (or the overload with `HttpMessageHandler` for tests).
