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
    ///   - mainCamera : Game 카메라 직접 연결 (필수 — null 이면 마우스 추적 불가)
    ///   - nextFruitPreviewTransform : 미리보기 위치 Transform
    /// </summary>
    public class FruitSpawner : MonoBehaviour
    {
        [Header("참조")]
        public FruitDatabase fruitDatabase;
        public GameObject[] fruitPrefabs; // Lv1~11 순서

        /// <summary>
        /// Game 카메라를 Inspector 에서 직접 연결한다.
        /// null 이면 Camera.main 으로 폴백하지만, 씬 재로드 전에
        /// Tag 가 설정되지 않은 경우 마우스 추적이 동작하지 않는다.
        /// </summary>
        public Camera mainCamera;

        [Header("드롭 설정")]
        public float dropY = 4f; // 과일이 나타나는 Y 좌표
        public float leftBound = -2.3f;
        public float rightBound = 2.3f;

        [Header("미리보기")]
        public Transform nextFruitPreviewTransform; // UI 미리보기 위치
        public float previewScale = 0.5f;

        // ── 내부 상태 ────────────────────────────────────────
        FruitBehaviour _currentFruit;
        int _nextLevel;
        GameObject _previewObj;

        bool _canDrop = true;
        Camera _cam;

        void Start()
        {
            _cam = ResolveCamera();
            _nextLevel = GetRandomDropLevel();
            SpawnNext();
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                return;

            // 카메라 지연 초기화 — Start() 시점에 null 이었을 경우 재시도
            if (_cam == null)
                _cam = ResolveCamera();

            if (_currentFruit == null || _cam == null)
                return;

            // 마우스 스크린 좌표 → 월드 좌표 변환
            // orthographic 카메라: z 깊이값은 x·y 에 영향 없음 (nearClipPlane 사용).
            // perspective  카메라: 월드 z=0 평면까지의 거리를 전달해야 x·y 가 정확함.
            Vector3 mouseScreen = Input.mousePosition;
            mouseScreen.z = _cam.orthographic
                ? _cam.nearClipPlane
                : -_cam.transform.position.z;
            Vector3 mouseWorld = _cam.ScreenToWorldPoint(mouseScreen);

            float clampedX = Mathf.Clamp(mouseWorld.x, leftBound, rightBound);
            _currentFruit.transform.position = new Vector3(clampedX, dropY, 0f);

            if (_canDrop && Input.GetMouseButtonDown(0))
                Drop();
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
                Quaternion.identity
            );

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
            if (nextFruitPreviewTransform == null)
                return;

            if (_previewObj != null)
                Destroy(_previewObj);

            int idx = level - 1;
            _previewObj = Instantiate(
                fruitPrefabs[idx],
                nextFruitPreviewTransform.position,
                Quaternion.identity,
                nextFruitPreviewTransform
            );

            // 미리보기는 물리 비활성화
            Rigidbody2D rb = _previewObj.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.bodyType = RigidbodyType2D.Kinematic;

            Collider2D col = _previewObj.GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;

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

        /// <summary>
        /// mainCamera (Inspector 직접 연결) → Camera.main → FindObjectOfType 순으로 탐색.
        /// </summary>
        Camera ResolveCamera()
        {
            if (mainCamera != null)
                return mainCamera;
            if (Camera.main != null)
                return Camera.main;
            return FindObjectOfType<Camera>();
        }
    }
}
