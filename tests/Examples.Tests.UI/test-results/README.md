# Chess Persistence Test Results

This directory contains test results from the chess persistence UI tests that verify save/load functionality.

## Test Flows

### Test 1: CompleteFlow_StartMoveSaveMoveLoad_RestoresPawnPosition

**Flow**: start → move → save → move → load

**Purpose**: Verifies that loading a saved game restores the exact board state at save time, discarding any moves made after saving.

**Steps**:
1. **Start** - Create a new chess game with initial board setup
2. **First Move** - Make a move (white pawn from e2 to e4)
3. **Save** - Save the game state (with white pawn at e4)
4. **Second Move** - Make another move (black pawn from e7 to e5)
5. **Load** - Load the saved game state, restoring to step 3 (white pawn at e4, black pawn at e7)

**Key Verification**: After loading, the board returns to the saved state with white pawn at e4 and black pawn at e7 (not at e5).

**Files**:
- `chess_complete_flow.gif` - Animated GIF showing all 5 steps
- `chess_complete_flow_merged.png` - Merged PNG showing all 5 frames side-by-side
- `chess_flow_1_start.html` through `chess_flow_5_load.html` - Individual HTML screenshots

---

### Test 2: CompleteFlow_StartMoveSaveNewLoad_ShowsSinglePawnMoved

**Flow**: start → move → save → new → load

**Purpose**: Verifies that after creating a new game (which resets the board), loading the saved game restores the previous game state.

**Steps**:
1. **Start** - Create a new chess game with initial board setup
2. **Move** - Make a move (white pawn from e2 to e4)
3. **Save** - Save the game state (with white pawn at e4)
4. **New** - Click "New Game" to reset the board to initial state
5. **Load** - Load the saved game state, restoring the moved pawn position

**Key Verification**: After loading, the board shows the white pawn at e4 (saved state), not the initial board from step 4.

**Files**:
- `chess_flow2_start_move_save_new.gif` - Animated GIF showing all 5 steps
- `chess_flow2_start_move_save_new_merged.png` - Merged PNG showing all 5 frames side-by-side
- `chess_flow2_1_start.html` through `chess_flow2_5_load.html` - Individual HTML screenshots

---

### Test 3: CompleteFlow_StartMoveMoveEatSaveNewLoad_ShowsPawnMovedTwiceAndEaten

**Flow**: start → move → move → eat → save → new → load

**Purpose**: Verifies that save/load correctly preserves complex game states including piece captures.

**Steps**:
1. **Start** - Create a new chess game with initial board setup
2. **First Move** - Move white pawn from e2 to e4
3. **Second Move** - Move black pawn from e7 to e5
4. **Eat** - Move white pawn from e4 to d6, capturing the black pawn at d7
5. **Save** - Save the game state (with white pawn at d6, black pawn at d7 captured)
6. **New** - Click "New Game" to reset the board to initial state
7. **Load** - Load the saved game state, restoring the captured piece state

**Key Verification**: After loading, the board shows:
- White pawn at d6 (moved twice from e2 → e4, then capturing d7 → d6)
- Black pawn at d7 is missing (captured)
- Black pawn at e7 moved to e5
- All other pieces remain in their initial positions

**Files**:
- `chess_flow3_moves_and_capture.gif` - Animated GIF showing all 7 steps
- `chess_flow3_moves_and_capture_merged.png` - Merged PNG showing all 7 frames side-by-side
- `chess_flow3_1_start.html` through `chess_flow3_7_load.html` - Individual HTML screenshots

---

## Implementation Details

### Save/Load Mechanism
- Uses static in-memory dictionary keyed by game ID
- Each save stores the complete board state and move count
- Load restores both the board configuration and move count

### Game ID Preservation
- The "New Game" button preserves the existing game ID (if one exists)
- This allows save/load to work correctly when resetting the board
- First click of "New Game" generates a new ID; subsequent clicks reuse it

### Move Sequence
- Move 1: White pawn advances from e2 to e4
- Move 2: Black pawn advances from e7 to e5 (alternating turns as in real chess)
- Move 3: White pawn captures black d7 pawn by moving from e4 to d6

### Capture Functionality
- Third move pattern: white pawn captures black pawn (e4 to d6, removing d7)
- Demonstrates that save/load preserves both piece positions and captured pieces

## Visualizations

All test flows generate:
1. **Animated GIF** - Shows the complete workflow with actual chess board rendering
2. **Merged PNG** - Shows all frames side-by-side for easy comparison
3. **Individual HTML files** - Full HTML capture of each step for detailed inspection

Each visualization includes:
- Game ID
- Game Type (Chess)
- Last Updated timestamp
- Complete chess board with Unicode piece symbols
- All UI buttons and their states

