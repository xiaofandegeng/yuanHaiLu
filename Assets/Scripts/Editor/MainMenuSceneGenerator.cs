using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using YuanHaiLu.Core;
using YuanHaiLu.GameSystem;
using YuanHaiLu.UI;

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
            var lineImg = lineObj.AddComponent<UnityEngine.UI.Image>();
            lineImg.color = new Color(0.8f, 0.6f, 0.2f, 0.5f);

            // 标题
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(canvasObj.transform, false);
            var titleRT = titleObj.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.1f, 0.72f);
            titleRT.anchorMax = new Vector2(0.9f, 0.92f);
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
            btnCRT.anchorMin = new Vector2(0.3f, 0.15f);
            btnCRT.anchorMax = new Vector2(0.7f, 0.58f);
            var layout = btnContainer.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 12;
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

            // 底部版本信息
            var versionObj = new GameObject("Version");
            versionObj.transform.SetParent(canvasObj.transform, false);
            var verRT = versionObj.AddComponent<RectTransform>();
            verRT.anchorMin = new Vector2(0f, 0f);
            verRT.anchorMax = new Vector2(1f, 0.05f);
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

            EditorUtility.DisplayDialog("主菜单生成完成",
                "主菜单场景已创建！\n\n" +
                "使用方法：\n" +
                "1. 先生成 Demo 场景（Tools/渊海录/生成Demo场景）\n" +
                "2. File → Build Settings\n" +
                "3. 添加 MainMenu 和 Demo_YanLiuTown 两个场景\n" +
                "4. MainMenu 设为 index 0\n" +
                "5. Play 即可！",
                "了解");
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
            txt.fontSize = 18;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = textColor;
            txt.text = text;

            // 按钮大小
            var layoutElement = btnObj.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.preferredHeight = 35;
            layoutElement.minWidth = 150;

            // 颜色过渡
            var colors = btn.colors;
            colors.normalColor = new Color(0.15f, 0.12f, 0.2f);
            colors.highlightedColor = new Color(0.25f, 0.2f, 0.35f);
            colors.pressedColor = new Color(0.1f, 0.08f, 0.15f);
            btn.colors = colors;
        }
    }
}
