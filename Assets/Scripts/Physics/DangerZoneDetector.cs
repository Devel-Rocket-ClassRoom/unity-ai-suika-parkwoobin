using System.Collections.Generic;
using UnityEngine;

namespace Suika
{
    [RequireComponent(typeof(Collider2D))]
    public class DangerZoneDetector : MonoBehaviour
    {
        [Header("게임오버 지연 시간")]
        [SerializeField] private float gameOverDelay = 1f;

        // 과일 → 위험 영역 판정 시작 시각
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
            {
                _fruitsInZone[fb] = Time.time;

                Debug.Log($"[DangerZone] Lv{fb.Data?.level} {fb.Data?.fruitName} 진입 " +
                          $"(HasTouched: {fb.HasTouched})");
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            FruitBehaviour fb = other.GetComponent<FruitBehaviour>();
            if (fb == null) return;

            _fruitsInZone.Remove(fb);

            Debug.Log($"[DangerZone] Lv{fb.Data?.level} {fb.Data?.fruitName} 탈출");
        }

        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

            List<FruitBehaviour> toRemove = new();
            List<FruitBehaviour> toReset = new();

            foreach (var kv in _fruitsInZone)
            {
                FruitBehaviour fb = kv.Key;

                if (fb == null)
                {
                    toRemove.Add(fb);
                    continue;
                }

                // 아직 바닥/과일/벽 등에 닿은 적 없는 과일은 판정 제외
                // 생성 위치에 걸쳐 있는 과일은 여기서 계속 타이머가 리셋됨
                if (!fb.HasTouched)
                {
                    toReset.Add(fb);
                    continue;
                }

                float elapsed = Time.time - kv.Value;

                // 핵심: 1초 이상 DangerZone 안에 계속 있을 때만 게임오버
                if (elapsed >= gameOverDelay)
                {
                    Debug.Log($"[DangerZone] Lv{fb.Data?.level} {fb.Data?.fruitName} " +
                              $"{elapsed:F2}초 동안 위험 영역에 머물러 게임오버");

                    GameManager.Instance.TriggerGameOver();
                    return;
                }
            }

            foreach (var k in toRemove)
            {
                _fruitsInZone.Remove(k);
            }

            foreach (var k in toReset)
            {
                if (k != null)
                    _fruitsInZone[k] = Time.time;
            }
        }
    }
}