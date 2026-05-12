using TMPro;
using UnityEngine;

namespace Suika
{
    /// <summary>
    /// 현재 점수와 베스트 스코어를 실시간으로 표시한다.
    /// TextMeshPro 텍스트 컴포넌트 2개를 Inspector 에서 연결.
    /// </summary>
    public class ScoreUI : MonoBehaviour
    {
        [Header("텍스트 참조")]
        public TMP_Text scoreText;
        public TMP_Text bestScoreText;

        void Start()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnScoreChanged += UpdateScore;
            UpdateScore(GameManager.Instance.Score);
            UpdateBest(GameManager.Instance.BestScore);
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnScoreChanged -= UpdateScore;
        }

        void UpdateScore(int score)
        {
            if (scoreText != null)
                scoreText.text = $"{score:D4}";
            UpdateBest(GameManager.Instance.BestScore);
        }

        void UpdateBest(int best)
        {
            if (bestScoreText != null)
                bestScoreText.text = $"{best:D4}";
        }
    }
}
