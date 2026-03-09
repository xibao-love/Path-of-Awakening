using UnityEngine;
using UnityEngine.InputSystem;
using GameNetcodeStuff;
using Path_of_Awakening.Skills;
using System.IO;

namespace Path_of_Awakening
{
    public class SkillUI : MonoBehaviour
    {
        public static SkillUI Instance { get; private set; }

        private bool isPanelOpen = false;
        public string CrosshairText = "";

        private Rect windowRect = new Rect(0, 0, 560, 680);
        private Vector2 scrollPosition;

        private float mainUiScale = 1.0f;
        private bool showHud = false;
        private Rect hudRect = new Rect(20, 20, 250, 100);
        private float hudScale = 1.0f;

        private float nextMainUiScale = 1.0f;
        private bool nextShowHud = false;
        private float nextHudScale = 1.0f;

        private Texture2D bgTexture;

        // ==========================================
        // 【美化新增】导航栏分页状态
        // ==========================================
        private int currentTab = 0;
        private readonly string[] tabs = { "状态与队伍", "觉醒图鉴", "面板设置" };

        private void Awake()
        {
            Instance = this;
            LoadBackgroundTexture();
        }

        private void LoadBackgroundTexture()
        {
            string folderPath = Path.Combine(BepInEx.Paths.PluginPath, "PathOfAwakening");
            string filePath = Path.Combine(folderPath, "background.png");

            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(BepInEx.Paths.PluginPath, "background.png");
            }

            if (File.Exists(filePath))
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                bgTexture = new Texture2D(2, 2);
                bgTexture.LoadImage(fileData);
                Plugin.Log.LogInfo("成功加载 UI 背景图！");
            }
            else
            {
                Plugin.Log.LogWarning($"未找到 UI 背景图: {filePath}，将使用默认纯色背景。");
            }
        }

        private void Start()
        {
            windowRect.x = (Screen.width - windowRect.width) / 2f;
            windowRect.y = (Screen.height - windowRect.height) / 2f;
        }

        private void Update()
        {
            if (AwakeningInputs.Instance.ToggleUIPanelKey.triggered)
            {
                SetPanelState(!isPanelOpen);
            }

            mainUiScale = nextMainUiScale;
            hudScale = nextHudScale;
            showHud = nextShowHud;
        }

        public void SetPanelState(bool open)
        {
            isPanelOpen = open;
            PlayerControllerB localPlayer = GameNetworkManager.Instance?.localPlayerController;

            if (isPanelOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                if (localPlayer != null) localPlayer.disableLookInput = true;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                if (localPlayer != null) localPlayer.disableLookInput = false;
            }
        }

        private void OnGUI()
        {
            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            if (showHud)
            {
                DrawHUD();
            }

            if (isPanelOpen)
            {
                Rect scaledWindow = new Rect(windowRect.x, windowRect.y, 560 * mainUiScale, 680 * mainUiScale);
                string hotkeyName = AwakeningInputs.Instance.ToggleUIPanelKey.GetBindingDisplayString();

                // 去除原版部分黑边框，加宽顶部以容纳拖动区域
                GUIStyle winStyle = new GUIStyle(GUI.skin.window);
                winStyle.padding.top = (int)(25 * mainUiScale);

                windowRect = GUI.Window(0, scaledWindow, DrawSkillWindow, $"Path of Awakening - 觉醒面板 ({hotkeyName} 键关闭)", winStyle);
            }

            // 【美化修改】：为屏幕中央的文字增加伪描边阴影，防止在雪地等亮色地图看不清
            if (!string.IsNullOrEmpty(CrosshairText))
            {
                GUIStyle crosshairStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };

                Rect pos = new Rect(Screen.width / 2 - 200, Screen.height / 2 + 50, 400, 100);

                // 绘制黑色阴影
                GUI.color = Color.black;
                GUI.Label(new Rect(pos.x + 2, pos.y + 2, pos.width, pos.height), CrosshairText, crosshairStyle);
                // 绘制原色文字
                GUI.color = Color.white;
                GUI.Label(pos, CrosshairText, crosshairStyle);
            }
        }

        private void DrawHUD()
        {
            Rect scaledHud = new Rect(hudRect.x, hudRect.y, 250 * hudScale, 100 * hudScale);
            if (isPanelOpen)
            {
                hudRect = GUI.Window(1, scaledHud, DrawHUDWindow, "状态 HUD (按住此处拖动)");
            }
            else
            {
                GUI.Box(scaledHud, "");
                GUILayout.BeginArea(scaledHud);
                GUILayout.Space(10 * hudScale);
                RenderHudContent();
                GUILayout.EndArea();
            }
        }

        private void DrawHUDWindow(int windowID)
        {
            RenderHudContent();
            GUI.DragWindow(new Rect(0, 0, 10000, 25 * hudScale));
        }

        private void RenderHudContent()
        {
            GUIStyle hudTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = (int)(16 * hudScale), fontStyle = FontStyle.Bold, richText = true };
            GUIStyle hudTextStyle = new GUIStyle(GUI.skin.label) { fontSize = (int)(14 * hudScale), richText = true };

            PlayerControllerB localPlayer = GameNetworkManager.Instance?.localPlayerController;
            if (localPlayer == null) return;
            ulong localId = localPlayer.playerClientId;

            GUILayout.BeginHorizontal();
            GUILayout.Space(10 * hudScale);
            GUILayout.BeginVertical();

            GUILayout.Label("当前觉醒状态", hudTitleStyle);
            if (SkillManager.ActivePlayerSkills.TryGetValue(localId, out var mySkill))
            {
                GUILayout.Label($"技能: <color=#00FFFF>{mySkill.Name}</color>", hudTextStyle);
                GUILayout.Label($"状态: {mySkill.GetStatus(localId)}", hudTextStyle);
            }
            else
            {
                GUILayout.Label("技能: 无", hudTextStyle);
                GUILayout.Label("状态: <color=#888888>尚未觉醒</color>", hudTextStyle);
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawSkillWindow(int windowID)
        {
            if (bgTexture != null)
            {
                Color originalColor = GUI.color;
                GUI.color = new Color(0.4f, 0.4f, 0.4f, 0.95f); // 稍微压暗背景图片，大幅提高前方文字的对比度和辨识度
                GUI.DrawTexture(new Rect(0, 0, 560 * mainUiScale, 680 * mainUiScale), bgTexture, ScaleMode.StretchToFill);
                GUI.color = originalColor;
            }

            // ==========================================
            // 定义高颜值排版的 GUIStyle
            // ==========================================
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = (int)(18 * mainUiScale), fontStyle = FontStyle.Bold, richText = true };
            GUIStyle textStyle = new GUIStyle(GUI.skin.label) { fontSize = (int)(14 * mainUiScale), richText = true, wordWrap = true };
            GUIStyle subTextStyle = new GUIStyle(GUI.skin.label) { fontSize = (int)(13 * mainUiScale), richText = true, wordWrap = true };
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = (int)(14 * mainUiScale), richText = true };
            GUIStyle tabBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = (int)(15 * mainUiScale), fontStyle = FontStyle.Bold };

            // 卡片式的背景边框
            GUIStyle cardBoxStyle = new GUIStyle(GUI.skin.box);
            cardBoxStyle.padding = new RectOffset((int)(10 * mainUiScale), (int)(10 * mainUiScale), (int)(10 * mainUiScale), (int)(10 * mainUiScale));

            PlayerControllerB localPlayer = GameNetworkManager.Instance?.localPlayerController;
            if (localPlayer == null)
            {
                GUILayout.Label("<color=#FF5555>尚未连接至服务器，请在进入游戏房间后使用此面板...</color>", textStyle);
                return;
            }
            ulong localId = localPlayer.playerClientId;

            // ==========================================
            // 顶部导航栏 (Tabs)
            // ==========================================
            GUILayout.Space(5 * mainUiScale);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < tabs.Length; i++)
            {
                // 若是当前选中的分页，赋予青蓝色的高亮底色
                if (currentTab == i) GUI.backgroundColor = new Color(0.2f, 0.7f, 0.9f, 1f);
                else GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);

                if (GUILayout.Button(tabs[i], tabBtnStyle, GUILayout.Height(35 * mainUiScale)))
                {
                    currentTab = i;
                }
            }
            GUI.backgroundColor = Color.white; // 恢复默认颜色
            GUILayout.EndHorizontal();
            GUILayout.Space(15 * mainUiScale);

            // ==========================================
            // 分页具体内容渲染
            // ==========================================
            if (currentTab == 0) // 【状态与队伍】页面
            {
                // 模块1：我的技能信息
                GUILayout.BeginVertical(cardBoxStyle);
                GUILayout.Label("◆ 个人觉醒", titleStyle);
                GUILayout.Space(8 * mainUiScale);

                if (SkillManager.ActivePlayerSkills.TryGetValue(localId, out var mySkill))
                {
                    GUILayout.Label($"<color=#00FFFF>【{mySkill.Name}】</color>", new GUIStyle(textStyle) { fontSize = (int)(16 * mainUiScale), fontStyle = FontStyle.Bold });
                    GUILayout.Label($"<color=#DDDDDD>{mySkill.Description}</color>", textStyle);
                    GUILayout.Space(5 * mainUiScale);
                    GUILayout.Label($"当前触发状态: {mySkill.GetStatus(localId)}", textStyle);
                }
                else
                {
                    GUILayout.Label("<color=#FF5555>当前尚未选择觉醒技能 (请前往顶部【觉醒图鉴】页进行绑定)</color>", textStyle);
                }
                GUILayout.EndVertical();

                GUILayout.Space(20 * mainUiScale);

                // 模块2：队友状态监控
                GUILayout.BeginVertical(cardBoxStyle);
                GUILayout.Label("◆ 小队状态监控", titleStyle);
                GUILayout.Space(8 * mainUiScale);

                if (StartOfRound.Instance != null)
                {
                    bool hasTeammates = false;
                    foreach (var player in StartOfRound.Instance.allPlayerScripts)
                    {
                        if (player.isPlayerControlled && player.playerClientId != localId)
                        {
                            hasTeammates = true;
                            string teammateSkill = SkillManager.ActivePlayerSkills.TryGetValue(player.playerClientId, out var tSkill)
                                ? $"<color=#00FFFF>{tSkill.Name}</color>"
                                : "<color=#888888>未觉醒</color>";

                            // 追加死亡检测标签
                            string deathTag = player.isPlayerDead ? " <color=#FF0000>[已阵亡]</color>" : "";
                            GUILayout.Label($"• {player.playerUsername}{deathTag} : {teammateSkill}", textStyle);
                        }
                    }
                    if (!hasTeammates)
                    {
                        GUILayout.Label("<color=#888888>未检测到其他活跃的队友...</color>", textStyle);
                    }
                }
                GUILayout.EndVertical();
            }
            else if (currentTab == 1) // 【觉醒图鉴】页面
            {
                GUILayout.Label("◆ 觉醒图鉴 <size=12><color=#AAAAAA>(点击右侧按钮进行选择)</color></size>", titleStyle);
                GUILayout.Space(5 * mainUiScale);

                scrollPosition = GUILayout.BeginScrollView(scrollPosition, cardBoxStyle, GUILayout.Height(500 * mainUiScale));

                // 【关键美化】定义专属的高清描述字体：增大字号、左侧微微缩进增加层次感
                GUIStyle enhancedDescStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = (int)(15 * mainUiScale),
                    richText = true,
                    wordWrap = true,
                    padding = new RectOffset((int)(8 * mainUiScale), 0, 0, 0)
                };

                foreach (var kvp in SkillManager.SkillRegistry)
                {
                    var skill = kvp.Value;

                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.BeginHorizontal();

                    // 左侧：名字与详细描述
                    GUILayout.BeginVertical(GUILayout.Width(380 * mainUiScale)); // 稍微加宽文字区域

                    // 名字加上书名号并进一步高亮放大
                    GUILayout.Label($"<size={(int)(17 * mainUiScale)}><b><color=#00FFFF>{skill.Name}</color></b></size>", new GUIStyle(textStyle) { richText = true });
                    GUILayout.Space(6 * mainUiScale);

                    // 【关键美化】改用最高对比度的纯白色 #FFFFFF 渲染技能描述，告别灰暗
                    GUILayout.Label($"<color=#FFFFFF>{skill.Description}</color>", enhancedDescStyle);
                    GUILayout.Space(4 * mainUiScale);

                    GUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    // 右侧：选择按钮
                    GUILayout.BeginVertical();
                    GUILayout.Space(12 * mainUiScale); // 稍微下移，使其和左侧的多行文字垂直居中对齐
                    if (SkillManager.ActivePlayerSkills.ContainsKey(localId))
                    {
                        GUI.enabled = false;
                        GUILayout.Button("<color=#888888>已锁定</color>", btnStyle, GUILayout.Width(90 * mainUiScale), GUILayout.Height(35 * mainUiScale));
                        GUI.enabled = true;
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
                        if (GUILayout.Button("选择赋予", btnStyle, GUILayout.Width(90 * mainUiScale), GUILayout.Height(35 * mainUiScale)))
                        {
                            SkillManager.RequestSkill(localPlayer, skill.Id);
                        }
                        GUI.backgroundColor = Color.white;
                    }
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();

                    GUILayout.Space(10 * mainUiScale); // 卡片之间的间距稍微加大，让页面更有呼吸感
                }
                GUILayout.EndScrollView();
            }
            else if (currentTab == 2) // 【面板设置】页面
            {
                // 设置模块1：尺寸缩放
                GUILayout.BeginVertical(cardBoxStyle);
                GUILayout.Label("◆ 界面外观缩放", titleStyle);
                GUILayout.Space(15 * mainUiScale);

                GUILayout.BeginHorizontal();
                GUILayout.Label("主面板比例:", textStyle, GUILayout.Width(110 * mainUiScale));
                if (GUILayout.Button("缩小 (-)", btnStyle, GUILayout.Width(80 * mainUiScale))) nextMainUiScale = Mathf.Max(0.5f, mainUiScale - 0.1f);
                GUILayout.Label($"<color=#00FFFF>{Mathf.RoundToInt(mainUiScale * 100)}%</color>", textStyle, GUILayout.Width(60 * mainUiScale));
                if (GUILayout.Button("放大 (+)", btnStyle, GUILayout.Width(80 * mainUiScale))) nextMainUiScale = Mathf.Min(2.0f, mainUiScale + 0.1f);
                GUILayout.EndHorizontal();

                GUILayout.Space(15 * mainUiScale);

                GUILayout.BeginHorizontal();
                GUILayout.Label("HUD小窗比例:", textStyle, GUILayout.Width(110 * mainUiScale));
                if (GUILayout.Button("缩小 (-)", btnStyle, GUILayout.Width(80 * mainUiScale))) nextHudScale = Mathf.Max(0.5f, hudScale - 0.1f);
                GUILayout.Label($"<color=#00FFFF>{Mathf.RoundToInt(hudScale * 100)}%</color>", textStyle, GUILayout.Width(60 * mainUiScale));
                if (GUILayout.Button("放大 (+)", btnStyle, GUILayout.Width(80 * mainUiScale))) nextHudScale = Mathf.Min(2.0f, hudScale + 0.1f);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                GUILayout.Space(20 * mainUiScale);

                // 设置模块2：功能开关
                GUILayout.BeginVertical(cardBoxStyle);
                GUILayout.Label("◆ 偏好开关", titleStyle);
                GUILayout.Space(10 * mainUiScale);

                GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = (int)(15 * mainUiScale), richText = true };
                // 强制使用白色字体，防止游戏原版黑色主题导致设置文字难以辨认
                nextShowHud = GUILayout.Toggle(showHud, " <color=#FFFFFF>开启屏幕常驻状态 HUD (可在屏幕任意拖动)</color>", toggleStyle);
                GUILayout.EndVertical();
            }

            // 允许拖动整个窗口，拖动感应区域保留在顶部 30 像素
            GUI.DragWindow(new Rect(0, 0, 10000, 30 * mainUiScale));
        }
    }
}