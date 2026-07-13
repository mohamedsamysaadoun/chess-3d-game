# Architecture Overview

## Game Structure

The Real Chess 3D game follows a standard Unity mobile game architecture:

```
┌─────────────────────────────────────────┐
│           UI Layer (Canvas)             │
├─────────────────────────────────────────┤
│         MainScript (Controller)         │
├─────────────────────────────────────────┤
│  ┌─────────────┐  ┌──────────────────┐  │
│  │ GameManager │  │   SaveSystem     │  │
│  │  (State)    │  │   (Persistence)  │  │
│  └─────────────┘  └──────────────────┘  │
├─────────────────────────────────────────┤
│           ChessEngine                   │
│  ┌──────────────────────────────────┐   │
│  │  Move Generation + Validation    │   │
│  ├──────────────────────────────────┤   │
│  │  Alpha-Beta Search               │   │
│  ├──────────────────────────────────┤   │
│  │  Evaluation (PST + Material)     │   │
│  ├──────────────────────────────────┤   │
│  │  Opening Book                    │   │
│  └──────────────────────────────────┘   │
├─────────────────────────────────────────┤
│      Rendering (3D Pieces + Board)      │
└─────────────────────────────────────────┘
```

## Class Hierarchy

### Core
- `MainScript` — Main game controller, manages UI panels
- `GameManager` — Singleton, holds game state
- `SaveSystem` — Save/load game state to PlayerPrefs

### Game (Chess Logic)
- `ChessEngine` — Complete chess engine with search and evaluation
- `Move` — Struct representing a chess move
- `ScoredMove` — Move with evaluation score
- `HistoryMove` — Move with undo information
- `OpeningBook` — Pre-computed opening moves

### UI
- `AdMobScript` — Ad management (PATCHED to remove ads)

## Game Flow

1. **App Start**: `MainScript.Start()` → `FindUIObjects()` → `SetupUIFunctions()`
2. **Main Menu**: User selects game mode and difficulty
3. **Game Start**: `AdMobScript.AdMobOnGameStart()` (PATCHED - no ad)
4. **Player Move**: User clicks piece → valid moves highlighted → click destination
5. **AI Move**: `ChessEngine.GetBestMove(fen, lastMove, depth, exactMove)`
   - Try opening book first (first 5 moves)
   - Fall back to alpha-beta search with quiescence
6. **Game End**: Checkmate/stalemate detected → `AdMobScript.AMOnGameCompleteEv()` (PATCHED)

## Data Flow

```
User Input → MainScript → ChessEngine.MakeMove()
                            ↓
                        ChessEngine.GenerateMoves()
                            ↓
                        ChessEngine.AlphaBeta() [recursive]
                            ↓
                        ChessEngine.Evaluate() [leaf nodes]
                            ↓
                        Returns best move
                            ↓
                        MainScript updates UI
```

## Persistence

- Save game: `PlayerPrefs.SetString("saved_game", json)`
- Load game: `PlayerPrefs.GetString("saved_game")`
- Save data: board FEN, current player, scores, move history

## Obfuscation Notes

The original game uses Marathi/Hindi word substitution for obfuscation. All decoded names are in `reference/obfuscation_map_full.json`.

Example translations:
- `SechDMG` → `ChessEngine` (Sech=search, DMG=engine)
- `KhaataKhol` → `OpeningBook` (Khaata=account, Khol=open)
- `SechChal` → `Move` (Chal=move)
- `DhoondoNormal` → `AlphaBeta` (Dhoondo=search)
