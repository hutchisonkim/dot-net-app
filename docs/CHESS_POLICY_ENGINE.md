# Chess Policy Engine

A characteristic-based policy engine for managing chess game rules in the DotNetApp framework.

## Overview

The Chess Policy Engine implements a flexible, reusable system for defining and enforcing game rules based on piece characteristics and abilities. This design pattern can be extended to other persistence-driven turn-based games.

## Architecture

### Core Concepts

1. **Characteristics** - Define movement patterns and behaviors for pieces
   - Examples: "Diagonal", "LShape", "Forward", "Orthogonal"
   - Configured with direction offsets, range limits, and path requirements

2. **Abilities** - Represent possible moves that can be performed
   - Each ability has an allowed/forbidden status
   - Forbidden abilities include a reason (e.g., "No piece to capture", "Path is blocked")

3. **Policy Engine** - Generates abilities from characteristics based on board state
   - Validates moves against current game state
   - Enforces rules like blocking, captures, and special moves

## Usage Example

```csharp
using DotNetApp.Core.Models;
using DotNetApp.Core.Services;

// Create initial chess board
var board = new ChessBoardState();
board[6, 4] = new ChessPiece(ChessPieceType.Pawn, ChessColor.White);

// Create policy engine
var policy = new ChessPolicy();

// Get available moves
var position = new Position(6, 4);
var abilities = policy.GetAbilities(position, board);

foreach (var ability in abilities.Where(a => a.IsAllowed))
{
    Console.WriteLine($"Can move to: {ability.Move.To}");
}
```

## Initial Chess Position

At the start of a chess game, the policy engine reports:

- **8 White Pawns**: 2 moves each (forward 1 or 2 squares) = 16 moves
- **2 White Knights**: 2 moves each (jumping to available squares) = 4 moves
- **Total**: 20 legal moves for White

This matches standard chess rules!

## Testing

60 comprehensive unit tests covering:

- All piece types and their characteristics
- Initial board position validation
- Forbidden ability detection (with reasons)
- Edge cases (boundaries, blocking, captures)

Run tests:
```bash
dotnet test --filter "Category=Unit"
```
