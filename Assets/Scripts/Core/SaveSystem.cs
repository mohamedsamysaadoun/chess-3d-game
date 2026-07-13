// Reconstructed SaveSystem - handles save/load game state
using UnityEngine;

namespace Chess3D.Core
{
    public class SaveSystem : MonoBehaviour
    {
        private const string SAVE_KEY = "saved_game";

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
    }

    [System.Serializable]
    public class GameState
    {
        public MainScript.GameMode gameMode;
        public MainScript.Player currentPlayer;
        public int whiteScore;
        public int blackScore;
        public string boardFen;
        public MoveRecord[] moveHistory;
    }

    [System.Serializable]
    public class MoveRecord
    {
        public byte fromSquare;
        public byte toSquare;
        public byte promotionPiece;
        public byte moveFlags;
    }
}
