# Examples layout

This folder contains example apps that demonstrate usage patterns for the DotNetApp template. The examples are intentionally minimal and follow MSDN/.NET 8 and Blazor conventions.

Structure
- src/Examples/Chess - Blazor WASM example for a turn-based game
- src/Examples/Pong  - Blazor WASM example for a real-time SignalR game

Tests
Each example has dedicated test projects located under `tests/Examples.{Name}.{TestType}`. Recommended test types:
- Unit: fast, isolated tests using xUnit
- Integration: tests that exercise more of the stack (Category=Integration)
- UI: end-to-end or browser tests (Category=UI) - use Playwright or similar
- PolicyUnit: focused unit tests for policy engine and state validation

Guidelines
- Tests must follow xUnit naming conventions: `{ClassName}_{Scenario}_{ExpectedBehavior}` for methods, and `{ClassName}Tests` for test classes.
- Use `[Trait("Category","Integration")]` or `[Trait("Category","UI")]` to categorize tests for filtering in CI.
- Keep examples minimal here; migrate full example code incrementally from `examples/` if desired.
