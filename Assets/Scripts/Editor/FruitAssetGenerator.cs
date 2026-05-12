using UnityEngine;
using UnityEditor;
using System.IO;

namespace Suika.Editor
{
    /// <summary>
    /// 메뉴 "Suika → Generate Fruit Assets" 실행 시
    /// FruitData × 11, FruitDatabase, Fruit Prefab × 11 을 자동 생성한다.
    /// 스프라이트가 없으면 임시 색상 원형으로 대체한다.
    /// </summary>
    public static class FruitAssetGenerator
    {
        // ── GDD 과일 데이터 ─────────────────────────────────────────
        static readonly FruitInfo[] Fruits =
        {
            new FruitInfo(1,  "Cherry",      0.22f, 1,  true,  new Color(0.90f, 0.10f, 0.10f)),
            new FruitInfo(2,  "Strawberry",  0.30f, 3,  true,  new Color(0.95f, 0.30f, 0.35f)),
            new FruitInfo(3,  "Grape",       0.38f, 6,  true,  new Color(0.55f, 0.20f, 0.70f)),
            new FruitInfo(4,  "Gyool",       0.50f, 10, true,  new Color(1.00f, 0.60f, 0.10f)),
            new FruitInfo(5,  "Orange",      0.62f, 15, true,  new Color(0.95f, 0.45f, 0.10f)),
            new FruitInfo(6,  "Apple",       0.75f, 21, false, new Color(0.85f, 0.15f, 0.15f)),
            new FruitInfo(7,  "Pear",        0.88f, 28, false, new Color(0.80f, 0.85f, 0.20f)),
            new FruitInfo(8,  "Peach",       1.03f, 36, false, new Color(1.00f, 0.75f, 0.65f)),
            new FruitInfo(9,  "Pineapple",   1.20f, 45, false, new Color(0.95f, 0.85f, 0.10f)),
            new FruitInfo(10, "Melon",       1.40f, 55, false, new Color(0.40f, 0.80f, 0.30f)),
            new FruitInfo(11, "Watermelon",  1.65f, 0,  false, new Color(0.15f, 0.65f, 0.20f)),
        };

        // Assets/png/ 폴더의 스프라이트 경로 (Lv1~11 순서)
        static readonly string[] PngPaths =
        {
            "Assets/png/00_cherry.png",
            "Assets/png/01_strawberry.png",
            "Assets/png/02_grape.png",
            "Assets/png/03_gyool.png",
            "Assets/png/04_orange.png",
            "Assets/png/05_apple.png",
            "Assets/png/06_pear.png",
            "Assets/png/07_peach.png",
            "Assets/png/08_pineapple.png",
            "Assets/png/09_melon.png",
            "Assets/png/10_watermelon.png",
        };

        // ── 경로 ───────────────────────────────────────────────────
        const string DataPath    = "Assets/ScriptableObjects";
        const string PrefabPath  = "Assets/Prefabs/Fruits";

        [MenuItem("Suika/Generate Fruit Assets")]
        static void Generate()
        {
            EnsureFolder(DataPath);
            EnsureFolder(PrefabPath);

            // PNG 텍스처 타입을 Sprite 로 강제 설정 + PPU = 이미지 크기 → 1 sprite = 1 unity unit
            foreach (string path in PngPaths)
            {
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;

                // 텍스처 크기 파악 (importSettings 적용 전 원본 크기)
                ti.GetSourceTextureWidthAndHeight(out int texW, out int texH);
                int ppu = Mathf.Max(texW, texH, 1);

                bool needsReimport = ti.textureType          != TextureImporterType.Sprite
                                  || ti.spriteImportMode     != SpriteImportMode.Single
                                  || ti.spritePixelsPerUnit  != ppu;
                if (needsReimport)
                {
                    ti.textureType         = TextureImporterType.Sprite;
                    ti.spriteImportMode    = SpriteImportMode.Single;
                    ti.spritePixelsPerUnit = ppu; // 1 텍스처 = 1 unity unit
                    ti.mipmapEnabled       = false;
                    ti.filterMode          = FilterMode.Bilinear;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }

            FruitData[] dataArray = new FruitData[Fruits.Length];

            for (int i = 0; i < Fruits.Length; i++)
            {
                FruitInfo info = Fruits[i];

                // ── 스프라이트 로드 (Assets/png/ 우선, 없으면 임시 원형) ──
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PngPaths[i]);
                if (sprite == null)
                {
                    Debug.LogWarning($"[FruitAssetGenerator] {PngPaths[i]} 를 찾을 수 없어 임시 스프라이트 사용");
                    sprite = MakeCircleSprite(info.color, 64);
                }

                // ── 1. FruitData ScriptableObject ──────────────────
                string dataAssetPath = $"{DataPath}/FruitData_Lv{info.level:D2}_{info.name}.asset";
                FruitData data = AssetDatabase.LoadAssetAtPath<FruitData>(dataAssetPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<FruitData>();
                    AssetDatabase.CreateAsset(data, dataAssetPath);
                }
                data.level      = info.level;
                data.fruitName  = info.name;
                data.radius     = info.radius;
                data.mergeScore = info.score;
                data.droppable  = info.droppable;
                data.sprite     = sprite;
                EditorUtility.SetDirty(data);
                dataArray[i] = data;

                // ── 2. Prefab ───────────────────────────────────────
                string prefabAssetPath = $"{PrefabPath}/Fruit_Lv{info.level:D2}_{info.name}.prefab";
                GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);

                if (existingPrefab == null)
                {
                    GameObject go = BuildFruitGameObject(info, data, sprite);
                    PrefabUtility.SaveAsPrefabAsset(go, prefabAssetPath);
                    Object.DestroyImmediate(go);
                }
                else
                {
                    using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabAssetPath))
                    {
                        ApplyFruitComponents(scope.prefabContentsRoot, info, data, sprite);
                    }
                }

                Debug.Log($"[FruitAssetGenerator] Lv{info.level} {info.name} 생성 완료");
            }

            AssetDatabase.SaveAssets();

            // ── 3. FruitDatabase ───────────────────────────────────
            string dbPath = $"{DataPath}/FruitDatabase.asset";
            FruitDatabase db = AssetDatabase.LoadAssetAtPath<FruitDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<FruitDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }
            db.fruits = dataArray;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ── 4. MergeHandler · FruitSpawner 에 자동 연결 ────────
            AutoWireScene(db);

            Debug.Log("[FruitAssetGenerator] 모든 에셋 생성 완료! MergeHandler·FruitSpawner 자동 연결됨.");
            EditorUtility.DisplayDialog("완료", "Fruit 에셋 생성 완료!\nMergeHandler·FruitSpawner가 자동으로 연결됐습니다.", "OK");
        }

        // ── 씬 오브젝트에 자동 연결 ──────────────────────────────────
        static void AutoWireScene(FruitDatabase db)
        {
            // Prefab 배열 로드
            GameObject[] prefabs = new GameObject[Fruits.Length];
            for (int i = 0; i < Fruits.Length; i++)
            {
                string path = $"{PrefabPath}/Fruit_Lv{Fruits[i].level:D2}_{Fruits[i].name}.prefab";
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            MergeHandler mh = Object.FindObjectOfType<MergeHandler>();
            if (mh != null)
            {
                mh.fruitDatabase = db;
                mh.fruitPrefabs  = prefabs;
                EditorUtility.SetDirty(mh);
            }

            FruitSpawner fs = Object.FindObjectOfType<FruitSpawner>();
            if (fs != null)
            {
                fs.fruitDatabase = db;
                fs.fruitPrefabs = prefabs;
                // 카메라가 미연결이면 자동 연결 (마우스 추적 버그 방지)
                if (fs.mainCamera == null)
                    fs.mainCamera = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>();
                EditorUtility.SetDirty(fs);
            }
        }

        // ── GameObject 생성 ──────────────────────────────────────────
        static GameObject BuildFruitGameObject(FruitInfo info, FruitData data, Sprite sprite)
        {
            GameObject go = new GameObject($"Fruit_Lv{info.level:D2}_{info.name}");
            ApplyFruitComponents(go, info, data, sprite);
            return go;
        }

        static void ApplyFruitComponents(GameObject go, FruitInfo info, FruitData data, Sprite sprite)
        {
            // SpriteRenderer
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            // 반지름에 맞게 스케일 조정 (스프라이트 기본 크기 1 unit 기준)
            float diameter = info.radius * 2f;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);

            // Rigidbody2D
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale   = 1f;
            rb.linearDamping  = 0.3f;
            rb.angularDamping = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // CircleCollider2D — 반지름을 0.5로 고정, 크기는 Scale로 표현
            CircleCollider2D col = go.GetComponent<CircleCollider2D>();
            if (col == null) col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            PhysicsMaterial2D mat = new PhysicsMaterial2D($"FruitMat_Lv{info.level}")
            {
                bounciness = 0.1f,
                friction   = 0.6f,
            };
            col.sharedMaterial = mat;

            // FruitBehaviour
            FruitBehaviour fb = go.GetComponent<FruitBehaviour>();
            if (fb == null) fb = go.AddComponent<FruitBehaviour>();
        }

        // ── 임시 원형 스프라이트 생성 ────────────────────────────────
        static Sprite MakeCircleSprite(Color color, int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - half + 0.5f;
                float dy = y - half + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= half - 1f)
                    tex.SetPixel(x, y, color);
                else if (dist <= half)
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b,
                        1f - (dist - (half - 1f))));
                else
                    tex.SetPixel(x, y, Color.clear);
            }
            tex.Apply();
            return Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size); // PPU = size → 1 Unity unit
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        // ── 데이터 구조체 ────────────────────────────────────────────
        struct FruitInfo
        {
            public int    level;
            public string name;
            public float  radius;
            public int    score;
            public bool   droppable;
            public Color  color;

            public FruitInfo(int level, string name, float radius,
                             int score, bool droppable, Color color)
            {
                this.level     = level;
                this.name      = name;
                this.radius    = radius;
                this.score     = score;
                this.droppable = droppable;
                this.color     = color;
            }
        }
    }
}
