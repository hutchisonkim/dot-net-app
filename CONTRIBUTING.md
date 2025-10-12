# Contributing

## MSDN / xUnit naming and project organization reminders
- Project names use the convention: `Examples.{Name}.{TestType}` (e.g., `Examples.Chess.Unit`).
- Test class names should be `{ClassName}Tests` and test methods should follow `{MethodName}_{Scenario}_{Expected}` when practical.
- Use `[Trait("Category","Integration")]` or `[Trait("Category","UI")]` to categorize tests for CI filtering.

## Rules
- Always follow MSDN and xUnit conventions;
- Avoid creating new documentation files;
- Avoid modifying existing documentation files;
- Avoid writing comments in code and project files;
- - Except for comment summaries in public APIs;
- - Except for placeholder comments like `// TODO: Implement this method`;
- Never cheat a test: Always let the test fail if the functionality is not implemented or broken;