// Reconstructed HighScoresScript - handles high score submission and retrieval
// Original: HighScoresScript (TypeDefIndex: 6214)

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Chess3D.Network
{
    /// <summary>
    /// Manages high score submission and retrieval from the server.
    /// NOTE: The original game uses an unauthenticated HTTP endpoint.
    /// </summary>
    public class HighScoresScript : MonoBehaviour
    {
        // Server endpoints (from decompiled code)
        private const string PHP_SUBMIT_URL = "https://www.eivaagames.com/games/highscores/submit-score.php";
        private string phpGetScoresUrl;

        // Score data
        private string[,] egHighScoresArray;
        private string[] egHighScoresTempArray;

        /// <summary>
        /// Submit a score to the server.
        /// WARNING: The original endpoint has no authentication - scores can be forged!
        /// </summary>
        public IEnumerator SubmitScore(string userId, int score, string gameMode)
        {
            WWWForm form = new WWWForm();
            form.AddField("user_id", userId);
            form.AddField("score", score);
            form.AddField("game_mode", gameMode);

            using (UnityWebRequest request = UnityWebRequest.Post(PHP_SUBMIT_URL, form))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[HighScores] Score submitted: {score}");
                }
                else
                {
                    Debug.LogError($"[HighScores] Submit failed: {request.error}");
                }
            }
        }

        /// <summary>
        /// Get high scores from the server.
        /// </summary>
        public IEnumerator GetHighScores(string gameMode)
        {
            string url = phpGetScoresUrl + "?mode=" + gameMode;

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    ParseScores(request.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"[HighScores] Get failed: {request.error}");
                }
            }
        }

        private void ParseScores(string response)
        {
            // Parse the server response
            // Format: "rank,name,score\nrank,name,score\n..."
            string[] lines = response.Split('\n');
            egHighScoresArray = new string[lines.Length, 3];
            egHighScoresTempArray = new string[lines.Length];

            for (int i = 0; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(',');
                if (parts.Length >= 3)
                {
                    egHighScoresArray[i, 0] = parts[0];  // rank
                    egHighScoresArray[i, 1] = parts[1];  // name
                    egHighScoresArray[i, 2] = parts[2];  // score
                }
            }
        }

        public string GetScoreEntry(int rank)
        {
            if (egHighScoresArray == null || rank >= egHighScoresArray.GetLength(0))
                return "";
            return $"{egHighScoresArray[rank, 0]}. {egHighScoresArray[rank, 1]} - {egHighScoresArray[rank, 2]}";
        }

        public int GetScoreCount()
        {
            return egHighScoresArray?.GetLength(0) ?? 0;
        }
    }
}
