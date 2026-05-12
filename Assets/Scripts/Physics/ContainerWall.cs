using UnityEngine;

namespace Suika
{
    [ExecuteAlways]
    public class ContainerWall : MonoBehaviour
    {
        [Header("컨테이너 크기")]
        public float halfWidth = 2.5f;
        public float height    = 6f;
        public float bottomY   = -4f;

        [Header("물리 여백")]
        [Tooltip("스프라이트 테두리 두께만큼 물리 벽을 안쪽으로 들여씀")]
        public float physicsInset = 0.15f;

        [Header("물리 재질")]
        public PhysicsMaterial2D wallMaterial;

        [Header("시각화 — U자형 이미지 연결")]
        [Tooltip("U자형 컨테이너 스프라이트를 여기에 드래그")]
        public Sprite containerSprite;
        public Color  containerColor = Color.white;
        public int    sortingOrder   = 1;

        void Awake() => Rebuild();

        void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) Rebuild();
            };
#endif
        }

        void Rebuild()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject c = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }

            BuildPhysics();
            BuildVisual();
        }

        void BuildPhysics()
        {
            if (!Application.isPlaying) return;

            // U자형 단일 EdgeCollider2D — physicsInset 만큼 안쪽에 배치
            float pw = halfWidth - physicsInset;
            float pb = bottomY   + physicsInset;

            GameObject go = new GameObject("Wall_U");
            go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            go.transform.SetParent(transform, false);

            EdgeCollider2D edge = go.AddComponent<EdgeCollider2D>();
            edge.points = new[]
            {
                new Vector2(-pw, bottomY + height),   // 왼쪽 상단
                new Vector2(-pw, pb),                 // 왼쪽 하단
                new Vector2( pw, pb),                 // 오른쪽 하단
                new Vector2( pw, bottomY + height),   // 오른쪽 상단
            };
            if (wallMaterial != null)
                edge.sharedMaterial = wallMaterial;
        }

        void BuildVisual()
        {
            GameObject go = new GameObject("Container_Visual");
            go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, bottomY + height * 0.5f, 0f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = containerSprite;
            sr.color        = containerColor;
            sr.sortingOrder = sortingOrder;

            if (containerSprite != null)
            {
                Vector2 s = containerSprite.bounds.size;
                go.transform.localScale = new Vector3(
                    halfWidth * 2f / s.x,
                    height       / s.y,
                    1f);
            }
            else
            {
                go.transform.localScale = new Vector3(halfWidth * 2f, height, 1f);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.78f, 0.66f, 0.42f, 0.6f);
            float l = -halfWidth, r = halfWidth, b = bottomY, t = bottomY + height;
            Gizmos.DrawLine(new Vector3(l, b), new Vector3(r, b));
            Gizmos.DrawLine(new Vector3(l, b), new Vector3(l, t));
            Gizmos.DrawLine(new Vector3(r, b), new Vector3(r, t));
        }
#endif
    }
}
