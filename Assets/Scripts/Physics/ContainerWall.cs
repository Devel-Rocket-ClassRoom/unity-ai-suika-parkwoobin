using UnityEngine;

namespace Suika
{
    /// <summary>
    /// 수박 게임 컨테이너(상자)의 바닥과 좌우 벽을 런타임에 생성한다.
    /// 이 컴포넌트를 빈 GameObject "Container" 에 붙이면
    /// Awake 에서 EdgeCollider2D 3개(바닥·왼쪽·오른쪽)가 자동 생성된다.
    ///
    ///   halfWidth  : 컨테이너 내부 절반 너비 (기본 2.5)
    ///   height     : 컨테이너 높이 (기본 5.0)
    ///   bottomY    : 바닥 Y 좌표 (기본 -4.0)
    /// </summary>
    public class ContainerWall : MonoBehaviour
    {
        [Header("컨테이너 크기")]
        public float halfWidth = 2.5f;
        public float height    = 6f;
        public float bottomY   = -4f;

        [Header("물리 재질")]
        [Tooltip("벽 마찰·탄성 설정 (없으면 기본값 사용)")]
        public PhysicsMaterial2D wallMaterial;

        void Awake()
        {
            BuildWalls();
        }

        void BuildWalls()
        {
            // ── 바닥 ─────────────────────────────────────────────
            CreateEdge("Wall_Bottom",
                new Vector2(-halfWidth, bottomY),
                new Vector2( halfWidth, bottomY));

            // ── 왼쪽 벽 ─────────────────────────────────────────
            CreateEdge("Wall_Left",
                new Vector2(-halfWidth, bottomY),
                new Vector2(-halfWidth, bottomY + height));

            // ── 오른쪽 벽 ────────────────────────────────────────
            CreateEdge("Wall_Right",
                new Vector2( halfWidth, bottomY),
                new Vector2( halfWidth, bottomY + height));
        }

        void CreateEdge(string objName, Vector2 start, Vector2 end)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(transform, false);

            EdgeCollider2D edge = go.AddComponent<EdgeCollider2D>();
            edge.points = new[] { start, end };

            if (wallMaterial != null)
                edge.sharedMaterial = wallMaterial;
        }

        // ── 에디터용 기즈모 (씬 뷰에서 컨테이너 시각화) ──────────
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.78f, 0.66f, 0.42f, 0.8f); // 나무 갈색 (#C8A870)

            float l = -halfWidth;
            float r =  halfWidth;
            float b =  bottomY;
            float t =  bottomY + height;

            // 바닥
            Gizmos.DrawLine(new Vector3(l, b), new Vector3(r, b));
            // 왼쪽
            Gizmos.DrawLine(new Vector3(l, b), new Vector3(l, t));
            // 오른쪽
            Gizmos.DrawLine(new Vector3(r, b), new Vector3(r, t));

            // 위험선 (빨간 점선 느낌 — 실선으로 표시)
            float dangerY = bottomY + height - 1f;
            Gizmos.color = new Color(1f, 0.42f, 0.42f, 0.9f); // #FF6B6B
            Gizmos.DrawLine(new Vector3(l, dangerY), new Vector3(r, dangerY));
        }
#endif
    }
}
