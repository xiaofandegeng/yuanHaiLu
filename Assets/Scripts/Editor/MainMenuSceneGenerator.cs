using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using YuanHaiLu.Core;
using YuanHaiLu.GameSystem;
using YuanHaiLu.UI;
using YuanHaiLu.Art;

namespace YuanHaiLu.Editor
{
    /// <summary>
    /// 主菜单场景生成器
    /// 菜单: Tools/渊海录/生成主菜单场景
    /// </summary>
    public static class MainMenuSceneGenerator
    {
        [MenuItem("Tools/渊海录/生成主菜单场景")]
        public static void Generate()
        {
            GenerateInternal(true);
        }

        public static void GenerateFromCommandLine()
        {
            GenerateInternal(false);
            SetupBuildSettings.Setup();
        }

        private static void GenerateInternal(bool showDialog)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 摄像机
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
            camObj.AddComponent<AudioListener>();

            // Canvas
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480, 270);
            canvasObj.AddComponent<GraphicRaycaster>();

            // 背景
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgRT = bgObj.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.08f, 0.06f, 0.12f);

            // 装饰线（上方）
            var lineObj = new GameObject("DecorLine");
            lineObj.transform.SetParent(canvasObj.transform, false);
            var lineRT = lineObj.AddComponent<RectTransform>();
            lineRT.anchorMin = new Vector2(0.1f, 0.7f);
            lineRT.anchorMax = new Vector2(0.9f, 0.71f);
            lineRT.offsetMin = Vector2.zero;
            lineRT.offsetMax = Vector2.zero;
            var lineImg = lineObj.AddComponent<UnityEngine.UI.Image>();
            lineImg.color = new Color(0.8f, 0.6f, 0.2f, 0.5f);

            // 标题
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(canvasObj.transform, false);
            var titleRT = titleObj.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.1f, 0.72f);
            titleRT.anchorMax = new Vector2(0.9f, 0.92f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;
            var titleText = titleObj.AddComponent<UnityEngine.UI.Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 42;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.85f, 0.3f);
            titleText.text = "渊 海 录";

            // 副标题
            var subtitleObj = new GameObject("Subtitle");
            subtitleObj.transform.SetParent(canvasObj.transform, false);
            var subRT = subtitleObj.AddComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0.2f, 0.64f);
            subRT.anchorMax = new Vector2(0.8f, 0.72f);
            subRT.offsetMin = Vector2.zero;
            subRT.offsetMax = Vector2.zero;
            var subText = subtitleObj.AddComponent<UnityEngine.UI.Text>();
            subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subText.fontSize = 14;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.7f, 0.7f, 0.7f);
            subText.text = "一剑天涯，问鼎江湖";

            // 按钮容器
            var btnContainer = new GameObject("ButtonContainer");
            btnContainer.transform.SetParent(canvasObj.transform, false);
            var btnCRT = btnContainer.AddComponent<RectTransform>();
            btnCRT.anchorMin = new Vector2(0.66f, 0.15f);
            btnCRT.anchorMax = new Vector2(0.94f, 0.58f);
            btnCRT.offsetMin = Vector2.zero;
            btnCRT.offsetMax = Vector2.zero;
            var layout = btnContainer.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // 创建按钮
            CreateMenuButton(btnContainer, "新游戏", Color.white);
            CreateMenuButton(btnContainer, "继续游戏", new Color(0.8f, 0.8f, 0.8f));
            CreateMenuButton(btnContainer, "设置", new Color(0.8f, 0.8f, 0.8f));
            CreateMenuButton(btnContainer, "退出", new Color(0.7f, 0.7f, 0.7f));

            CreateAppearanceSelector(canvasObj);

            // 底部版本信息
            var versionObj = new GameObject("Version");
            versionObj.transform.SetParent(canvasObj.transform, false);
            var verRT = versionObj.AddComponent<RectTransform>();
            verRT.anchorMin = new Vector2(0f, 0f);
            verRT.anchorMax = new Vector2(1f, 0.05f);
            verRT.offsetMin = Vector2.zero;
            verRT.offsetMax = Vector2.zero;
            var verText = versionObj.AddComponent<UnityEngine.UI.Text>();
            verText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            verText.fontSize = 10;
            verText.alignment = TextAnchor.MiddleCenter;
            verText.color = new Color(0.5f, 0.5f, 0.5f);
            verText.text = "渊海录 v0.1 Demo  |  Unity 6";

            // MainMenu 脚本
            canvasObj.AddComponent<MainMenu>();

            // GameManager（主菜单也需要）
            var gmObj = new GameObject("[GameManager]");
            gmObj.AddComponent<GameManager>();

            // AudioManager
            var audioObj = new GameObject("[AudioManager]");
            audioObj.AddComponent<AudioManager>();

            // SaveManager
            var saveObj = new GameObject("SaveManager");
            saveObj.transform.SetParent(gmObj.transform);
            saveObj.AddComponent<SaveManager>();

            // EventSystem
            var esObj = new GameObject("[EventSystem]");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // 保存
            string scenePath = "Assets/Scenes/MainMenu.unity";
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"[MainMenu] 主菜单场景生成完成: {scenePath}");

            if (showDialog)
            {
                EditorUtility.DisplayDialog("主菜单生成完成",
                    "主菜单场景已创建！\n\n" +
                    "可选择 2 种性别 × 6 种职业，然后开始游戏。\n" +
                    "全部正式区域与室内场景会由 Build Settings 工具自动加入。",
                    "了解");
            }
        }

        private static void CreateAppearanceSelector(GameObject canvasObject)
        {
            var panel = new GameObject("CharacterSelector");
            panel.transform.SetParent(canvasObject.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.13f);
            panelRect.anchorMax = new Vector2(0.62f, 0.64f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.07f, 0.12f, 0.94f);

            var previewObject = new GameObject("CharacterPreview");
            previewObject.transform.SetParent(panel.transform, false);
            var previewRect = previewObject.AddComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.17f, 0.68f);
            previewRect.anchorMax = new Vector2(0.17f, 0.68f);
            previewRect.pivot = new Vector2(0.5f, 0.5f);
            previewRect.sizeDelta = new Vector2(34f, 34f);
            var preview = previewObject.AddComponent<Image>();
            preview.color = Color.white;
            var defaultCatalog = CharacterArtCatalog.LoadDefault();
            if (!defaultCatalog.TryGet(PlayerAppearance.Default.ArtId, out var defaultEntry))
                throw new System.InvalidOperationException("Default formal player preview is missing.");
            var prefabRenderer = defaultEntry.Prefab.GetComponent<SpriteRenderer>();
            if (prefabRenderer == null || prefabRenderer.sprite == null)
                throw new System.InvalidOperationException("Default formal player idle sprite is missing.");
            preview.sprite = prefabRenderer.sprite;
            preview.preserveAspect = true;
            var labelObject = new GameObject("CharacterSelectionLabel");
            labelObject.transform.SetParent(panel.transform, false);
            var labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.32f, 0.70f);
            labelRect.anchorMax = new Vector2(0.96f, 0.94f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 14;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = new Color(1f, 0.82f, 0.35f);
            label.text = "主角：" + PlayerAppearance.Default.DisplayName;

            var hintObject = new GameObject("CharacterSelectionHint");
            hintObject.transform.SetParent(panel.transform, false);
            var hintRect = hintObject.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.32f, 0.50f);
            hintRect.anchorMax = new Vector2(0.96f, 0.70f);
            hintRect.offsetMin = Vector2.zero;
            hintRect.offsetMax = Vector2.zero;
            var hint = hintObject.AddComponent<Text>();
            hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hint.fontSize = 9;
            hint.alignment = TextAnchor.MiddleLeft;
            hint.color = new Color(0.68f, 0.68f, 0.72f);
            hint.text = "选择性别与入门流派";

            var gridObject = new GameObject("CharacterChoiceGrid");
            gridObject.transform.SetParent(panel.transform, false);
            var gridRect = gridObject.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.04f, 0.04f);
            gridRect.anchorMax = new Vector2(0.96f, 0.34f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;
            var grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            grid.cellSize = new Vector2(39f, 17f);
            grid.spacing = new Vector2(3f, 3f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            foreach (var appearance in PlayerAppearance.All)
                CreateAppearanceButton(gridObject, appearance);
        }

        private static void CreateAppearanceButton(GameObject parent, PlayerAppearance appearance)
        {
            var buttonObject = new GameObject("Btn_角色_" + appearance.ArtId);
            buttonObject.transform.SetParent(parent.transform, false);
            var image = buttonObject.AddComponent<Image>();
            image.color = appearance == PlayerAppearance.Default
                ? new Color(0.72f, 0.48f, 0.16f)
                : new Color(0.15f, 0.12f, 0.2f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 8;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = appearance.DisplayName.Replace(" · ", "");
        }

        private static void CreateMenuButton(GameObject parent, string text, Color textColor)
        {
            var btnObj = new GameObject($"Btn_{text}");
            btnObj.transform.SetParent(parent.transform, false);

            var img = btnObj.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.15f, 0.12f, 0.2f, 0.9f);

            var btn = btnObj.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;

            // 按钮文字
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRT = txtObj.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;

            var txt = txtObj.AddComponent<UnityEngine.UI.Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 13;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = textColor;
            txt.text = text;

            // 按钮大小
            var layoutElement = btnObj.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.preferredHeight = 24;
            layoutElement.minWidth = 105;

            // 颜色过渡
            var colors = btn.colors;
            colors.normalColor = new Color(0.15f, 0.12f, 0.2f);
            colors.highlightedColor = new Color(0.25f, 0.2f, 0.35f);
            colors.pressedColor = new Color(0.1f, 0.08f, 0.15f);
            btn.colors = colors;
        }
    }
}
