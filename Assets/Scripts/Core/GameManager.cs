// Reconstructed GameManager - manages overall game state
using UnityEngine;

namespace Chess3D.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public MainScript.GameMode CurrentGameMode { get; set; }
        public MainScript.Player CurrentPlayer { get; set; }
        public MainScript.Difficulty CurrentDifficulty { get; set; }

        public int WhiteScore { get; set; }
        public int BlackScore { get; set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
