# GitHub Copilot Instructions for DotNetApp

This repository is a **Blazor/.NET 8 Game Template** for building multiplayer game applications. When working with this codebase, follow these guidelines:

## Architecture Overview

### Technology Stack
- **.NET 8** SDK
- **Blazor WebAssembly** for client applications
- **SignalR** for real-time multiplayer communication
- **xUnit** for testing
- **Coverlet** for code coverage
- **Playwright** for E2E testing

### Project Structure
- `src/Shared/DotNetApp.Core/` - Core abstractions and services (game state, messaging)
- `src/DotNetApp.Server/` - ASP.NET Core server with SignalR hub and REST API
- `src/DotNetApp.Client/` - Blazor WebAssembly client library
- `examples/Chess/` - Turn-based persistence example
- `examples/Pong/` - Real-time SignalR example
- `tests/` - Unit, integration, and E2E tests for all projects

### Key Abstractions
- `IGameStateService` - Interface for game state persistence (InMemory and Cosmos DB implementations)
- `GameHub` - SignalR hub for real-time game communication with room management
- `GameMessage` - Base message contract for real-time events
- `GameState` - Base model for persisted game state

## Coding Conventions

### C# Style
- Follow **MSDN C# Coding Conventions**
- Use `async`/`await` for all async operations
- Prefer dependency injection over static classes
- Use nullable reference types where appropriate
- Keep methods focused and single-purpose

### Testing Requirements
- **100% code coverage** for shared libraries (`DotNetApp.Core`, `DotNetApp.Server`)
- Add tests for all new features in appropriate test projects:
  - Unit tests: `tests/*Tests.Unit/`
  - Integration tests: `tests/*Tests.Integration/`
  - E2E tests: `tests/*Tests.E2E/`
- Use xUnit test framework with `[Fact]` and `[Theory]` attributes
- Use `[Trait("Category", "Unit")]` or `[Trait("Category", "Integration")]` for test categorization

### Naming Conventions
- Interfaces: `IGameStateService`
- Async methods: `SaveGameStateAsync`, `GetGameStateAsync`
- Test classes: `{ClassName}Tests` 
- Test methods: `{MethodName}_{Scenario}_{ExpectedBehavior}`

## Building and Testing

### Build Commands
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/DotNetApp.Server/DotNetApp.Server.csproj
```

### Test Commands
```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only integration tests
dotnet test --filter "Category=Integration"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

### Coverage Reporting
```bash
# Aggregate coverage files (PowerShell)
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/aggregate_coverage.ps1

# Generate coverage HTML report (PowerShell)
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/generate_coverage_html.ps1
```

## Adding New Features

### Creating a New Game
1. Choose transport method:
   - **Persistence-focused** (turn-based): Use `IGameStateService`
   - **Real-time** (fast-paced): Use SignalR `GameHub`

2. Create new Blazor WebAssembly project:
   > **Choose the appropriate template:**
   > - For games requiring server interaction (multiplayer, SignalR, REST API), use the **hosted** template:
   >   ```bash
   >   dotnet new blazorwasm --hosted -n MyGame
   >   cd MyGame
   >   dotnet add reference ../Shared/DotNetApp.Core/DotNetApp.Core.csproj
   >   # Add server-side references as needed in MyGame.Server
   >   # Add client-side references in MyGame.Client
   >   ```
   > - For purely client-side games, use the **standalone** template:
   >   ```bash
   >   dotnet new blazorwasm -n MyGame
   >   cd MyGame
   >   dotnet add reference ../../src/Shared/DotNetApp.Core/DotNetApp.Core.csproj
   >   ```

3. Implement game logic following patterns in `examples/Chess/` or `examples/Pong/`

4. Add comprehensive tests to maintain 100% coverage

### Adding New Services
- Implement interfaces from `DotNetApp.Core`
- Register services in `Program.cs` with appropriate lifetime (Scoped, Transient, Singleton)
- Add unit tests for business logic
- Add integration tests for API endpoints or SignalR hubs

### Modifying Existing Code
- Maintain backward compatibility when possible
- Update existing tests before modifying implementation
- Ensure code coverage remains at 100% for shared libraries
- Update documentation if changing public APIs

## SignalR Hub Development
- Use group-based room management for multiplayer games
- Implement proper connection/disconnection handling
- Send strongly-typed messages derived from `GameMessage`
- Test hub methods with integration tests using `WebApplicationFactory`

## State Management
- Keep all game state serializable (JSON)
- Validate state transitions in service layer
- Handle concurrent access with appropriate locking or versioning
- Test edge cases (null states, concurrent updates, disconnections)

## Dependencies
- Use centralized package management via `Directory.Packages.props`
- Keep dependencies minimal and up-to-date via Dependabot
- Only add new packages when necessary and approved
- Prefer standard .NET libraries over third-party alternatives

## CI/CD Integration
- All PRs must pass CI checks (build + tests)
- Coverage reports are automatically published to GitHub Pages
- Use GitHub Actions workflow defined in `.github/workflows/test-and-publish.yml`

## Documentation
- Update `README.md` for user-facing changes
- Update `CONTRIBUTING.md` for developer-facing changes
- Keep code examples in sync with actual implementation
- Document complex algorithms or business logic in code comments

## Common Patterns

### Dependency Injection
```csharp
// In Program.cs
builder.Services.AddScoped<IGameStateService, InMemoryGameStateStore>();
```

### Async Service Methods
```csharp
public async Task<GameState?> GetGameStateAsync(string gameId)
{
    // Implementation
}
```

### SignalR Hub Methods
```csharp
public async Task SendMessage(string room, GameMessage message)
{
    await Clients.Group(room).SendAsync("ReceiveMessage", message);
}
```

### Test Setup with WebApplicationFactory
```csharp
[Trait("Category", "Integration")]
public class GameHubTests : IClassFixture<WebApplicationFactory<Program>>
{
    // Test implementation
}
```

## References
- See `README.md` for getting started guide
- See `CONTRIBUTING.md` for detailed architecture and contribution guidelines
- Check `examples/` folder for implementation patterns
