using UnityEngine;

namespace Suika
{
    public class FruitSpawner : MonoBehaviour
    {
        [Header("참조")]
        public FruitDatabase fruitDatabase;
        public GameObject[] fruitPrefabs; // Lv1~11 순서
        public Camera mainCamera;

        [Header("드롭 설정")]
        public float dropY = 4f;
        public float leftBound = -2.3f;
        public float rightBound = 2.3f;

        [Header("구름 홀더")]
        [Tooltip("구름 이미지 스프라이트를 여기에 연결")]
        public Sprite cloudSprite;
        [Tooltip("과일 중심 기준 구름 중심의 Y 오프셋")]
        public float cloudOffsetY = 0.2f;
        [Tooltip("구름 고정 크기 (Unity unit)")]
        public float cloudSize = 1.2f;
        [Tooltip("구름의 가로:세로 비율")]
        public float cloudAspect = 0.55f;
        [Tooltip("과일 sortingOrder보다 낮게 설정 — 구름이 과일 아래 레이어")]
        public int cloudSortingOrder = 5;

        [Header("미리보기")]
        public Transform nextFruitPreviewTransform;
        public float previewScale = 0.5f;

        // ── 내부 상태 ─────────────────────────────────────────
        FruitBehaviour _currentFruit;
        int _nextLevel;
        GameObject _previewObj;
        bool _canDrop = true;
        Camera _cam;

        // 구름 오브젝트
        GameObject _cloudObj;
        SpriteRenderer _cloudSr;

        void Start()
        {
            _cam = ResolveCamera();
            _nextLevel = GetRandomDropLevel();
            CreateCloud();
            SpawnNext();
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            {
                SetCloudVisible(false);
                return;
            }

            if (_cam == null) _cam = ResolveCamera();
            if (_cam == null) return;

            // 마우스 → 월드 좌표 (과일 유무와 관계없이 항상 계산)
            Vector3 mouseScreen = Input.mousePosition;
            mouseScreen.z = _cam.orthographic
                ? _cam.nearClipPlane
                : -_cam.transform.position.z;
            float clampedX = Mathf.Clamp(
                _cam.ScreenToWorldPoint(mouseScreen).x,
                leftBound, rightBound);

            // 구름은 항상 커서를 따라 이동 (낙하 중에도)
            MoveCloud(clampedX);

            if (_currentFruit == null) return;

            // 과일 위치 + 드롭
            _currentFruit.transform.position = new Vector3(clampedX, dropY, 0f);

            if (_canDrop && Input.GetMouseButtonDown(0))
                Drop();
        }

        // ── 구름 생성 ─────────────────────────────────────────
        void CreateCloud()
        {
            if (_cloudObj != null) Destroy(_cloudObj);

            _cloudObj = new GameObject("FruitCloud");
            _cloudObj.transform.SetParent(transform, false);

            _cloudSr = _cloudObj.AddComponent<SpriteRenderer>();
            _cloudSr.sprite = cloudSprite;
            _cloudSr.sortingOrder = cloudSortingOrder;

            SetCloudVisible(false);
        }

        void MoveCloud(float x)
        {
            if (_cloudObj == null) return;

            _cloudObj.transform.position = new Vector3(x, dropY + cloudOffsetY, 0f);
            _cloudObj.transform.localScale = new Vector3(cloudSize, cloudSize * cloudAspect, 1f);

            SetCloudVisible(true);
        }

        void SetCloudVisible(bool visible)
        {
            if (_cloudSr != null)
                _cloudSr.enabled = visible;
        }

        // ── 드롭 ─────────────────────────────────────────────
        void Drop()
        {
            _canDrop = false;

            // 구름은 사라지지 않고 다음 과일 생성 때까지 제자리 유지
            _currentFruit.Drop();
            _currentFruit = null;

            Invoke(nameof(SpawnNext), 0.5f);
        }

        // ── 다음 과일 준비 ────────────────────────────────────
        void SpawnNext()
        {
            int spawnLevel = _nextLevel;
            _nextLevel = GetRandomDropLevel();

            int prefabIdx = spawnLevel - 1;
            GameObject obj = Instantiate(
                fruitPrefabs[prefabIdx],
                new Vector3(0f, dropY, 0f),
                Quaternion.identity
            );

            _currentFruit = obj.GetComponent<FruitBehaviour>();
            _currentFruit.Initialize(fruitDatabase.GetByLevel(spawnLevel));
            _currentFruit.SetKinematic(true);

            // 과일은 구름보다 위 레이어 (구름이 과일 아래에 그려짐)
            SpriteRenderer fruitSr = obj.GetComponent<SpriteRenderer>();
            if (fruitSr != null)
                fruitSr.sortingOrder = cloudSortingOrder + 1;

            UpdatePreview(_nextLevel);
            _canDrop = true;
        }

        // ── 미리보기 ──────────────────────────────────────────
        void UpdatePreview(int level)
        {
            if (nextFruitPreviewTransform == null) return;

            if (_previewObj != null) Destroy(_previewObj);

            int idx = level - 1;
            // 부모 기준 localPosition = (0,0,0) 으로 생성해 정중앙 정렬
            _previewObj = Instantiate(fruitPrefabs[idx], nextFruitPreviewTransform);
            _previewObj.transform.localPosition = Vector3.zero;
            _previewObj.transform.localRotation = Quaternion.identity;

            Rigidbody2D rb = _previewObj.GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

            Collider2D col = _previewObj.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            FruitBehaviour fb = _previewObj.GetComponent<FruitBehaviour>();
            if (fb != null)
            {
                fb.Initialize(fruitDatabase.GetByLevel(level));
                fb.enabled = false;
            }

            // Initialize() 가 scale 을 덮어쓰므로 이후에 고정 크기 적용
            _previewObj.transform.localScale = Vector3.one * previewScale;
        }

        // ── 헬퍼 ─────────────────────────────────────────────
        int GetRandomDropLevel() => Random.Range(1, 6);

        Camera ResolveCamera()
        {
            if (mainCamera != null) return mainCamera;
            if (Camera.main != null) return Camera.main;
            return FindObjectOfType<Camera>();
        }
    }
}
