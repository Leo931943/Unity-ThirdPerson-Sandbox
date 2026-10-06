using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonDemo.Editor
{
    public static class ThirdPersonSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ThirdPersonScene.unity";
        private const string MaterialsFolder = "Assets/Materials";

        [MenuItem("Tools/第三人稱場景 (Third Person)/1. 一鍵建立完整遊玩場景 (Build Complete Scene)", false, 1)]
        public static void BuildCompleteSceneMenu()
        {
            BuildCompleteScene(false);
        }

        public static void BuildCompleteScene(bool silent = false)
        {
            // 詢問使用者是否要儲存當前場景 (非靜默模式時)
            if (!silent && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureDirectories();

            // 建立全新空白場景
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 準備材質
            Material matGround = GetOrCreateMaterial("M_Ground", new Color(0.18f, 0.20f, 0.24f), 0.2f, 0.8f);
            Material matGroundAccent = GetOrCreateMaterial("M_GroundAccent", new Color(0.24f, 0.27f, 0.32f), 0.3f, 0.7f);
            Material matPlayer = GetOrCreateMaterial("M_Player", new Color(0.0f, 0.72f, 0.83f), 0.4f, 0.5f);
            Material matVisor = GetOrCreateMaterial("M_Visor", new Color(1.0f, 0.75f, 0.1f), 0.8f, 0.2f);
            Material matPlatform = GetOrCreateMaterial("M_Platform", new Color(0.35f, 0.4f, 0.5f), 0.3f, 0.6f);
            Material matObstacle = GetOrCreateMaterial("M_Obstacle", new Color(0.95f, 0.45f, 0.25f), 0.3f, 0.6f);
            Material matCollectible = GetOrCreateMaterial("M_Collectible", new Color(0.1f, 0.9f, 0.5f), 0.9f, 0.1f);
            Material matBoundary = GetOrCreateMaterial("M_Boundary", new Color(0.12f, 0.14f, 0.17f), 0.1f, 0.9f);

            // 1. 建立燈光
            CreateLighting();

            // 2. 建立地圖環境
            CreateEnvironment(matGround, matGroundAccent, matPlatform, matObstacle, matBoundary);

            // 3. 建立收集物 (水晶)
            CreateCollectibles(matCollectible);

            // 4. 建立玩家角色
            GameObject player = CreatePlayer(matPlayer, matVisor);

            // 5. 建立第三人稱跟隨相機
            GameObject cameraObj = CreateCamera(player.transform);

            // 讓玩家控制器關聯相機
            ThirdPersonController controller = player.GetComponent<ThirdPersonController>();
            controller.cameraTransform = cameraObj.transform;

            // 6. 建立 HUD 介面
            CreateHUD();

            // 儲存場景
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            // 將新場景加入 Build Settings (如果尚未加入)
            AddSceneToBuildSettings(ScenePath);

            // 選取玩家並在視圖中聚焦
            Selection.activeGameObject = player;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            Debug.Log("<color=#00ff88><b>[ThirdPerson] 第三人稱自由走動場景已建立並載入成功！</b></color> 點擊頂部 ▶ Play 按鈕即可開始遊玩！");

            if (!silent)
            {
                EditorUtility.DisplayDialog(
                    "第三人稱場景建立完成！",
                    "場景已成功建立並儲存至：\n" + ScenePath + "\n\n現在可以直接點擊 Unity 上方的 ▶ (Play) 按鈕開始體驗！\n\n操作方式：\n• W A S D：移動\n• Shift：衝刺跑步\n• Space：跳躍\n• 滑鼠：自由環視\n• 滾輪：鏡頭縮放\n• ESC：釋放滑鼠",
                    "立即開始！"
                );
            }
        }

        [MenuItem("Tools/第三人稱場景 (Third Person)/2. 在當前場景新增第三人稱主角 (Add Player Only)", false, 2)]
        public static void AddPlayerToCurrentScene()
        {
            EnsureDirectories();

            // 防呆處理：先清理當前場景中已存在的玩家物件，防止多出第二個角色同步移動
            var existingPlayers = Object.FindObjectsByType<ThirdPersonController>(FindObjectsSortMode.None);
            foreach (var ep in existingPlayers)
            {
                Undo.DestroyObjectImmediate(ep.gameObject);
            }

            Material matPlayer = GetOrCreateMaterial("M_Player", new Color(0.0f, 0.72f, 0.83f), 0.4f, 0.5f);
            Material matVisor = GetOrCreateMaterial("M_Visor", new Color(1.0f, 0.75f, 0.1f), 0.8f, 0.2f);

            Vector3 spawnPos = Vector3.up * 1.5f;
            if (SceneView.lastActiveSceneView != null)
            {
                spawnPos = SceneView.lastActiveSceneView.pivot;
                spawnPos.y += 1.0f;
            }

            GameObject player = CreatePlayer(matPlayer, matVisor);
            player.transform.position = spawnPos;

            // 相機設定
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = CreateCamera(player.transform).GetComponent<Camera>();
            }
            else
            {
                ThirdPersonCamera tpCam = mainCam.GetComponent<ThirdPersonCamera>();
                if (tpCam == null) tpCam = mainCam.gameObject.AddComponent<ThirdPersonCamera>();
                tpCam.target = player.transform;
            }

            ThirdPersonController controller = player.GetComponent<ThirdPersonController>();
            controller.cameraTransform = mainCam.transform;

            CreateHUD();

            Selection.activeGameObject = player;
            Undo.RegisterCreatedObjectUndo(player, "Create Third Person Player");
        }

        [MenuItem("Tools/第三人稱場景 (Third Person)/3. 打開第三人稱遊玩場景 (Open Scene)", false, 3)]
        public static void OpenScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            if (File.Exists(ScenePath))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(ScenePath);
                }
            }
            else
            {
                if (EditorUtility.DisplayDialog("提示", "尚未建立第三人稱場景，是否現在自動建立？", "建立場景", "取消"))
                {
                    BuildCompleteScene();
                }
            }
        }

        [MenuItem("Tools/第三人稱場景 (Third Person)/4. 清理多餘的重複角色 (Clean Duplicate Players)", false, 4)]
        public static void CleanDuplicatePlayersMenu()
        {
            CleanDuplicatePlayersInScene(false);
        }

        public static int CleanDuplicatePlayersInScene(bool silent = true)
        {
            var controllers = Object.FindObjectsByType<ThirdPersonController>(FindObjectsSortMode.None);
            if (controllers.Length <= 1)
            {
                if (!silent)
                {
                    EditorUtility.DisplayDialog("提示", "目前場景中只有單一玩家角色，沒有重複角色需要清理。", "確定");
                }
                return 0;
            }

            int removed = 0;
            for (int i = 1; i < controllers.Length; i++)
            {
                Undo.DestroyObjectImmediate(controllers[i].gameObject);
                removed++;
            }

            Debug.Log($"<color=#00ff88>[ThirdPerson] 已成功清理 {removed} 個多餘的重複角色物件！</color>");
            if (!silent)
            {
                EditorUtility.DisplayDialog("清理完成", $"已成功移除 {removed} 個多餘的玩家角色，現在只保留一個主角！", "確定");
            }
            return removed;
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
        }

        private static Material GetOrCreateMaterial(string name, Color color, float metallic = 0.2f, float smoothness = 0.5f)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                mat = new Material(shader);
                mat.color = color;

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }
                if (mat.HasProperty("_Metallic"))
                {
                    mat.SetFloat("_Metallic", metallic);
                }
                if (mat.HasProperty("_Smoothness"))
                {
                    mat.SetFloat("_Smoothness", smoothness);
                }

                AssetDatabase.CreateAsset(mat, path);
            }

            return mat;
        }

        private static void CreateLighting()
        {
            GameObject lightObj = new GameObject("Directional Light (Sun)");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.96f, 0.90f);
            light.intensity = 1.3f;
            light.shadows = LightShadows.Soft;

            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.0f;
        }

        private static void CreateEnvironment(Material matGround, Material matAccent, Material matPlatform, Material matObstacle, Material matBoundary)
        {
            GameObject envRoot = new GameObject("Environment");

            // 1. 主地板 (80x80)
            GameObject mainGround = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainGround.name = "Main Ground";
            mainGround.transform.SetParent(envRoot.transform);
            mainGround.transform.position = new Vector3(0f, -0.5f, 0f);
            mainGround.transform.localScale = new Vector3(80f, 1f, 80f);
            mainGround.GetComponent<Renderer>().sharedMaterial = matGround;

            // 2. 中央地標圓盤/平台
            GameObject centerPlaza = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            centerPlaza.name = "Center Plaza";
            centerPlaza.transform.SetParent(envRoot.transform);
            centerPlaza.transform.position = new Vector3(0f, 0.05f, 0f);
            centerPlaza.transform.localScale = new Vector3(12f, 0.1f, 12f);
            centerPlaza.GetComponent<Renderer>().sharedMaterial = matAccent;

            // 核心修正：Unity 預設 PrimitiveType.Cylinder 帶有 CapsuleCollider。
            // 由於扁平比例 (12, 0.1, 12)，PhysX 會將其判定為半徑 6 米的巨大隱形球體，
            // 導致玩家踩不上去且無法靠近中間的水晶！此處替換為貼合網格的 MeshCollider。
            Object.DestroyImmediate(centerPlaza.GetComponent<Collider>());
            centerPlaza.AddComponent<MeshCollider>();

            // 3. 邊緣防護矮牆 (防止玩家不慎走出邊界)
            CreateBoundaryWalls(envRoot.transform, matBoundary);

            // 4. 斜坡區域 (測試角色爬坡與下坡黏地)
            CreateRamps(envRoot.transform, matPlatform);

            // 5. 階梯區域 (測試 Step Offset 階梯攀爬)
            CreateStairs(envRoot.transform, matAccent, matPlatform);

            // 6. 跑酷高台與浮島 (測試跳躍能力)
            CreateParkourCourse(envRoot.transform, matPlatform, matObstacle);

            // 7. 柱子與障礙走廊 (測試相機防穿牆與走動閃避)
            CreateObstacleCourse(envRoot.transform, matObstacle);
        }

        private static void CreateBoundaryWalls(Transform parent, Material mat)
        {
            float size = 80f;
            float thickness = 1.5f;
            float height = 1.2f;

            Vector3[] positions = new Vector3[]
            {
                new Vector3(0, height * 0.5f, size * 0.5f),
                new Vector3(0, height * 0.5f, -size * 0.5f),
                new Vector3(size * 0.5f, height * 0.5f, 0),
                new Vector3(-size * 0.5f, height * 0.5f, 0)
            };

            Vector3[] scales = new Vector3[]
            {
                new Vector3(size, height, thickness),
                new Vector3(size, height, thickness),
                new Vector3(thickness, height, size),
                new Vector3(thickness, height, size)
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"Border Wall {i + 1}";
                wall.transform.SetParent(parent);
                wall.transform.position = positions[i];
                wall.transform.localScale = scales[i];
                wall.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        private static void CreateRamps(Transform parent, Material mat)
        {
            GameObject rampGroup = new GameObject("Ramps Area");
            rampGroup.transform.SetParent(parent);

            // 緩坡 (約 18 度)
            GameObject gentleRamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gentleRamp.name = "Gentle Ramp (18 deg)";
            gentleRamp.transform.SetParent(rampGroup.transform);
            gentleRamp.transform.position = new Vector3(-15f, 1.2f, 15f);
            gentleRamp.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
            gentleRamp.transform.localScale = new Vector3(4f, 0.4f, 10f);
            gentleRamp.GetComponent<Renderer>().sharedMaterial = mat;

            // 緩坡頂部平台
            GameObject rampTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rampTop.name = "Ramp Top Platform";
            rampTop.transform.SetParent(rampGroup.transform);
            rampTop.transform.position = new Vector3(-15f, 2.7f, 22.5f);
            rampTop.transform.localScale = new Vector3(6f, 0.6f, 6f);
            rampTop.GetComponent<Renderer>().sharedMaterial = mat;

            // 陡坡 (約 30 度)
            GameObject steepRamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            steepRamp.name = "Steep Ramp (30 deg)";
            steepRamp.transform.SetParent(rampGroup.transform);
            steepRamp.transform.position = new Vector3(-8f, 1.8f, 15f);
            steepRamp.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
            steepRamp.transform.localScale = new Vector3(3f, 0.4f, 8f);
            steepRamp.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateStairs(Transform parent, Material matStairs, Material matTop)
        {
            GameObject stairsGroup = new GameObject("Staircase Area");
            stairsGroup.transform.SetParent(parent);

            int stepCount = 8;
            float stepWidth = 4f;
            float stepHeight = 0.25f;
            float stepDepth = 0.6f;

            Vector3 startPos = new Vector3(15f, 0f, 10f);

            for (int i = 0; i < stepCount; i++)
            {
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Step_{i + 1}";
                step.transform.SetParent(stairsGroup.transform);
                float y = (i + 1) * stepHeight * 0.5f;
                float z = startPos.z + i * stepDepth;
                step.transform.position = new Vector3(startPos.x, y, z);
                step.transform.localScale = new Vector3(stepWidth, (i + 1) * stepHeight, stepDepth);
                step.GetComponent<Renderer>().sharedMaterial = matStairs;
            }

            // 階梯頂部觀景台
            GameObject topPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topPlatform.name = "Observation Deck";
            topPlatform.transform.SetParent(stairsGroup.transform);
            topPlatform.transform.position = new Vector3(15f, stepCount * stepHeight - 0.2f, startPos.z + (stepCount * stepDepth) + 3f);
            topPlatform.transform.localScale = new Vector3(8f, 0.4f, 7f);
            topPlatform.GetComponent<Renderer>().sharedMaterial = matTop;
        }

        private static void CreateParkourCourse(Transform parent, Material matPlatform, Material matObstacle)
        {
            GameObject parkourGroup = new GameObject("Parkour Platforms");
            parkourGroup.transform.SetParent(parent);

            // 連續跳躍平台
            Vector3[] platformPositions = new Vector3[]
            {
                new Vector3(-12f, 1.0f, -10f),
                new Vector3(-16f, 1.8f, -14f),
                new Vector3(-20f, 2.6f, -10f),
                new Vector3(-24f, 3.5f, -15f),
                new Vector3(-20f, 4.5f, -22f),
                new Vector3(-13f, 5.5f, -24f) // 最高浮島
            };

            for (int i = 0; i < platformPositions.Length; i++)
            {
                GameObject plat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plat.name = $"Parkour_Plat_{i + 1}";
                plat.transform.SetParent(parkourGroup.transform);
                plat.transform.position = platformPositions[i];
                plat.transform.localScale = new Vector3(3.2f, 0.5f, 3.2f);
                plat.GetComponent<Renderer>().sharedMaterial = (i == platformPositions.Length - 1) ? matObstacle : matPlatform;
            }
        }

        private static void CreateObstacleCourse(Transform parent, Material mat)
        {
            GameObject obstacleGroup = new GameObject("Obstacles & Pillars");
            obstacleGroup.transform.SetParent(parent);

            // 一排石柱長廊
            for (int i = 0; i < 6; i++)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Pillar_{i + 1}";
                pillar.transform.SetParent(obstacleGroup.transform);
                pillar.transform.position = new Vector3(5f + (i % 2) * 5f, 2.5f, -10f - i * 3.5f);
                pillar.transform.localScale = new Vector3(1.2f, 2.5f, 1.2f);
                pillar.GetComponent<Renderer>().sharedMaterial = mat;
            }

            // 一座拱門
            GameObject archLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            archLeft.name = "Arch_Pillar_L";
            archLeft.transform.SetParent(obstacleGroup.transform);
            archLeft.transform.position = new Vector3(-3f, 2.5f, 12f);
            archLeft.transform.localScale = new Vector3(1f, 5f, 1f);
            archLeft.GetComponent<Renderer>().sharedMaterial = mat;

            GameObject archRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            archRight.name = "Arch_Pillar_R";
            archRight.transform.SetParent(obstacleGroup.transform);
            archRight.transform.position = new Vector3(3f, 2.5f, 12f);
            archRight.transform.localScale = new Vector3(1f, 5f, 1f);
            archRight.GetComponent<Renderer>().sharedMaterial = mat;

            GameObject archTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            archTop.name = "Arch_Beam";
            archTop.transform.SetParent(obstacleGroup.transform);
            archTop.transform.position = new Vector3(0f, 5.2f, 12f);
            archTop.transform.localScale = new Vector3(7f, 0.6f, 1.2f);
            archTop.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateCollectibles(Material mat)
        {
            GameObject group = new GameObject("Collectibles (Crystals)");

            Vector3[] positions = new Vector3[]
            {
                new Vector3(0f, 1.2f, 5f),        // 前方中央
                new Vector3(0f, 6.2f, 12f),       // 拱門頂上
                new Vector3(15f, 3.0f, 18f),      // 階梯觀景台
                new Vector3(-15f, 3.8f, 22.5f),   // 緩坡頂部
                new Vector3(-13f, 6.7f, -24f),    // 跑酷最高浮島
                new Vector3(-20f, 3.8f, -10f),    // 跑酷中途
                new Vector3(7.5f, 1.2f, -18f),    // 柱子長廊
                new Vector3(-25f, 1.2f, 20f)      // 秘密角落
            };

            for (int i = 0; i < positions.Length; i++)
            {
                // 以菱形方塊製作水晶外觀
                GameObject gem = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gem.name = $"Crystal_{i + 1}";
                gem.transform.SetParent(group.transform);
                gem.transform.position = positions[i];
                gem.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
                gem.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

                gem.GetComponent<Renderer>().sharedMaterial = mat;

                // 設為 Trigger 觸發器，使用球體觸發器以提供更舒適自然的收集判定
                Object.DestroyImmediate(gem.GetComponent<Collider>());
                SphereCollider sc = gem.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 0.8f;

                // 加上 Kinematic Rigidbody 確保 PhysX 穩定觸發 Trigger 事件
                Rigidbody rb = gem.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                gem.AddComponent<CollectibleItem>();
            }
        }

        private static GameObject CreatePlayer(Material matBody, Material matVisor)
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1.0f, 0f);

            // 1. CharacterController 組件
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 2.0f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0f, 0f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 50f;

            // 2. ThirdPersonController 移動邏輯腳本
            ThirdPersonController controller = player.AddComponent<ThirdPersonController>();
            controller.jumpHeight = 2.6f;
            controller.spawnPosition = new Vector3(0f, 1.0f, 0f);

            // 3. 視覺外觀 (膠囊體)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "BodyVisual";
            body.transform.SetParent(player.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.9f, 1.0f, 0.9f);
            body.GetComponent<Renderer>().sharedMaterial = matBody;

            // 移除子物件碰撞體，避免與 CharacterController 衝突
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // 4. 角色前方護目鏡/面罩 (辨識朝向)
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "VisorEyes";
            visor.transform.SetParent(player.transform);
            visor.transform.localPosition = new Vector3(0f, 0.5f, 0.4f);
            visor.transform.localScale = new Vector3(0.5f, 0.22f, 0.25f);
            visor.GetComponent<Renderer>().sharedMaterial = matVisor;
            Object.DestroyImmediate(visor.GetComponent<Collider>());

            return player;
        }

        private static GameObject CreateCamera(Transform playerTarget)
        {
            GameObject camObj = GameObject.FindWithTag("MainCamera");
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            camObj.transform.position = new Vector3(0f, 2.5f, -4.5f);
            camObj.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

            Camera cam = camObj.GetComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;

            ThirdPersonCamera tpCam = camObj.GetComponent<ThirdPersonCamera>();
            if (tpCam == null)
            {
                tpCam = camObj.AddComponent<ThirdPersonCamera>();
            }
            tpCam.target = playerTarget;

            return camObj;
        }

        private static void CreateHUD()
        {
            GameObject hud = GameObject.Find("HUD");
            if (hud == null)
            {
                hud = new GameObject("HUD");
                hud.AddComponent<SimpleHUD>();
            }
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }

            EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
            {
                newScenes[i] = scenes[i];
            }
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }

    [InitializeOnLoad]
    public static class ThirdPersonAutoInitializer
    {
        static ThirdPersonAutoInitializer()
        {
            EditorApplication.delayCall += CheckAndAutoBuild;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private static void OnHierarchyChanged()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var controllers = Object.FindObjectsByType<ThirdPersonController>(FindObjectsSortMode.None);
            if (controllers.Length > 1)
            {
                Debug.LogWarning($"<color=#ffaa00>[ThirdPerson] 檢測到編輯器場景中存在多餘的玩家角色 ({controllers.Length} 個)，已自動為您清理多餘副本！</color>");
                for (int i = 1; i < controllers.Length; i++)
                {
                    Undo.DestroyObjectImmediate(controllers[i].gameObject);
                }
            }
        }

        private static void CheckAndAutoBuild()
        {
            if (!File.Exists("Assets/Scenes/ThirdPersonScene.unity"))
            {
                Debug.Log("<color=#00e5ff>[ThirdPerson] 正在自動建立第三人稱場景...</color>");
                ThirdPersonSceneBuilder.BuildCompleteScene(silent: true);
            }
        }
    }
}
