# Modding Guide

This document describes how to modify the reconstructed chess game.

## Ad Removal (Already Done)

All ad-related functions in `AdMobScript.cs` are already stubbed:
- `AdMobInit()` — Returns immediately
- `AMReqInters()` — Returns immediately
- `AMOnGameCompleteEv()` — Returns immediately
- `AdMobOnGameStart()` — Returns immediately

## Modifying AI Difficulty

Edit `Assets/Scripts/Game/ChessEngine.cs`:

```csharp
// In GetBestMove method:
public string GetBestMove(string fen, string lastMove, int depth, bool exactMove)
{
    // Change the depth based on difficulty
    switch (GameManager.Instance.CurrentDifficulty)
    {
        case MainScript.Difficulty.Easy:    depth = 1; break;
        case MainScript.Difficulty.Medium:  depth = 3; break;
        case MainScript.Difficulty.Hard:    depth = 5; break;
        case MainScript.Difficulty.Expert:  depth = 8; break;
    }
    // ... rest of method
}
```

## Adding Custom Pieces

1. Add new mesh to `Assets/Models/Pieces/`
2. Add new texture to `Assets/Textures/Pieces/`
3. Create new material in `Assets/Materials/Pieces/`
4. Update `ChessEngine.cs` to handle the new piece type

## Changing Board Skins

The game has 3 board styles (Wood sets 1, 2, 3):
- `Assets/Textures/Board/boardBorder1*.png`
- `Assets/Textures/Board/boardBorder2*.png`
- `Assets/Textures/Board/boardBorder3*.png`

To add a new skin, add new textures following the same naming pattern.

## Adding New UI Panels

1. Add new enum value to `MainScript.GamePanel`:
   ```csharp
   public enum GamePanel
   {
       // ... existing values ...
       MyNewPanel = 23
   }
   ```
2. Create a new Canvas in the scene
3. Add panel transition logic to `MainScript`

## Network Features (Future)

The `Network/` folder is a placeholder for:
- Online multiplayer
- Cloud save sync
- Online leaderboards (replacing the vulnerable HTTP endpoint)

## Cheat Codes (For Testing)

Add these to `MainScript.Update()`:

```csharp
if (Input.GetKeyDown(KeyCode.F1))
{
    // Force win
    GameManager.Instance.WhiteScore = 9999;
}
if (Input.GetKeyDown(KeyCode.F2))
{
    // Show AI's thinking
    Debug.Log(ChessEngine.principalVariation);
}
```
