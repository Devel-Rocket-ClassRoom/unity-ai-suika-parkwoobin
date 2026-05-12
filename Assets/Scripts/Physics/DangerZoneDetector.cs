using System.Collections.Generic;
using UnityEngine;

namespace Suika
{
    /// <summary>
    /// 상단 위험선(트리거 영역) 위에 과일이 일정 시간 이상 머물면
    /// GameManager.TriggerGameOver() 를 호출한다.
    ///
    /// 설정 방법:
    ///   - 이 컴포넌트를 빈 GameObject 에 추가
    ///   - BoxCollider2D(IsTrigger=true) 를 같이 붙이고 위험선 영역에 배치
    ///   - Layer 에 "Fruit" 레이어를 만들고, 모든 과일 Prefab 에 적용
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DangerZoneDetector : MonoBehaviour
    {
        [Tooltip("이 시간(초) 동안 과일이 위험선 위에 머물면 게임오버")]
        public float gameOverDelay = 3f;

        // 현재 위험 영역 안에 있는 과일과 진입 시각
        readonly Dictionary<FruitBehaviour, float> _fruitsInZone = new();

        Collider2D _col;

        void Awake()
        {
            _col = GetComponent<Collider2D>();
            _col.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            FruitBehaviour fb = other.GetComponent<FruitBehaviour>();
            if (fb == null) return;
            if (!_fruitsInZone.ContainsKey(fb))
                _fruitsInZone[fb] = Time.time;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            FruitBehaviour fb = other.GetComponent<FruitBehaviour>();
            if (fb != null)
                _fruitsInZone.Remove(fb);
        }

        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

            // null(파괴된 과일) 제거
            List<FruitBehaviour> toRemove = new();
            foreach (var kv in _fruitsInZone)
            {
                if (kv.Key == null) { toRemove.Add(kv.Key); continue; }

                // 낙하 중 과일은 판정 유예
                if (kv.Key.IsFalling) { _fruitsInZone[kv.Key] = Time.time; continue; }

                if (Time.time - kv.Value >= gameOverDelay)
                {
                    GameManager.Instance.TriggerGameOver();
                    return;
                }
            }
            foreach (var k in toRemove) _fruitsInZone.Remove(k);
        }
    }
}
