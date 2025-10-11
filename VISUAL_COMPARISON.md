# Visual Comparison: Before vs After

## The Problem

### Original Code (PR #36, commit 74f5acc) - ❌ INCORRECT

```csharp
public void MakeExampleMove()
{
    // Move 1: advance white e2 pawn to e4
    if (moveCount == 0 && boardState.ContainsKey((6, 4)))
    {
        boardState[(4, 4)] = boardState[(6, 4)]; // Move piece
        boardState.Remove((6, 4)); // Remove from old position
        moveCount++;
        lastUpdated = DateTime.UtcNow;
    }
    // Move 2: advance white e4 pawn to e5    ← ❌ WRONG COMMENT!
    else if (moveCount == 1 && boardState.ContainsKey((4, 4)))    ← ❌ WRONG COORDINATE!
    {
        boardState[(3, 4)] = boardState[(4, 4)]; // Move piece
        boardState.Remove((4, 4)); // Remove from old position
        moveCount++;
        lastUpdated = DateTime.UtcNow;
    }
}
```

**What's wrong:**
- ❌ Comment says "white e4 pawn to e5" but should be "black e7 pawn to e5"
- ❌ Code checks `(4, 4)` which is e4 (white's pawn) instead of `(1, 4)` which is e7 (black's pawn)
- ❌ This results in two consecutive white moves - violates chess rules!

### Visual Representation - INCORRECT BEHAVIOR

```
After Move 1 (White e2→e4):
  8  ♜ ♞ ♝ ♛ ♚ ♝ ♞ ♜
  7  ♟ ♟ ♟ ♟ ♟ ♟ ♟ ♟   ← Black pawns at e7
  6  ·  ·  ·  ·  ·  ·  ·  ·
  5  ·  ·  ·  ·  ·  ·  ·  ·
  4  ·  ·  ·  ·  ♙ ·  ·  ·   ← White pawn at e4
  3  ·  ·  ·  ·  ·  ·  ·  ·
  2  ♙ ♙ ♙ ♙ ·  ♙ ♙ ♙
  1  ♖ ♘ ♗ ♕ ♔ ♗ ♘ ♖
     a  b  c  d  e  f  g  h

After Move 2 (INCORRECTLY: White e4→e5):
  8  ♜ ♞ ♝ ♛ ♚ ♝ ♞ ♜
  7  ♟ ♟ ♟ ♟ ♟ ♟ ♟ ♟   ← Black pawns still at e7 (unmoved!)
  6  ·  ·  ·  ·  ·  ·  ·  ·
  5  ·  ·  ·  ·  ♙ ·  ·  ·   ← White pawn at e5 (moved twice!)
  4  ·  ·  ·  ·  ·  ·  ·  ·   ← No piece at e4 anymore
  3  ·  ·  ·  ·  ·  ·  ·  ·
  2  ♙ ♙ ♙ ♙ ·  ♙ ♙ ♙
  1  ♖ ♘ ♗ ♕ ♔ ♗ ♘ ♖
     a  b  c  d  e  f  g  h

❌ Problem: White moved TWICE (e2→e4→e5), Black never moved!
```

---

## The Fix

### Current Code (After Fix) - ✅ CORRECT

```csharp
public void MakeExampleMove()
{
    // Move 1: White pawn advances from e2 to e4
    // Board coordinates: row 6 (e2 in chess notation) to row 4 (e4)
    // This is White's first move in a standard chess opening
    if (moveCount == 0 && boardState.ContainsKey((6, 4)))
    {
        boardState[(4, 4)] = boardState[(6, 4)]; // Move piece
        boardState.Remove((6, 4)); // Remove from old position
        moveCount++;
        lastUpdated = DateTime.UtcNow;
    }
    // Move 2: Black pawn advances from e7 to e5    ← ✅ CORRECT COMMENT!
    // Board coordinates: row 1 (e7 in chess notation) to row 3 (e5)
    // This is Black's response, alternating turns as in a real chess game
    // Note: row 1 = e7 (black's starting pawn row), NOT row 4 (e4)
    else if (moveCount == 1 && boardState.ContainsKey((1, 4)))    ← ✅ CORRECT COORDINATE!
    {
        boardState[(3, 4)] = boardState[(1, 4)]; // Move piece
        boardState.Remove((1, 4)); // Remove from old position
        moveCount++;
        lastUpdated = DateTime.UtcNow;
    }
}
```

**What's correct:**
- ✅ Comment correctly says "black e7 pawn to e5"
- ✅ Code checks `(1, 4)` which is e7 (black's pawn)
- ✅ Properly alternates turns: White → Black (follows chess rules!)

### Visual Representation - CORRECT BEHAVIOR

```
After Move 1 (White e2→e4):
  8  ♜ ♞ ♝ ♛ ♚ ♝ ♞ ♜
  7  ♟ ♟ ♟ ♟ ♟ ♟ ♟ ♟   ← Black pawn at e7
  6  ·  ·  ·  ·  ·  ·  ·  ·
  5  ·  ·  ·  ·  ·  ·  ·  ·
  4  ·  ·  ·  ·  ♙ ·  ·  ·   ← White pawn at e4
  3  ·  ·  ·  ·  ·  ·  ·  ·
  2  ♙ ♙ ♙ ♙ ·  ♙ ♙ ♙
  1  ♖ ♘ ♗ ♕ ♔ ♗ ♘ ♖
     a  b  c  d  e  f  g  h

After Move 2 (CORRECTLY: Black e7→e5):
  8  ♜ ♞ ♝ ♛ ♚ ♝ ♞ ♜
  7  ♟ ♟ ♟ ♟ ·  ♟ ♟ ♟   ← Black pawn moved from e7
  6  ·  ·  ·  ·  ·  ·  ·  ·
  5  ·  ·  ·  ·  ♟ ·  ·  ·   ← Black pawn now at e5
  4  ·  ·  ·  ·  ♙ ·  ·  ·   ← White pawn still at e4
  3  ·  ·  ·  ·  ·  ·  ·  ·
  2  ♙ ♙ ♙ ♙ ·  ♙ ♙ ♙
  1  ♖ ♘ ♗ ♕ ♔ ♗ ♘ ♖
     a  b  c  d  e  f  g  h

✅ Correct: Alternating turns - White moved (e2→e4), then Black moved (e7→e5)
```

---

## Coordinate System Reference

```
Chess Board Layout:
┌─────────────────────────────────────┐
│  Row 0 = Rank 8  (Black back row)  │
│  Row 1 = Rank 7  (Black pawns)     │ ← e7 is at (1, 4)
│  Row 2 = Rank 6                     │
│  Row 3 = Rank 5                     │ ← e5 is at (3, 4)
│  Row 4 = Rank 4                     │ ← e4 is at (4, 4)
│  Row 5 = Rank 3                     │
│  Row 6 = Rank 2  (White pawns)     │ ← e2 is at (6, 4)
│  Row 7 = Rank 1  (White back row)  │
└─────────────────────────────────────┘
  Col:  0  1  2  3  4  5  6  7
File:  a  b  c  d  e  f  g  h
                      ↑
                  Column 4 = File e
```

### Key Coordinates

| Chess Notation | Coordinates | Piece (Initial) |
|---------------|-------------|-----------------|
| e2 | **(6, 4)** | ♙ White pawn |
| e4 | **(4, 4)** | · (target after move 1) |
| e7 | **(1, 4)** | ♟ Black pawn |
| e5 | **(3, 4)** | · (target after move 2) |

---

## Summary

- **Before**: Code and docs said "white e4→e5" using `(4, 4)` ❌
- **After**: Code and docs say "black e7→e5" using `(1, 4)` ✅
- **Result**: Proper turn alternation, correct chess behavior! ✅

This visual comparison clearly shows why the fix was necessary and what was changed.
