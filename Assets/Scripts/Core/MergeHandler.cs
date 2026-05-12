using System.Collections;
using UnityEngine;

namespace Suika
{
    /// <summary>
    /// 씬에 하나만 존재하는 싱글턴.
    /// FruitBehaviour 로부터 머지 요청을 받아
    ///   1) 두 과일 삭제
    ///   2) 다음 레벨 과일 생성
    ///   3) GameManager 에 점수 전달
    /// 을 수행한다.
    /// </summary>
    public class MergeHandler : MonoBehaviour
    {
        public static MergeHandler Instance { get; private set; }

        [Header("참조")]
        [Tooltip("FruitDatabase ScriptableObject")]
        public FruitDatabase fruitDatabase;

        [Tooltip("과일 Prefab 배열 — FruitDatabase.fruits 와 동일한 순서(Lv1~11)")]
        public GameObject[] fruitPrefabs;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>FruitBehaviour 가 충돌 시 호출.</summary>
        public void RequestMerge(FruitBehaviour a, FruitBehaviour b, Vector2 position)
        {
            StartCoroutine(DoMerge(a, b, position));
        }

        IEnumerator DoMerge(FruitBehaviour a, FruitBehaviour b, Vector2 position)
        {
            // 한 프레임 뒤에 실행 — 물리 콜백 중 Destroy 방지
            yield return null;

            if (a == null || b == null) yield break;

            int currentLevel = a.Data.level;
            int nextLevel = currentLevel + 1;

            // 점수 획득
            int score = fruitDatabase.GetByLevel(nextLevel) != null
                ? fruitDatabase.GetByLevel(nextLevel).mergeScore
                : a.Data.mergeScore; // fallback
            GameManager.Instance.AddScore(score);

            // 두 과일 제거
            Destroy(a.gameObject);
            Destroy(b.gameObject);

            // Lv11 수박은 더 이상 머지 불가 → 생성만 하고 종료
            if (nextLevel > 11) yield break;

            // 다음 레벨 과일 생성
            int prefabIdx = nextLevel - 1;
            if (prefabIdx >= fruitPrefabs.Length) yield break;

            GameObject newFruitObj = Instantiate(fruitPrefabs[prefabIdx], position, Quaternion.identity);
            FruitBehaviour newFruit = newFruitObj.GetComponent<FruitBehaviour>();
            if (newFruit != null)
            {
                FruitData nextData = fruitDatabase.GetByLevel(nextLevel);
                newFruit.Initialize(nextData);
                // 머지로 생성된 과일은 즉시 물리 적용(낙하 상태 아님)
                newFruit.SetKinematic(false);
            }
        }
    }
}
