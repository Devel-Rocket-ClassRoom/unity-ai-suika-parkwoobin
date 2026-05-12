using UnityEngine;

namespace Suika
{
    /// <summary>
    /// 각 과일 GameObject 에 붙는 핵심 컴포넌트.
    /// Rigidbody2D + CircleCollider2D 와 함께 동작하며
    /// MergeHandler 에 충돌 이벤트를 전달한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class FruitBehaviour : MonoBehaviour
    {
        // ── 공개 상태 ──────────────────────────────────────
        public FruitData Data { get; private set; }

        /// <summary>낙하 중(아직 착지 전)이면 true — 이 상태에서는 머지 무시.</summary>
        public bool IsFalling { get; private set; } = true;

        // ── 내부 참조 ──────────────────────────────────────
        Rigidbody2D _rb;
        CircleCollider2D _col;
        SpriteRenderer _sr;

        // 머지 중복 방지 플래그
        bool _merging;

        // 착지 판정용 (속도가 충분히 느려지면 착지 완료)
        const float LandedSpeedThreshold = 0.1f;
        const float LandedCheckDelay = 0.3f;   // 생성 직후 판정 유예 시간
        float _spawnTime;

        // ── 초기화 ─────────────────────────────────────────
        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CircleCollider2D>();
            _sr = GetComponent<SpriteRenderer>();
        }

        /// <summary>FruitSpawner 가 생성 후 반드시 호출.</summary>
        public void Initialize(FruitData data)
        {
            Data = data;

            // 스프라이트 적용 — 없으면 레벨에 맞는 색상 원형으로 대체
            if (_sr != null)
                _sr.sprite = data.sprite != null ? data.sprite : MakePlaceholderSprite(data.level);

            // 크기 적용
            // 스프라이트는 PPU=64, 64x64px → 1 Unity unit 크기
            // diameter = radius * 2 (GDD 상대값 그대로 Unity unit 사용)
            float diameter = data.radius * 2f;
            transform.localScale = new Vector3(diameter, diameter, 1f);
            _col.radius = 0.5f; // 로컬 스페이스 0.5 → 월드 스페이스 radius

            // 물리 설정
            _rb.gravityScale = 1f;
            _rb.linearDamping = 0.3f;
            _rb.angularDamping = 0.5f;

            PhysicsMaterial2D mat = new PhysicsMaterial2D("FruitMat")
            {
                bounciness = 0.1f,
                friction = 0.6f,
            };
            _col.sharedMaterial = mat;

            _spawnTime = Time.time;
        }

        // ── 매 프레임 ──────────────────────────────────────
        void Update()
        {
            // 생성 후 일정 시간 지나고, 속도가 느려지면 착지 처리
            if (IsFalling
                && Time.time - _spawnTime > LandedCheckDelay
                && _rb.linearVelocity.magnitude < LandedSpeedThreshold)
            {
                IsFalling = false;
            }
        }

        // ── 충돌 처리 ──────────────────────────────────────
        void OnCollisionEnter2D(Collision2D col)
        {
            if (_merging) return;

            FruitBehaviour other = col.gameObject.GetComponent<FruitBehaviour>();
            if (other == null) return;
            if (other._merging) return;

            // 같은 레벨, 둘 다 착지 완료 상태일 때만 머지
            if (Data.level == other.Data.level && !IsFalling && !other.IsFalling)
            {
                _merging = true;
                other._merging = true;

                Vector2 midPoint = (transform.position + other.transform.position) * 0.5f;
                MergeHandler.Instance.RequestMerge(this, other, midPoint);
            }
        }

        // ── 임시 스프라이트 생성 ────────────────────────────
        static readonly Color[] LevelColors =
        {
            new Color(0.90f, 0.10f, 0.10f), // Lv1  Cherry
            new Color(0.95f, 0.30f, 0.35f), // Lv2  Strawberry
            new Color(0.55f, 0.20f, 0.70f), // Lv3  Grape
            new Color(1.00f, 0.60f, 0.10f), // Lv4  Dekopon
            new Color(0.95f, 0.45f, 0.10f), // Lv5  Persimmon
            new Color(0.85f, 0.15f, 0.15f), // Lv6  Apple
            new Color(0.80f, 0.85f, 0.20f), // Lv7  Pear
            new Color(1.00f, 0.75f, 0.65f), // Lv8  Peach
            new Color(0.95f, 0.85f, 0.10f), // Lv9  Pineapple
            new Color(0.40f, 0.80f, 0.30f), // Lv10 Melon
            new Color(0.15f, 0.65f, 0.20f), // Lv11 Watermelon
        };

        static Sprite MakePlaceholderSprite(int level)
        {
            const int size = 64;
            Color col = LevelColors[Mathf.Clamp(level - 1, 0, LevelColors.Length - 1)];

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - half + 0.5f;
                float dy = y - half + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = dist <= half - 1f ? 1f
                            : dist <= half     ? 1f - (dist - (half - 1f))
                            : 0f;
                tex.SetPixel(x, y, new Color(col.r, col.g, col.b, alpha));
            }
            tex.Apply();
            return Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size);
        }

        // ── 외부 제어 ──────────────────────────────────────
        /// <summary>드롭 시 물리 활성화. 낙하 전에는 Kinematic 으로 대기.</summary>
        public void SetKinematic(bool isKinematic)
        {
            _rb.bodyType = isKinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
            if (isKinematic)
                IsFalling = true;
        }

        /// <summary>드롭 실행 — Kinematic 해제 후 물리 적용.</summary>
        public void Drop()
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _spawnTime = Time.time;
            IsFalling = true;
        }
    }
}
