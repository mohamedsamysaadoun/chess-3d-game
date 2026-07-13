# Real Chess 3D - Reconstructed Unity Project

This is a reconstructed Unity project for the Real Chess 3D mobile game, based on complete reverse engineering analysis.

## 📋 Project Status

- ✅ **Assets extracted** — 294 textures, 49 meshes, 95 materials
- ✅ **Chess engine decoded** — All class/method names translated from Marathi/Hindi obfuscation
- ✅ **C# scripts scaffolded** — Core game logic structure in place
- ✅ **Ad removal patches** — All ad functions stubbed out
- 🔄 **Method body recovery** — In progress (using Ghidra decompilation)

## 🎯 What's Included

### Scripts (C#)

```
Assets/Scripts/
├── Core/
│   ├── MainScript.cs       # Main game controller (Parda panels, game state)
│   ├── GameManager.cs      # Singleton game manager
│   └── SaveSystem.cs       # Save/load game state
├── Game/
│   ├── ChessEngine.cs      # Complete chess engine (alpha-beta + PST)
│   ├── Move.cs             # Move struct + ScoredMove + HistoryMove
│   └── OpeningBook.cs      # Opening book (first 5 moves)
├── UI/
│   └── AdMobScript.cs      # Ad management (PATCHED - ads removed)
├── AI/                     # (placeholder for AI enhancements)
├── Network/                # (placeholder for online features)
├── Input/                  # (placeholder for input handlers)
└── Utils/                  # (placeholder for utilities)
```

### Assets

```
Assets/
├── Textures/
│   ├── Pieces/     (101 chess piece textures - 3 sets: Wood)
│   ├── Board/      (28 board and border textures)
│   ├── UI/         (421 UI textures)
│   ├── Sprites/    (90 sprites)
│   └── Skybox/     (2 cubemaps)
├── Models/
│   ├── Pieces/     (12 chess piece meshes - pawn, rook, knight, bishop, queen, king)
│   ├── Board/      (26 board and border meshes)
│   └── Props/      (60 other meshes)
├── Materials/      (100 material definitions as JSON)
├── Audio/SFX/      (sound effects)
└── Shaders/        (Unity shaders)
```

### Reference Materials

The `reference/` directory contains the reverse engineering artifacts:
- `obfuscation_map_full.json` — 91 Marathi/Hindi → English name mappings
- `ghidra-decompiled/` — 30 decompiled C pseudocode files for key functions
- `scene-data/` — Complete scene hierarchy (1,243 GameObjects)
- `stringliteral.json` — All 12,004 string literals from the game

## 🔧 Chess Engine Architecture

The chess engine (`ChessEngine.cs`) is a classic mailbox-based implementation:

- **Board representation**: 10x12 mailbox (120 squares with off-board padding)
- **Search algorithm**: Alpha-beta pruning with quiescence search
- **Move ordering**: History heuristic + PV sorting + QuickSort
- **Evaluation**: Material + Piece-Square Tables + King safety + Pawn structure
- **Max search depth**: 10 plies
- **Opening book**: Separate class with pre-computed moves

### Decoded Class Names (originally Marathi/Hindi)

| Original | Decoded | Purpose |
|---|---|---|
| `SechDMG` | `ChessEngine` | Main chess engine |
| `KhaataKhol` | `OpeningBook` | Opening moves |
| `SechChal` | `Move` | Move struct |
| `KeemtiChal` | `ScoredMove` | Move with score |
| `HistoryMove` | `HistoryMove` | Undo information |
| `PALI` | `Player` | White/Black |
| `Parda` | `GamePanel` | UI panel identifier |
| `DMG_MUSKL` | `Difficulty` | Easy/Medium/Hard/Expert |

### Decoded Method Names

| Original | Decoded | Purpose |
|---|---|---|
| `EkChalBatao` | `GetBestMove` | Public API |
| `DhoondoNormal` | `AlphaBeta` | Main search |
| `DhoondoDhainya` | `QuiescenceSearch` | Quiescence search |
| `SochnaSuruKro` | `StartSearch` | Search initialization |
| `SamjoPHAN` | `ParseFEN` | FEN parsing |
| `PichheLe` | `UnmakeMove` | Undo move |
| `SuruBatao` | `GetOpeningMove` | Opening book lookup |

## 🛠️ Ad Removal Patches

All ad-related functions are stubbed to immediately return (no-op):

| Function | RVA | Original Behavior | Patched |
|---|---|---|---|
| `AdMobInit` | 0xEC9BCC | Initialize AdMob SDK | Returns immediately |
| `AMReqInters` | 0xEC9C54 | Request interstitial ad | Returns immediately |
| `AMOnGameCompleteEv` | 0xEC15CC | Show ad on game complete | Returns immediately |
| `AdMobOnGameStart` | 0xEB6CB0 | Show ad on game start | Returns immediately |

## 🚀 Getting Started

### Prerequisites

- Unity Hub
- Unity 2022.3 LTS or Unity 6.0 (to match original)
- Android Build Support module

### Opening the Project

1. Open Unity Hub
2. Click "Add project from disk"
3. Select this folder
4. Wait for Unity to import all assets
5. Open the Main scene (when available)

### Building for Android

1. File → Build Settings
2. Switch Platform to Android
3. Set Bundle Identifier to `com.yourname.chess3d`
4. Set Min API Level to 22 (Android 5.1)
5. Build and Run

## 📚 Related Repository

The complete reverse engineering analysis is in:
https://github.com/mohamedsamysaadoun/chess-3d-re

That repository contains:
- Full IL2CPP dump (40,000+ methods)
- Ghidra decompilation output
- Obfuscation analysis
- Binary patches
- Frida hooks

## ⚖️ Legal Notice

This is a reverse engineering project for educational purposes. All trademarks belong to their respective owners. The reconstructed code is based on analysis of the original game's binary, not the original source code.

## 📄 License

MIT License - see LICENSE file for details
