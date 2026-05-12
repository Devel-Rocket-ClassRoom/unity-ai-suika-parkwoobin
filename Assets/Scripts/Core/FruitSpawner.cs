using UnityEngine;

namespace Suika
{
    /// <summary>
    /// 마우스 X 위치에 맞춰 현재 과일을 표시하다가
    /// 클릭 시 낙하시킨다. 다음 과일을 미리 준비한다.
    ///
    /// 설정 방법:
    ///   - 이 컴포넌트를 빈 GameObject(FruitSpawner) 에 추가
    ///   - dropY : 드롭 라인 Y 좌표 (Inspector 에서 지정)
    ///   - leftBound / rightBound : 컨테이너 좌우 X 한계
    ///   - fruitDatabase, fruitPrefabs 연결
    ///   - nextFruitPreviewTransform : 미리보기 위치 Transform
    /// </summary>
    public class FruitSpawner : MonoBehaviour
    {
        [Header("참조")]
        public FruitDatabase fruitDatabase;
        public GameObject[] fruitPrefabs;       // Lv1~11 순서

        [Header("드롭 설정")]
        public float dropY = 4f;                // 과일이 나타나는 Y 좌표
        public float leftBound = -2.3f;
        public float rightBound = 2.3f;

        [Header("미리보기")]
        public Transform nextFruitPreviewTransform;  // UI 미리보기 위치
        public float previewScale = 0.5f;

        // ── 내부 상태 ────────────────────────────────────────
        FruitBehaviour _currentFruit;
        int _nextLevel;
        GameObject _previewObj;

        bool _canDrop = true;
        Camera _cam;

        void Start()
        {
            _cam = Camera.main;
            _nextLevel = GetRandomDropLevel();
            SpawnNext();
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            // 카메라 지연 초기화 — Start() 시점에 null이었을 경우 재시도
            if (_cam == null) _cam = Camera.main;

            if (_currentFruit == null || _cam == null) return;

            // 마우스 X → 월드 X 변환
            // ScreenToWorldPoint 의 z 는 '카메라로부터의 거리(depth)' 를 의미한다.
            // 카메라가 (0, -1, -10) 에 있으면 월드 z=0 평면까지 거리는 10.
            Vector3 mouseScreen = Input.mousePosition;
            mouseScreen.z = -_cam.transform.position.z; // 월드 z=0 평면까지의 거리
            Vector3 mouseWorld = _cam.ScreenToWorldPoint(mouseScreen);
            float clampedX = Mathf.Clamp(mouseWorld.x, leftBound, rightBound);
            _currentFruit.transform.position = new Vector3(clampedX, dropY, 0f);

            // 클릭 시 드롭
            if (_canDrop && Input.GetMouseButtonDown(0))
            {
                Drop();
            }
        }

        // ── 드롭 ────────────────────────────────────────────
        void Drop()
        {
            _canDrop = false;
            _currentFruit.Drop();
            _currentFruit = null;

            // 잠깐 대기 후 다음 과일 생성
            Invoke(nameof(SpawnNext), 0.5f);
        }

        // ── 다음 과일 준비 ───────────────────────────────────
        void SpawnNext()
        {
            int spawnLevel = _nextLevel;
            _nextLevel = GetRandomDropLevel();

            // 현재 과일 생성 (Kinematic — 아직 공중에 있음)
            int prefabIdx = spawnLevel - 1;
            GameObject obj = Instantiate(
                fruitPrefabs[prefabIdx],
                new Vector3(0f, dropY, 0f),
                Quaternion.identity);

            _currentFruit = obj.GetComponent<FruitBehaviour>();
            _currentFruit.Initialize(fruitDatabase.GetByLevel(spawnLevel));
            _currentFruit.SetKinematic(true);

            // 미리보기 업데이트
            UpdatePreview(_nextLevel);
            _canDrop = true;
        }

        // ── 미리보기 ─────────────────────────────────────────
        void UpdatePreview(int level)
        {
            if (nextFruitPreviewTransform == null) return;

            if (_previewObj != null) Destroy(_previewObj);

            int idx = level - 1;
            _previewObj = Instantiate(
                fruitPrefabs[idx],
                nextFruitPreviewTransform.position,
                Quaternion.identity,
                nextFruitPreviewTransform);

            // 미리보기는 물리 비활성화
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

            _previewObj.transform.localScale = Vector3.one * previewScale;
        }

        // ── 헬퍼 ────────────────────────────────────────────
        /// <summary>GDD §2-1: 드롭 가능 과일 Lv1~5 중 랜덤.</summary>
        int GetRandomDropLevel() => Random.Range(1, 6);
    }
}
