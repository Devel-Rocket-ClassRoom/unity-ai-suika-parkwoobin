using TMPro;
using UnityEngine;

namespace Suika
{
    /// <summary>
    /// Rank 이미지 하위(Vertical Layout)에 Best Score Top 3을 표시하고,
    /// 그 아래에 현재 점수를 표시한다.
    /// 
    /// Canvas > Rank 이미지 > Vertical Layout 하위에 다음을 배치:
    /// - FirstScoreText (TMP_Text) — 1등
    /// - SecondScoreText (TMP_Text) — 2등
    /// - ThirdScoreText (TMP_Text) — 3등
    /// - CurrentScoreText (TMP_Text) — 현재 점수
    /// </summary>
    public class RankUI : MonoBehaviour
    {
        [Header("베스트 스코어 텍스트")]
        public TMP_Text firstScoreText;
        public TMP_Text secondScoreText;
        public TMP_Text thirdScoreText;

        [Header("현재 점수")]
        public TMP_Text currentScoreText;

        void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[RankUI] GameManager.Instance가 null입니다!");
                return;
            }

            // 초기 업데이트
            UpdateRank();
            UpdateCurrentScore(GameManager.Instance.Score);

            // 리스너 등록
            GameManager.Instance.OnScoreChanged += UpdateCurrentScore;
            GameManager.Instance.OnTop3Changed += UpdateRank;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged -= UpdateCurrentScore;
                GameManager.Instance.OnTop3Changed -= UpdateRank;
            }
        }

        void UpdateRank()
        {
            if (GameManager.Instance == null) return;

            var top3 = GameManager.Instance.GetTop3Scores();

            if (firstScoreText != null)
                firstScoreText.text = top3.Count > 0 ? $"1. {top3[0]:D4}" : "0000";

            if (secondScoreText != null)
                secondScoreText.text = top3.Count > 1 ? $"2. {top3[1]:D4}" : "0000";

            if (thirdScoreText != null)
                thirdScoreText.text = top3.Count > 2 ? $"3. {top3[2]:D4}" : "0000";
        }

        void UpdateCurrentScore(int score)
        {
            if (currentScoreText != null)
                currentScoreText.text = $"{score:D4}";
        }
    }
}
