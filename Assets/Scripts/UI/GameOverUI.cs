using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Suika
{
    /// <summary>
    /// 게임 오버 시 오버레이 패널을 표시한다.
    /// 평소에는 gameOverPanel 을 비활성화해 둔다.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("패널")]
        public GameObject gameOverPanel;

        [Header("텍스트")]
        public TMP_Text finalScoreText;
        public TMP_Text bestScoreText;

        [Header("버튼")]
        public Button retryButton;

        GameManager cachedGameManager;

        void Awake()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            cachedGameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
            if (cachedGameManager != null)
                cachedGameManager.OnGameOver += ShowGameOver;

            if (retryButton != null)
                retryButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());

            if (gameOverPanel == null || finalScoreText == null || bestScoreText == null || retryButton == null)
            {
                Debug.LogError($"[GameOverUI] Inspector 참조가 누락되었습니다. panel={gameOverPanel}, finalScore={finalScoreText}, bestScore={bestScoreText}, retryButton={retryButton}", this);
            }
        }

        void OnDestroy()
        {
            if (cachedGameManager != null)
                cachedGameManager.OnGameOver -= ShowGameOver;

            if (retryButton != null)
                retryButton.onClick.RemoveAllListeners();
        }

        void ShowGameOver(int finalScore, int bestScore)
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (finalScoreText != null) finalScoreText.text = $"SCORE  {finalScore}";
            if (bestScoreText != null) bestScoreText.text = $"BEST   {bestScore}";
        }
    }
}
