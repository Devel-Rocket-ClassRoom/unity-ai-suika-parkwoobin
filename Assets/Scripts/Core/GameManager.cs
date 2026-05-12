using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
        public event System.Action OnTop3Changed;

        // ── 상태 ────────────────────────────────────────────
        public int Score { get; private set; }
        public int BestScore { get; private set; }
        public bool IsGameOver { get; private set; }
        private List<int> top3Scores = new List<int>();

        const string BestScoreKey = "BestScore";
        const string Top3Key = "Top3Scores";

        // Top 3 스코어 조회 (읽기 전용)
        public List<int> GetTop3Scores() => new List<int>(top3Scores);

        // ── 초기화 ──────────────────────────────────────────
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadTop3Scores();
            // BestScore = Top3의 첫 번째 값 (없으면 0)
            BestScore = top3Scores.Count > 0 ? top3Scores[0] : 0;
        }

        void LoadTop3Scores()
        {
            top3Scores.Clear();
            string top3Json = PlayerPrefs.GetString(Top3Key, "");
            if (!string.IsNullOrEmpty(top3Json))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<IntListWrapper>(top3Json);
                    top3Scores = new List<int>(wrapper.scores);
                }
                catch
                {
                    top3Scores = new List<int>();
                }
            }
        }

        void SaveTop3Scores()
        {
            var wrapper = new IntListWrapper { scores = top3Scores.ToArray() };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(Top3Key, json);
            // BestScore 동기화
            BestScore = top3Scores.Count > 0 ? top3Scores[0] : 0;
            PlayerPrefs.Save();
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

            // Top 3 업데이트 (BestScore는 자동 동기화됨)
            UpdateTop3Scores(Score);

            OnGameOver?.Invoke(Score, BestScore);
        }

        void UpdateTop3Scores(int newScore)
        {
            top3Scores.Add(newScore);
            top3Scores.Sort((a, b) => b.CompareTo(a)); // 내림차순 정렬
            if (top3Scores.Count > 3)
                top3Scores.RemoveAt(top3Scores.Count - 1);
            SaveTop3Scores();
            OnTop3Changed?.Invoke();
        }

        // ── 재시작 ───────────────────────────────────────────
        public void RestartGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }

    // JSON 직렬화 헬퍼
    [System.Serializable]
    public class IntListWrapper
    {
        public int[] scores;
    }
}
