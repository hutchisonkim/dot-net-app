# Contributing to DotNetApp

Thank you for your interest in contributing to this Blazor/.NET 8 game template! This guide will help you understand the architecture and how to extend it for your own games.

## Architecture Overview

DotNetApp is designed as a **reusable template** for building multiplayer games with two primary transport methods:

1. **Persistence-focused**: Using `IGameStateService` for turn-based games (e.g., Chess)
2. **Real-time-focused**: Using SignalR for fast-paced multiplayer games (e.g., Pong)

### Core Components

#### DotNetApp.Core (Shared Library)

Located in `src/Shared/DotNetApp.Core/`, this library contains:

- **Abstractions**
  - `IGameStateService`: Interface for game state persistence
  - `IHealthService`: Service health check abstraction

- **Models**
  - `GameState`: Base model for persisted game state
  - `HealthStatus`: Health check model

- **Messages**
  - `GameMessage`: Base message for real-time communication

- **Services**
  - `InMemoryGameStateStore`: Fast, in-memory state storage
  - `CosmosGameStateStore`: Production-ready Azure Cosmos DB storage

#### DotNetApp.Server

Located in `src/DotNetApp.Server/`, provides:

- **Hubs**
  - `GameHub`: SignalR hub for real-time game communication
  - Group-based room management
  - Player join/leave notifications
  - Message broadcasting

- **Controllers**
  - `StateController`: REST API for health checks

#### DotNetApp.Client

Located in `src/DotNetApp.Client/`, provides:

- **Services**
  - `GameHubClient`: Wrapper for SignalR client connection
  - Event handlers for game messages

## Creating a New Game

### Step 1: Choose Your Transport

**Persistence-focused games** (Turn-based, save/load required):
- Use `IGameStateService`
- Examples: Chess, Checkers, Card Games, RPGs

**Real-time games** (Fast synchronization required):
- Use SignalR `GameHub`
- Examples: Pong, Racing, Fighting Games, FPS

You can also use **both** for hybrid games (e.g., persistent player profiles + real-time gameplay).

### Step 2: Create Project Structure

```bash
# Create new Blazor WebAssembly project
dotnet new blazorwasm -n MyGame -o examples/MyGame

# Add reference to shared library
cd examples/MyGame
dotnet add reference ../../src/Shared/DotNetApp.Core/DotNetApp.Core.csproj

# Add to solution
cd ../..
dotnet sln add examples/MyGame/MyGame.csproj
```

### Step 3: Implement Game Logic

#### For Persistence-Focused Games

Create a service to interact with `IGameStateService`:

```csharp
using DotNetApp.Core.Abstractions;
using DotNetApp.Core.Models;

public class MyGameService
{
    private readonly IGameStateService _stateService;

    public MyGameService(IGameStateService stateService)
    {
        _stateService = stateService;
    }

    public async Task<GameState> CreateGame(string gameId)
    {
        var state = new GameState
        {
            GameId = gameId,
            GameType = "MyGame",
            StateData = SerializeInitialState(),
            CreatedAt = DateTime.UtcNow
        };
        
        return await _stateService.SaveGameStateAsync(state);
    }

    public async Task<GameState?> LoadGame(string gameId)
    {
        return await _stateService.GetGameStateAsync(gameId);
    }

    private string SerializeInitialState()
    {
        // Serialize your game's initial state to JSON
        return JsonSerializer.Serialize(new { /* your state */ });
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddSingleton<IGameStateService, InMemoryGameStateStore>();
builder.Services.AddScoped<MyGameService>();
```

#### For Real-Time Games

Use SignalR client in your Razor component:

```razor
@page "/mygame"
@using Microsoft.AspNetCore.SignalR.Client
@using DotNetApp.Core.Messages
@implements IAsyncDisposable

<h1>My Game</h1>

@code {
    private HubConnection? hubConnection;
    private string gameId = "room-1";

    protected override async Task OnInitializedAsync()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/gamehub")
            .Build();

        hubConnection.On<GameMessage>("ReceiveGameMessage", (message) =>
        {
            // Handle game message
            StateHasChanged();
        });

        await hubConnection.StartAsync();
        await hubConnection.InvokeAsync("JoinGame", gameId);
    }

    private async Task SendMove(string moveData)
    {
        var message = new GameMessage
        {
            GameId = gameId,
            MessageType = "move",
            Payload = moveData,
            Timestamp = DateTime.UtcNow
        };
        
        await hubConnection!.InvokeAsync("SendGameMessage", message);
    }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }
    }
}
```

### Step 4: Add Tests

Create integration tests for your game:

```bash
# Create test project
dotnet new xunit -n MyGame.Tests.Integration -o tests/MyGame.Tests.Integration

# Add references
cd tests/MyGame.Tests.Integration
dotnet add reference ../../src/Shared/DotNetApp.Core/DotNetApp.Core.csproj
dotnet add package Microsoft.AspNetCore.Mvc.Testing

# Add to solution
cd ../..
dotnet sln add tests/MyGame.Tests.Integration/MyGame.Tests.Integration.csproj
```

Example test:
```csharp
using DotNetApp.Core.Services;
using DotNetApp.Core.Models;
using Xunit;

namespace MyGame.Tests.Integration;

[Trait("Category", "Integration")]
public class MyGameTests
{
    [Fact]
    public async Task MyGame_SaveAndLoad_PreservesState()
    {
        // Arrange
        var store = new InMemoryGameStateStore();
        var gameState = new GameState
        {
            GameId = "test-1",
            GameType = "MyGame",
            StateData = "{\"score\":100}",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await store.SaveGameStateAsync(gameState);
        var loaded = await store.GetGameStateAsync("test-1");

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("MyGame", loaded.GameType);
        Assert.Contains("100", loaded.StateData);
    }
}
```

### Step 5: Verify Coverage

Run tests with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/aggregate_coverage.ps1
```

Aim for 100% coverage of your game logic.

## Azure AD B2C Integration

To add authentication to your game:

### 1. Register Application in Azure AD B2C

1. Navigate to Azure Portal → Azure AD B2C
2. Register a new application
3. Note the **Application (client) ID** and **Tenant name**
4. Add redirect URIs for your app (e.g., `http://localhost:5000/authentication/login-callback`)

### 2. Configure Server

In `src/DotNetApp.Server/appsettings.json`:
```json
{
  "AzureAdB2C": {
    "Instance": "https://{tenant}.b2clogin.com/",
    "ClientId": "{client-id}",
    "Domain": "{tenant}.onmicrosoft.com",
    "SignUpSignInPolicyId": "B2C_1_signupsignin"
  }
}
```

In `src/DotNetApp.Server/Program.cs`:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));

// Later in the pipeline
app.UseAuthentication();
app.UseAuthorization();
```

### 3. Configure Client

Install package:
```bash
cd src/DotNetApp.Client
dotnet add package Microsoft.Authentication.WebAssembly.Msal
```

In `Program.cs`:
```csharp
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
});
```

### 4. Secure SignalR Hub

Add `[Authorize]` attribute to `GameHub`:
```csharp
[Authorize]
public class GameHub : Hub
{
    // Your hub methods
}
```

## Best Practices

### 1. Keep State Serializable
Always use JSON-serializable types for `GameState.StateData`:
```csharp
public class ChessGameState
{
    public string Board { get; set; } = "";
    public string CurrentTurn { get; set; } = "white";
    public List<string> MoveHistory { get; set; } = new();
}

// In your code
var stateJson = JsonSerializer.Serialize(chessState);
```

### 2. Handle Disconnections
For SignalR games, implement reconnection logic:
```csharp
hubConnection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect() // Built-in reconnection
    .Build();

hubConnection.Reconnected += async (connectionId) =>
{
    // Rejoin game room
    await hubConnection.InvokeAsync("JoinGame", gameId);
};
```

### 3. Test Edge Cases
- Network failures
- Concurrent updates
- Invalid game states
- Player disconnections

### 4. Use Strongly-Typed Messages
Define message types for clarity:
```csharp
public class MoveMessage
{
    public string GameId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
}
```

### 5. Validate State Transitions
Always validate moves/actions before updating state:
```csharp
public async Task<bool> MakeMove(string gameId, Move move)
{
    var state = await _stateService.GetGameStateAsync(gameId);
    if (state == null) return false;
    
    if (!IsValidMove(state, move)) return false;
    
    // Apply move
    var updatedState = ApplyMove(state, move);
    await _stateService.SaveGameStateAsync(updatedState);
    return true;
}
```

## Running Tests

### Unit Tests
```bash
dotnet test --filter "Category=Unit"
```

### Integration Tests
```bash
dotnet test --filter "Category=Integration"
```

### All Tests with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

## Code Style

This project follows:
- **MSDN C# Coding Conventions**
- **xUnit Testing Guidelines**
- **Blazor Best Practices**

Use the provided Roslyn analyzers:
```bash
dotnet build
# Analyzer warnings will be shown in build output
```

## Submitting Changes

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-game`)
3. Add tests for your changes
4. Ensure all tests pass (`dotnet test`)
5. Verify coverage meets 100% for new code
6. Commit your changes (`git commit -m 'Add MyGame'`)
7. Push to the branch (`git push origin feature/my-game`)
8. Open a Pull Request

## Questions?

Open an issue on GitHub or reach out to the maintainers.

Happy game building! 🎮
