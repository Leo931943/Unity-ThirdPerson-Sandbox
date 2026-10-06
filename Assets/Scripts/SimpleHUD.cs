using UnityEngine;

namespace ThirdPersonDemo
{
    public class SimpleHUD : MonoBehaviour
    {
        public static int Score { get; private set; } = 0;
        public static int TotalCollectibles = 0;

        private ThirdPersonController playerController;
        private GUIStyle titleStyle;
        private GUIStyle infoStyle;
        private GUIStyle boxStyle;
        private GUIStyle winStyle;
        private bool stylesInitialized = false;

        public static void AddScore(int amount)
        {
            Score += amount;
        }

        public static void ResetGame()
        {
            Score = 0;
        }

        private void Start()
        {
            ResetGame();
            playerController = FindFirstObjectByType<ThirdPersonController>();
            TotalCollectibles = FindObjectsByType<CollectibleItem>(FindObjectsSortMode.None).Length;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            // 背景框樣式
            boxStyle = new GUIStyle(GUI.skin.box);
            Texture2D bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0.08f, 0.1f, 0.14f, 0.85f));
            bgTex.Apply();
            boxStyle.normal.background = bgTex;

            // 標題樣式
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.85f, 1f) }
            };

            // 內文樣式
            infoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.92f, 0.95f, 0.98f) }
            };

            // 獲勝提示樣式
            winStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 1f, 0.4f) }
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            // 1. 左上角：操作指引與狀態面板
            float panelWidth = 260f;
            float panelHeight = 220f;
            Rect panelRect = new Rect(20, 20, panelWidth, panelHeight);

            GUI.Box(panelRect, GUIContent.none, boxStyle);

            GUILayout.BeginArea(new Rect(30, 28, panelWidth - 20, panelHeight - 16));
            GUILayout.Label("🎮 第三人稱自由探索", titleStyle);
            GUILayout.Space(6);

            GUILayout.Label("• [W A S D]  前後左右移動", infoStyle);
            GUILayout.Label("• [Shift]   按住衝刺快跑", infoStyle);
            GUILayout.Label("• [Space]   跳躍", infoStyle);
            GUILayout.Label("• [滑鼠移動]  旋轉第三人稱視角", infoStyle);
            GUILayout.Label("• [滑鼠滾輪]  拉近 / 拉遠鏡頭", infoStyle);
            GUILayout.Label("• [ESC]     鎖定 / 釋放滑鼠", infoStyle);
            GUILayout.Label("• [R]       回到起點", infoStyle);

            GUILayout.EndArea();

            // 2. 右上角：玩家實時狀態與目標
            float statWidth = 220f;
            float statHeight = 130f;
            Rect statRect = new Rect(Screen.width - statWidth - 20, 20, statWidth, statHeight);

            GUI.Box(statRect, GUIContent.none, boxStyle);

            GUILayout.BeginArea(new Rect(Screen.width - statWidth - 10, 28, statWidth - 20, statHeight - 16));
            GUILayout.Label("📊 角色狀態", titleStyle);
            GUILayout.Space(4);

            if (playerController != null)
            {
                string stateText = playerController.Grounded ? (playerController.IsSprinting ? "衝刺中 🏃‍♂️" : "地面走動 🚶‍♂️") : "滯空跳躍中 🪂";
                GUILayout.Label($"狀態: {stateText}", infoStyle);
                GUILayout.Label($"移動速度: {playerController.Speed:F1} m/s", infoStyle);
            }

            GUILayout.Space(4);
            if (TotalCollectibles > 0)
            {
                GUILayout.Label($"💎 水晶收集: {Score} / {TotalCollectibles}", infoStyle);
                if (Score >= TotalCollectibles)
                {
                    GUILayout.Label("🎉 恭喜！全部水晶收集完畢！", winStyle);
                }
            }
            GUILayout.EndArea();

            // R 鍵重置起點
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.R)
            {
                if (playerController != null)
                {
                    playerController.Respawn();
                }
            }
        }
    }
}
