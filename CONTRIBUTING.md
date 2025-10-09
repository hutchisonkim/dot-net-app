# Contributing to DotNetApp

Thank you for your interest in contributing to DotNetApp! This document provides guidelines and best practices for contributing to this project.

## Development Guidelines

### Testing Best Practices

#### bUnit/UI Tests

When writing tests for Blazor components using bUnit, follow these guidelines to ensure deterministic, fast, and reliable tests:

**✅ DO:**
- Use bUnit's `WaitForState()` or `WaitForAssertion()` methods to wait for async operations to complete
- Use `WaitForState()` to wait for a specific condition in the component's markup or state
- Use `WaitForAssertion()` to repeatedly assert until the condition is met or timeout occurs
- Keep test assertions simple and focused on specific component behavior

**❌ DON'T:**
- Use `Task.Delay()` in test methods to wait for async initialization or state updates
- Use arbitrary delays (like `await Task.Delay(100)`) before calling `WaitForState()` or assertions
- Mix async delays with bUnit's deterministic wait helpers

**Example - Incorrect (slow and flaky):**
```csharp
[Fact]
public async Task MyComponent_AfterLoad_ShowsData()
{
    // Arrange
    var cut = ctx.RenderComponent<MyComponent>();
    
    // ❌ DON'T: Using Task.Delay is slow and can still be flaky
    await Task.Delay(100);
    cut.WaitForState(() => !cut.Markup.Contains("Loading"));
    
    // Assert
    Assert.Contains("Data loaded", cut.Markup);
}
```

**Example - Correct (fast and deterministic):**
```csharp
[Fact]
public void MyComponent_AfterLoad_ShowsData()
{
    // Arrange
    var cut = ctx.RenderComponent<MyComponent>();
    
    // ✅ DO: Use WaitForState directly - it polls efficiently
    cut.WaitForState(() => !cut.Markup.Contains("Loading"));
    
    // Assert
    Assert.Contains("Data loaded", cut.Markup);
}
```

#### Why Avoid Task.Delay?

1. **Flakiness**: Arbitrary delays can cause intermittent test failures if async operations take slightly longer than expected
2. **Slow Tests**: Fixed delays make tests unnecessarily slow, even when the operation completes quickly
3. **Redundancy**: bUnit's wait helpers already poll efficiently until conditions are met
4. **Scalability**: As test suites grow, accumulated delays significantly increase CI/CD pipeline times

#### When Task.Delay is Acceptable

Task.Delay is acceptable in test infrastructure (e.g., `TestHttpMessageHandler`) to simulate real-world delays, but not in test logic itself.

## Pull Request Guidelines

- Write tests for your changes
- Ensure all tests pass locally before submitting
- Follow existing code style and conventions
- Keep changes focused and minimal
- Update documentation if needed

## Getting Started

1. Clone the repository
2. Run `dotnet restore` to restore dependencies
3. Run `dotnet build` to build the solution
4. Run `dotnet test` to execute all tests

For more information, see the [README.md](README.md).
