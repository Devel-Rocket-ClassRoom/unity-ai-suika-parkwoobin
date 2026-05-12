using UnityEngine;

namespace Suika
{
    /// <summary>
    /// 게임 전체 상태(점수, 게임오버)를 관리하는 싱글턴.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ── 이벤트 ──────────────────────────────────────────
        public event System.Action<int> OnScoreChanged;
        public event System.Action<int, int> OnGameOver; // (finalScore, bestScore)

        // ── 상태 ────────────────────────────────────────────
        public int Score { get; private set; }
        public int BestScore { get; private set; }
        public bool IsGameOver { get; private set; }

        const string BestScoreKey = "BestScore";

        // ── 초기화 ──────────────────────────────────────────
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        }

        // ── 점수 ────────────────────────────────────────────
        public void AddScore(int amount)
        {
            if (IsGameOver) return;
            Score += amount;
            OnScoreChanged?.Invoke(Score);
        }

        // ── 게임오버 ─────────────────────────────────────────
        public void TriggerGameOver()
        {
            if (IsGameOver) return;
            IsGameOver = true;

            if (Score > BestScore)
            {
                BestScore = Score;
                PlayerPrefs.SetInt(BestScoreKey, BestScore);
                PlayerPrefs.Save();
            }

            OnGameOver?.Invoke(Score, BestScore);
        }

        // ── 재시작 ───────────────────────────────────────────
        public void RestartGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
