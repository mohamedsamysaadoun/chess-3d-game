// Reconstructed SaveSystem - handles save/load game state
// Original: SaveSystem (TypeDefIndex: 6189)

using UnityEngine;

namespace Chess3D.Core
{
    /// <summary>
    /// Handles saving and loading game state using PlayerPrefs.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        private const string SAVE_KEY = "chess3d_saved_game";
        private const string SETTINGS_KEY = "chess3d_settings";

        // Save game data
        [System.Serializable]
        public class GameState
        {
            public int gameMode;
            public int currentPlayer;
            public int difficulty;
            public int whiteScore;
            public int blackScore;
            public string boardFEN;
            public MoveRecord[] moveHistory;
            public int halfMoveClock;
            public int fullMoveNumber;
        }

        [System.Serializable]
        public class MoveRecord
        {
            public byte fromSquare;
            public byte toSquare;
            public byte promotionPiece;
            public byte moveFlags;
        }

        [System.Serializable]
        public class Settings
        {
            public float soundVolume = 1.0f;
            public float musicVolume = 0.7f;
            public bool vibrateOnMove = true;
            public bool showCoordinates = false;
            public int boardStyle = 0;
            public int pieceStyle = 0;
            public string playerName = "Player";
        }

        public void SaveCurrentGame(GameState state)
        {
            string json = JsonUtility.ToJson(state);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            Debug.Log("[SaveSystem] Game saved");
        }

        public GameState LoadLastSavedGame()
        {
            string json = PlayerPrefs.GetString(SAVE_KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[SaveSystem] No saved game found");
                return null;
            }
            return JsonUtility.FromJson<GameState>(json);
        }

        public bool SaveGameExists()
        {
            return PlayerPrefs.HasKey(SAVE_KEY);
        }

        public void DeleteSaveGame()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
        }

        public void SaveSettings(Settings settings)
        {
            string json = JsonUtility.ToJson(settings);
            PlayerPrefs.SetString(SETTINGS_KEY, json);
            PlayerPrefs.Save();
        }

        public Settings LoadSettings()
        {
            string json = PlayerPrefs.GetString(SETTINGS_KEY, "");
            if (string.IsNullOrEmpty(json))
                return new Settings();
            return JsonUtility.FromJson<Settings>(json);
        }
    }
}
