# DotNetApp

DotNetApp is a demo starter built to showcase clean, maintainable **.NET 8** patterns while following **MSDN best practices** and **xUnit testing guidelines**.

## Features

✔️ Basic .NET 8 Web API + Blazor WebAssembly app template  
✔️ Unit tests (xUnit, bUnit), integration tests, and optional E2E tests (Playwright)  
✔️ GitHub Actions Runner for private repo workflow runs ([hutchisonkim/github-runner](https://github.com/hutchisonkim/github-runner))  
✔️ GitHub Actions workflows for deployment and diagnostics  
✔️ GitHub Pages integration for build artifacts  
🚧 End-to-end programmatic orchestration  
🚧 Full code coverage tracking  

## Build Configuration

The repository uses hierarchical `Directory.Build.props` files to enforce different quality standards for production vs. test code:

- **Production code** (`src/`): Strict settings with `TreatWarningsAsErrors=true` and Roslynator analyzers enabled
- **Test code** (`tests/`): Relaxed settings with `TreatWarningsAsErrors=false` and analyzers disabled to prevent brittle build failures

This approach maintains high code quality for production while allowing more flexibility in test projects.

## Coverage

![Test Coverage](https://hutchisonkim.github.io/dot-net-app/coverage-summary.svg)
