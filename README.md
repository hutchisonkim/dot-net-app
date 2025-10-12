# DotNetApp - Blazor/.NET 8 Game Template

DotNetApp is a **reusable template** for building multiplayer game applications with **Blazor WebAssembly** and **.NET 8**. It provides a clean architecture with shared abstractions for both persistence-focused and real-time multiplayer games.

## Features

✔️ **Game State Management**: IGameStateService abstraction with InMemory and Cosmos DB implementations  
✔️ **Real-time Communication**: SignalR hub for multiplayer game synchronization  
✔️ **Example Games**: Chess (persistence-focused) and Pong (real-time-focused)  
✔️ **Comprehensive Testing**: Unit tests, integration tests, and E2E tests (Playwright)  
✔️ **100% Code Coverage**: Full coverage tracking with automated reporting  
✔️ **GitHub Actions CI/CD**: Automated build, test, and deployment workflows  
✔️ **GitHub Pages Integration**: Automated coverage reports and documentation  

## Architecture

### Shared Libraries

- **DotNetApp.Core**: Shared abstractions and services
  - `IGameStateService`: Interface for game state persistence
  - `InMemoryGameStateStore`: In-memory implementation for testing and single-instance games
  - `CosmosGameStateStore`: Azure Cosmos DB implementation for production
  - `GameHub`: SignalR hub for real-time game communication
  - `GameMessage`: Message contracts for real-time events

### Example Games

#### Chess (Persistence-Focused)
Located in `examples/Chess/`, demonstrates:
- Using `IGameStateService` for turn-based game state
- Save/load game mechanics
- Minimal Blazor UI components

#### Pong (Real-Time-Focused)
Located in `examples/Pong/`, demonstrates:
- Using SignalR for real-time player synchronization
- Group-based game rooms
- Event-driven game updates

## Getting Started

### Prerequisites

- .NET 8 SDK
- (Optional) Azure Cosmos DB Emulator for testing Cosmos persistence

### Building the Solution

```bash
dotnet build
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Generate coverage reports (requires PowerShell)
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/aggregate_coverage.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/generate_coverage_html.ps1
```

### Running the Examples

#### Chess Example
```bash
cd examples/Chess
dotnet run
```

#### Pong Example
```bash
# First, start the server with SignalR hub
cd src/DotNetApp.Server
dotnet run

# Then, in another terminal, start the Pong client
cd examples/Pong
dotnet run
```

## Creating a New Game

To create a new game using this template:

1. **Choose your transport**:
   - Use `IGameStateService` for turn-based or persistence-heavy games
   - Use SignalR (`GameHub`) for real-time multiplayer games

2. **Create a new Blazor WebAssembly project**:
   ```bash
   dotnet new blazorwasm -n MyGame
   cd MyGame
   dotnet add reference ../../src/Shared/DotNetApp.Core/DotNetApp.Core.csproj
   ```

3. **Implement your game logic**:
   - For persistence: Inject `IGameStateService` and use `SaveGameStateAsync`/`GetGameStateAsync`
   - For real-time: Use `GameHubClient` wrapper or directly use `HubConnection`

4. **Add tests**:
   - Create a test project in `tests/` directory
   - Follow patterns from `Examples.Tests.Integration` or `DotNetApp.Server.Tests.Integration.GameHub`

See [CONTRIBUTING.md](./CONTRIBUTING.md) for detailed guidance on extending the template.

## Azure AD B2C Integration

To enable authentication with Azure AD B2C:

1. Register your application in Azure AD B2C
2. Configure the client ID and tenant in `appsettings.json`
3. Add authentication middleware in `Program.cs`
4. See [CONTRIBUTING.md](./CONTRIBUTING.md) for detailed setup instructions

## Testing & Coverage

This repository maintains 100% code coverage for shared libraries and examples. Coverage is tracked per project:

- **DotNetApp.Core**: Game state and messaging abstractions
- **DotNetApp.Server**: API and SignalR hub
- **Examples**: Chess and Pong implementations

To verify coverage locally:
```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/aggregate_coverage.ps1
```

Coverage reports are automatically published to GitHub Pages on every merge to `main`.

## Coverage

![Test Coverage](https://hutchisonkim.github.io/dot-net-app/coverage-summary.svg)

## License

This project is provided as-is for educational and demonstration purposes.
