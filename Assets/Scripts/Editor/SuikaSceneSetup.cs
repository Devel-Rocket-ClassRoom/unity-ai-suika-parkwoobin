using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Suika.Editor
{
    /// <summary>
    /// Unity 상단 메뉴 "Suika → Setup Main Scene" 을 실행하면
    /// MainScene 에 필요한 모든 GameObject 를 자동으로 생성/배치한다.
    ///
    /// 실행 전 준비:
    ///   1. Assets/ScriptableObjects/FruitDatabase.asset 생성 완료
    ///   2. Assets/Prefabs/Fruits/ 에 과일 Prefab Lv1~11 준비 완료
    ///   3. 위 두 항목을 이 스크립트 실행 후 Inspector 에서 직접 연결
    /// </summary>
    public static class SuikaSceneSetup
    {
        // ── 컨테이너 치수 (ContainerWall 과 일치) ──────────────
        const float HalfWidth  = 2.5f;
        const float Height     = 6f;
        const float BottomY    = -4f;
        const float DangerY    = BottomY + Height - 1f;   // 위험선 Y
        const float DropY      = BottomY + Height + 0.5f; // 드롭 라인 Y

        [MenuItem("Suika/Setup Main Scene")]
        static void SetupScene()
        {
            // ── 1. GameManager ────────────────────────────────────
            GameObject gmObj = GetOrCreate("GameManager");
            EnsureComponent<GameManager>(gmObj);
            Undo.RegisterCreatedObjectUndo(gmObj, "Create GameManager");

            // ── 2. MergeHandler ───────────────────────────────────
            GameObject mhObj = GetOrCreate("MergeHandler");
            EnsureComponent<MergeHandler>(mhObj);
            Undo.RegisterCreatedObjectUndo(mhObj, "Create MergeHandler");

            // ── 3. Container (벽 + 바닥) ──────────────────────────
            GameObject container = GetOrCreate("Container");
            ContainerWall wall = EnsureComponent<ContainerWall>(container);
            wall.halfWidth = HalfWidth;
            wall.height    = Height;
            wall.bottomY   = BottomY;
            Undo.RegisterCreatedObjectUndo(container, "Create Container");

            // ── 4. DangerZone (위험선 트리거) ─────────────────────
            GameObject dangerObj = GetOrCreate("DangerZone");
            dangerObj.transform.position = new Vector3(0f, DangerY + 0.1f, 0f);

            BoxCollider2D dangerCol = EnsureComponent<BoxCollider2D>(dangerObj);
            dangerCol.isTrigger = true;
            dangerCol.size      = new Vector2(HalfWidth * 2f, 0.2f);

            DangerZoneDetector detector = EnsureComponent<DangerZoneDetector>(dangerObj);
            detector.gameOverDelay = 3f;
            Undo.RegisterCreatedObjectUndo(dangerObj, "Create DangerZone");

            // ── 5. FruitSpawner ───────────────────────────────────
            GameObject spawnerObj = GetOrCreate("FruitSpawner");
            FruitSpawner spawner = EnsureComponent<FruitSpawner>(spawnerObj);
            spawner.dropY       = DropY;
            spawner.leftBound   = -HalfWidth + 0.3f;
            spawner.rightBound  =  HalfWidth - 0.3f;
            Undo.RegisterCreatedObjectUndo(spawnerObj, "Create FruitSpawner");

            // ── 6. Camera ─────────────────────────────────────────
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic     = true;   // 2D 게임 — 반드시 직교 투영
                cam.orthographicSize = 6f;
                cam.transform.position = new Vector3(0f, -1f, -10f);
                cam.backgroundColor = new Color(1f, 0.97f, 0.91f); // #FFF8E7
            }

            // ── 7. Canvas + UI ────────────────────────────────────
            SetupUI(spawnerObj);

            // ── 8. 위험선 시각화 (LineRenderer) ──────────────────
            SetupDangerLine();

            EditorUtility.SetDirty(gmObj);
            EditorUtility.SetDirty(mhObj);
            EditorUtility.SetDirty(container);
            EditorUtility.SetDirty(spawnerObj);

            Debug.Log("[SuikaSceneSetup] 씬 구성 완료! " +
                      "MergeHandler·FruitSpawner 의 FruitDatabase·Prefabs 를 Inspector 에서 연결하세요.");
        }

        // ── UI 구성 ────────────────────────────────────────────────
        static void SetupUI(GameObject spawnerObj)
        {
            // Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            GameObject canvasObj;
            if (canvas == null)
            {
                canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvasObj = canvas.gameObject;
            }

            // EventSystem
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                // Both 모드 호환: StandaloneInputModule 유지
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Score UI
            GameObject scoreUIObj = GetOrCreateChild(canvasObj, "ScoreUI");
            ScoreUI scoreUI = EnsureComponent<ScoreUI>(scoreUIObj);

            // Current Score Text
            GameObject curScoreObj = GetOrCreateChild(scoreUIObj, "CurrentScore");
            RectTransform curRect = EnsureComponent<RectTransform>(curScoreObj);
            curRect.anchorMin = new Vector2(0f, 1f);
            curRect.anchorMax = new Vector2(0f, 1f);
            curRect.pivot     = new Vector2(0f, 1f);
            curRect.anchoredPosition = new Vector2(20f, -20f);
            curRect.sizeDelta = new Vector2(160f, 60f);
            TMP_Text curText = EnsureComponent<TextMeshProUGUI>(curScoreObj);
            curText.text = "SCORE\n0000";
            curText.fontSize = 24;
            curText.alignment = TextAlignmentOptions.Left;
            scoreUI.scoreText = curText;

            // Best Score Text
            GameObject bestScoreObj = GetOrCreateChild(scoreUIObj, "BestScore");
            RectTransform bestRect = EnsureComponent<RectTransform>(bestScoreObj);
            bestRect.anchorMin = new Vector2(1f, 1f);
            bestRect.anchorMax = new Vector2(1f, 1f);
            bestRect.pivot     = new Vector2(1f, 1f);
            bestRect.anchoredPosition = new Vector2(-20f, -20f);
            bestRect.sizeDelta = new Vector2(160f, 60f);
            TMP_Text bestText = EnsureComponent<TextMeshProUGUI>(bestScoreObj);
            bestText.text = "BEST\n0000";
            bestText.fontSize = 24;
            bestText.alignment = TextAlignmentOptions.Right;
            scoreUI.bestScoreText = bestText;

            // Next Fruit Preview
            GameObject previewObj = GetOrCreateChild(canvasObj, "NextFruitPreview");
            RectTransform prevRect = EnsureComponent<RectTransform>(previewObj);
            prevRect.anchorMin = new Vector2(1f, 1f);
            prevRect.anchorMax = new Vector2(1f, 1f);
            prevRect.pivot     = new Vector2(1f, 1f);
            prevRect.anchoredPosition = new Vector2(-20f, -100f);
            prevRect.sizeDelta = new Vector2(80f, 80f);

            // 스포너에 미리보기 위치 연결
            FruitSpawner spawner = spawnerObj.GetComponent<FruitSpawner>();
            if (spawner != null)
                spawner.nextFruitPreviewTransform = previewObj.transform;

            // GameOver UI
            GameObject goPanel = GetOrCreateChild(canvasObj, "GameOverPanel");
            RectTransform goRect = EnsureComponent<RectTransform>(goPanel);
            goRect.anchorMin = Vector2.zero;
            goRect.anchorMax = Vector2.one;
            goRect.sizeDelta = Vector2.zero;

            Image goBg = EnsureComponent<Image>(goPanel);
            goBg.color = new Color(0f, 0f, 0f, 0.7f);

            GameOverUI gameOverUI = EnsureComponent<GameOverUI>(goPanel);
            gameOverUI.gameOverPanel = goPanel;

            // Final Score
            GameObject finalScoreObj = GetOrCreateChild(goPanel, "FinalScore");
            RectTransform fsRect = EnsureComponent<RectTransform>(finalScoreObj);
            fsRect.anchorMin = new Vector2(0.5f, 0.6f);
            fsRect.anchorMax = new Vector2(0.5f, 0.6f);
            fsRect.sizeDelta = new Vector2(400f, 60f);
            fsRect.anchoredPosition = Vector2.zero;
            TMP_Text fsText = EnsureComponent<TextMeshProUGUI>(finalScoreObj);
            fsText.text = "SCORE  0";
            fsText.fontSize = 36;
            fsText.alignment = TextAlignmentOptions.Center;
            gameOverUI.finalScoreText = fsText;

            // Best Score (GameOver)
            GameObject goBestObj = GetOrCreateChild(goPanel, "BestScoreLabel");
            RectTransform gbRect = EnsureComponent<RectTransform>(goBestObj);
            gbRect.anchorMin = new Vector2(0.5f, 0.5f);
            gbRect.anchorMax = new Vector2(0.5f, 0.5f);
            gbRect.sizeDelta = new Vector2(400f, 60f);
            gbRect.anchoredPosition = Vector2.zero;
            TMP_Text gbText = EnsureComponent<TextMeshProUGUI>(goBestObj);
            gbText.text = "BEST   0";
            gbText.fontSize = 28;
            gbText.alignment = TextAlignmentOptions.Center;
            gameOverUI.bestScoreText = gbText;

            // Retry Button
            GameObject btnObj = GetOrCreateChild(goPanel, "RetryButton");
            RectTransform btnRect = EnsureComponent<RectTransform>(btnObj);
            btnRect.anchorMin = new Vector2(0.5f, 0.35f);
            btnRect.anchorMax = new Vector2(0.5f, 0.35f);
            btnRect.sizeDelta = new Vector2(200f, 60f);
            btnRect.anchoredPosition = Vector2.zero;
            Image btnImg = EnsureComponent<Image>(btnObj);
            btnImg.color = new Color(0.26f, 0.52f, 0.96f);
            Button btn = EnsureComponent<Button>(btnObj);
            gameOverUI.retryButton = btn;

            GameObject btnTextObj = GetOrCreateChild(btnObj, "Text");
            RectTransform btRect = EnsureComponent<RectTransform>(btnTextObj);
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;
            TMP_Text btnText = EnsureComponent<TextMeshProUGUI>(btnTextObj);
            btnText.text = "RETRY";
            btnText.fontSize = 24;
            btnText.alignment = TextAlignmentOptions.Center;

            goPanel.SetActive(false);
        }

        // ── 위험선 시각화 ─────────────────────────────────────────
        static void SetupDangerLine()
        {
            GameObject lineObj = GetOrCreate("DangerLine");
            LineRenderer lr = EnsureComponent<LineRenderer>(lineObj);
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(-HalfWidth, DangerY, 0f));
            lr.SetPosition(1, new Vector3( HalfWidth, DangerY, 0f));
            lr.startWidth = lr.endWidth = 0.04f;
            lr.material   = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = new Color(1f, 0.42f, 0.42f, 0.9f); // #FF6B6B
            lr.useWorldSpace = true;
        }

        // ── 유틸 ────────────────────────────────────────────────────
        static GameObject GetOrCreate(string name)
        {
            GameObject found = GameObject.Find(name);
            return found != null ? found : new GameObject(name);
        }

        static GameObject GetOrCreateChild(GameObject parent, string childName)
        {
            Transform t = parent.transform.Find(childName);
            if (t != null) return t.gameObject;
            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            return comp != null ? comp : go.AddComponent<T>();
        }
    }
}
