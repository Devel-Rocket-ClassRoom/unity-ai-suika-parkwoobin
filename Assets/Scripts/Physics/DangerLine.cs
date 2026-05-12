using UnityEngine;

namespace Suika
{
    [ExecuteAlways]
    public class DangerLine : MonoBehaviour
    {
        [Header("점선 설정")]
        public float totalWidth  = 5f;
        public float dashLength  = 0.18f;
        public float gapLength   = 0.12f;
        public float lineHeight  = 0.05f;
        public Color dashColor   = new Color(1f, 0.42f, 0.42f, 0.9f);
        public int   sortingOrder = 50;

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

        public void Rebuild()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject c = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }

            Sprite sp   = GetWhiteSprite();
            float  step = dashLength + gapLength;
            int    n    = Mathf.Max(1, Mathf.FloorToInt(totalWidth / step));

            // 전체 대시들을 중앙 정렬
            float used    = n * dashLength + (n - 1) * gapLength;
            float startX  = -used * 0.5f + dashLength * 0.5f;

            for (int i = 0; i < n; i++)
            {
                GameObject go = new GameObject($"Dash_{i}");
                go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(startX + i * step, 0f, 0f);
                go.transform.localScale    = new Vector3(dashLength, lineHeight, 1f);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = sp;
                sr.color        = dashColor;
                sr.sortingOrder = sortingOrder;
            }
        }

        static Sprite _whiteSprite;
        static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _whiteSprite;
        }
    }
}
