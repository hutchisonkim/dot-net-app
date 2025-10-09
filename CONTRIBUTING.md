# Contributing to DotNetApp

Thank you for your interest in contributing to DotNetApp!

## Build Configuration

### Assembly Information Generation

This repository uses a per-project approach for assembly information generation rather than a global setting. This allows most projects to benefit from auto-generated assembly attributes (version, product, company, etc.) while giving specific projects the ability to opt out when needed.

#### Projects with `GenerateAssemblyInfo=false`

The following projects explicitly disable assembly info generation:

- **DotNetApp.Server**: Avoids duplicate attribute errors in container builds
- **DotNetApp.Client**: Avoids duplicate attribute errors in container builds and Blazor WebAssembly packaging

For all other projects, the .NET SDK automatically generates standard assembly attributes (AssemblyVersion, AssemblyFileVersion, AssemblyProduct, etc.) from project properties.

#### When to Disable Assembly Info Generation

Only disable `GenerateAssemblyInfo` in a project file if:
1. You're experiencing duplicate attribute compilation errors
2. Container or packaging builds conflict with auto-generated attributes
3. You need custom assembly attributes defined in code

If you need to disable it, add this to your project file with a comment explaining why:

```xml
<!-- Disable SDK-generated assembly attributes to avoid duplicate attribute errors in container builds -->
<GenerateAssemblyInfo>false</GenerateAssemblyInfo>
```

## Testing

Run tests using the provided test categories:
- Unit tests: `dotnet test --filter "Category=Unit"`
- Integration tests: `dotnet test --filter "Category=Integration"`
- E2E tests: `dotnet test --filter "Category=E2E"` (requires Playwright setup)

## Code Standards

- Follow .NET naming conventions and coding standards
- Write unit tests for new functionality
- Ensure all tests pass before submitting a pull request
- Code analysis and warnings are treated as errors (`TreatWarningsAsErrors=true`)
