using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using YuanHaiLu.Art;
using YuanHaiLu.Core;
using YuanHaiLu.GameSystem;
using YuanHaiLu.UI;

namespace YuanHaiLu.Editor
{
    /// <summary>
    /// Captures fixed-size review images without leaving a target scene, canvas,
    /// camera target, or quality setting behind in the editor.
    /// </summary>
    public static class VisualRegressionCapture
    {
        public const int Width = 480;
        public const int Height = 270;
        private const string ReviewDirectory = "/private/tmp/yuanhailu-art-review";

        public static void CaptureScene(string sceneId, string outputPath)
        {
            CaptureScene(sceneId, outputPath, "normal");
        }

        public static void CaptureScene(string sceneId, string outputPath, string environmentState)
        {
            if (string.IsNullOrWhiteSpace(sceneId))
                throw new ArgumentException("A formal scene id is required.", nameof(sceneId));
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("An output path is required.", nameof(outputPath));

            var before = CaptureEditorState.Read();
            Scene captureScene = default;
            var addedScene = false;
            GameObject cameraObject = null;
            RegionEnvironmentController environmentController = null;
            string previousEnvironmentState = null;
            try
            {
                captureScene = OpenForCapture(RegionSceneBuilder.ScenePath(sceneId), out addedScene);
                environmentController = captureScene.GetRootGameObjects()
                    .Select(root => root.GetComponent<RegionEnvironmentController>())
                    .FirstOrDefault(value => value != null);
                if (environmentController != null)
                    previousEnvironmentState = environmentController.CurrentEnvironmentState;
                if (environmentController != null && environmentState != "normal")
                    environmentController.SetEnvironmentState(environmentState);
                else if (environmentController == null && environmentState != "normal")
                    throw new ArgumentException(
                        $"'{sceneId}' has no environment state '{environmentState}'.",
                        nameof(environmentState));

                cameraObject = CreateWorldCamera(captureScene);
                RenderCamera(cameraObject.GetComponent<Camera>(), outputPath);
            }
            finally
            {
                if (environmentController != null && previousEnvironmentState != null &&
                    environmentController.CurrentEnvironmentState != previousEnvironmentState)
                    environmentController.SetEnvironmentState(previousEnvironmentState);
                if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
                if (addedScene && captureScene.IsValid() && captureScene.isLoaded)
                    EditorSceneManager.CloseScene(captureScene, true);
                before.Restore();
            }
        }

        public static void CaptureMainMenu(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("An output path is required.", nameof(outputPath));

            var before = CaptureEditorState.Read();
            Scene captureScene = default;
            var addedScene = false;
            CanvasSnapshot canvasSnapshot = default;
            try
            {
                captureScene = OpenForCapture("Assets/Scenes/MainMenu.unity", out addedScene);
                var camera = FindInScene<Camera>(captureScene);
                var canvas = FindInScene<Canvas>(captureScene);
                if (camera == null || canvas == null)
                    throw new InvalidOperationException("MainMenu requires both a camera and a canvas.");

                canvasSnapshot = CanvasSnapshot.Read(canvas);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
                RenderCamera(camera, outputPath);
            }
            finally
            {
                canvasSnapshot.Restore();
                if (addedScene && captureScene.IsValid() && captureScene.isLoaded)
                    EditorSceneManager.CloseScene(captureScene, true);
                before.Restore();
            }
        }

        public static float ChangedPixelRatio(string expectedPath, string actualPath)
        {
            Texture2D expected = null;
            Texture2D actual = null;
            try
            {
                expected = LoadImage(expectedPath);
                actual = LoadImage(actualPath);
                if (expected.width != actual.width || expected.height != actual.height)
                    throw new ArgumentException("Images must have identical dimensions.");
                var expectedPixels = expected.GetPixels32();
                var actualPixels = actual.GetPixels32();
                var changed = 0;
                for (var index = 0; index < expectedPixels.Length; index++)
                    if (expectedPixels[index].Equals(actualPixels[index])) continue;
                    else changed++;
                return changed / (float)expectedPixels.Length;
            }
            finally
            {
                if (expected != null) UnityEngine.Object.DestroyImmediate(expected);
                if (actual != null) UnityEngine.Object.DestroyImmediate(actual);
            }
        }

        [MenuItem("Tools/渊海录/美术/截取临时正式美术验收图")]
        public static void CaptureTemporaryReview()
        {
            Directory.CreateDirectory(ReviewDirectory);
            CaptureMainMenu(Path.Combine(ReviewDirectory, "main-menu.png"));
            foreach (var id in OutdoorSceneIds)
                CaptureScene(id, Path.Combine(ReviewDirectory, id + ".png"));
            CaptureScene(
                "prologue_village",
                Path.Combine(ReviewDirectory, "prologue_village-burned.png"),
                "burned");
            Debug.Log("[VisualRegressionCapture] wrote review images to " + ReviewDirectory);
        }

        public static void CaptureTemporaryReviewFromCommandLine()
        {
            CaptureTemporaryReview();
        }

        // ========== docs/16 MVP 试玩画面实拍（Gate R1 审查输入） ==========

        private const string MvpReviewDirectory =
            "/private/tmp/yuanhailu-mvp-rework-review";

        /// <summary>
        /// 三张 480×270、1×、无标注的 Demo 游戏实拍（docs/16 E.3）：
        /// 客栈门外出生点、河岸战斗点、客栈掌柜柜台。
        /// 使用场景自带像素相机与绑定 UI，输出前不作为正式视觉基线。
        /// </summary>
        public static void CaptureMvpGameplay(string reviewDirectory)
        {
            Directory.CreateDirectory(reviewDirectory);
            CaptureDemoGameplayFrame(
                "Assets/Scenes/Demo_YanLiuTown.unity",
                // The real new-game spawn is one tile below the inn threshold.
                // Keeping the review hero here verifies that the 32px silhouette
                // reads on the path, rather than hiding it inside the doorway.
                new Vector2(7.5f, 7.6f),
                Path.Combine(reviewDirectory, "town-spawn-1x.png"));
            CaptureDemoGameplayFrame(
                "Assets/Scenes/Demo_YanLiuTown.unity",
                new Vector2(15f, 4.2f),
                Path.Combine(reviewDirectory, "town-riverbank-1x.png"));
            CaptureDemoGameplayFrame(
                "Assets/Scenes/Demo_Inn.unity",
                // 柜台前一格：同屏可辨男主、掌柜和交互距离，避免把审查角色
                // 摆在左下角而看不出“进入后去找谁”的主流程。
                new Vector2(15f, 8.4f),
                Path.Combine(reviewDirectory, "inn-counter-1x.png"));
            Debug.Log("[VisualRegressionCapture] wrote MVP gameplay images to " + reviewDirectory);
        }

        public static void CaptureMvpGameplayFromCommandLine()
        {
            CaptureMvpGameplay(MvpReviewDirectory);
        }

        [MenuItem("Tools/渊海录/美术/截取MVP试玩复核图")]
        public static void CaptureMvpGameplayFromEditor()
        {
            CaptureMvpGameplay(MvpReviewDirectory);
        }

        private static void CaptureDemoGameplayFrame(
            string scenePath, Vector2 playerPosition, string outputPath)
        {
            var before = CaptureEditorState.Read();
            Scene captureScene = default;
            var addedScene = false;
            Vector3 previousPlayerPosition = default;
            GameObject player = null;
            Camera gameCamera = null;
            List<GameObject> hiddenForeignRoots = null;
            Dictionary<GameObject, bool> reviewQuestTargets = null;
            try
            {
                captureScene = OpenForCapture(scenePath, out addedScene);
                // OpenForCapture is additive so it can restore the editor exactly, but
                // a Camera renders objects from every loaded scene.  Isolate the target
                // roots or an already-open inn will paint over a town review frame.
                hiddenForeignRoots = HideForeignSceneRoots(captureScene);
                var pixelCamera = FindInScene<PixelPerfectCamera>(captureScene);
                if (pixelCamera == null)
                    throw new InvalidOperationException(
                        "Demo scene requires a PixelPerfectCamera for gameplay captures.");
                gameCamera = pixelCamera.GetComponent<Camera>();

                player = captureScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Select(value => value.gameObject)
                    .FirstOrDefault(value => value.CompareTag("Player"));
                if (player == null)
                    throw new InvalidOperationException(
                        "Demo scene requires a Player for gameplay captures.");
                previousPlayerPosition = player.transform.position;
                player.transform.position = playerPosition;

                // 河岸截图审查的是“接到任务后的战斗读图”，不是接任务前正确隐藏
                // 的空场。仅在编辑器截图期间临时展示 Gate 管控的水匪与荷包，finally
                // 中逐一还原，因此不改变实际任务门控或已保存场景。
                reviewQuestTargets = RevealRiverbankCombatForReview(captureScene, outputPath);

                // 审查图必须走真实 CameraFollow 规则。旧实现直接改相机位置，绕过
                // 地图边界，导致出生点附近露出场景外清屏色并误判为画面问题。
                var follow = gameCamera.GetComponent<CameraFollow>();
                if (follow == null)
                    throw new InvalidOperationException(
                        "Demo scene requires CameraFollow for gameplay captures.");
                follow.SetTarget(player.transform);
                follow.SnapToTarget();
                // 编辑模式不走 Awake/Start；显式构建一次 HUD，保证实拍含 UI（docs/16 E.5）。
                BuildRuntimeUiForCapture(captureScene);
                Canvas.ForceUpdateCanvases();
                RenderCamera(gameCamera, outputPath);
            }
            finally
            {
                if (player != null)
                    player.transform.position = previousPlayerPosition;
                if (reviewQuestTargets != null)
                {
                    foreach (var pair in reviewQuestTargets)
                        if (pair.Key != null)
                            pair.Key.SetActive(pair.Value);
                }
                if (hiddenForeignRoots != null)
                {
                    foreach (var root in hiddenForeignRoots)
                        if (root != null)
                            root.SetActive(true);
                }
                if (addedScene && captureScene.IsValid() && captureScene.isLoaded)
                    EditorSceneManager.CloseScene(captureScene, true);
                before.Restore();
            }
        }

        private static List<GameObject> HideForeignSceneRoots(Scene captureScene)
        {
            var hidden = new List<GameObject>();
            for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded || scene == captureScene)
                    continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (!root.activeSelf)
                        continue;
                    root.SetActive(false);
                    hidden.Add(root);
                }
            }
            return hidden;
        }

        private static Dictionary<GameObject, bool> RevealRiverbankCombatForReview(
            Scene scene, string outputPath)
        {
            var targets = new Dictionary<GameObject, bool>();
            if (!Path.GetFileName(outputPath).Equals(
                    "town-riverbank-1x.png", StringComparison.Ordinal))
                return targets;

            foreach (var gate in scene.GetRootGameObjects()
                         .SelectMany(root => root.GetComponentsInChildren<QuestStageGate>(true)))
            {
                if (gate == null ||
                    (gate.targetId != "river_bandit" &&
                     gate.targetId != "quest_lost_pouch"))
                    continue;
                foreach (var target in gate.targets)
                {
                    if (target == null || targets.ContainsKey(target))
                        continue;
                    targets.Add(target, target.activeSelf);
                    target.SetActive(true);
                }
            }
            return targets;
        }

        private static void BuildRuntimeUiForCapture(Scene scene)
        {
            var hud = FindInScene<HUD>(scene);
            if (hud == null)
                throw new InvalidOperationException(
                    "Demo scene requires a HUD for gameplay captures.");
            var buildUi = typeof(HUD).GetMethod(
                "BuildUI", BindingFlags.Instance | BindingFlags.NonPublic);
            if (buildUi == null)
                throw new InvalidOperationException("HUD.BuildUI was not found for capture.");
            buildUi.Invoke(hud, null);
        }

        private static readonly string[] OutdoorSceneIds =
        {
            "prologue_village", "luoyuan", "tianshu", "yanliu", "cangyue",
            "jueyun", "chisha", "youhuang", "hanyuan", "zhenyue"
        };

        private static Scene OpenForCapture(string scenePath, out bool addedScene)
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var existing = SceneManager.GetSceneAt(index);
                if (existing.path == scenePath)
                {
                    addedScene = false;
                    return existing;
                }
            }

            addedScene = true;
            return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .FirstOrDefault(component => component != null);
        }

        private static GameObject CreateWorldCamera(Scene scene)
        {
            var cameraObject = new GameObject("VisualReviewCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.transform.position = new Vector3(20f, 12f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 13.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(18, 29, 25, 255);
            camera.cullingMask = ~0;
            return cameraObject;
        }

        private static void RenderCamera(Camera camera, string outputPath)
        {
            var fullOutputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath) ?? Path.GetTempPath());
            var renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point
            };
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            var previousActive = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            var previousPixelRect = camera.pixelRect;
            var previousAntiAliasing = QualitySettings.antiAliasing;
            var pixelCamera = camera.GetComponent<PixelPerfectCamera>();
            try
            {
                QualitySettings.antiAliasing = 0;
                camera.targetTexture = renderTexture;
                // 必须先绑定 RT 再计算 pixelRect。若反过来，Unity 会用当前编辑器
                // Game View 的物理尺寸钳制 rect；渲染到 480×270 时便只画出中央
                // 362px，左右露出清屏色，截图看起来像缩略图。
                if (pixelCamera != null)
                    pixelCamera.UpdateCameraForScreen(Width, Height);
                else
                    camera.pixelRect = new Rect(0f, 0f, Width, Height);
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(fullOutputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                if (pixelCamera != null)
                {
                    pixelCamera.UpdateCameraForScreen(
                        previousTarget != null ? previousTarget.width : Screen.width,
                        previousTarget != null ? previousTarget.height : Screen.height);
                }
                camera.pixelRect = previousPixelRect;
                RenderTexture.active = previousActive;
                QualitySettings.antiAliasing = previousAntiAliasing;
                UnityEngine.Object.DestroyImmediate(texture);
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static Texture2D LoadImage(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Capture image was not found.", path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("Unable to decode capture image: " + path);
            }
            return texture;
        }

        private readonly struct CanvasSnapshot
        {
            private readonly Canvas canvas;
            private readonly RenderMode renderMode;
            private readonly Camera worldCamera;
            private readonly float planeDistance;

            private CanvasSnapshot(Canvas canvas)
            {
                this.canvas = canvas;
                renderMode = canvas.renderMode;
                worldCamera = canvas.worldCamera;
                planeDistance = canvas.planeDistance;
            }

            public static CanvasSnapshot Read(Canvas canvas)
            {
                return canvas == null ? default : new CanvasSnapshot(canvas);
            }

            public void Restore()
            {
                if (canvas == null) return;
                canvas.renderMode = renderMode;
                canvas.worldCamera = worldCamera;
                canvas.planeDistance = planeDistance;
            }
        }
    }

    /// <summary>Observable editor state used by visual-capture regression tests.</summary>
    public readonly struct CaptureEditorState : IEquatable<CaptureEditorState>
    {
        private readonly string sceneSignature;
        private readonly string activeScenePath;
        private readonly RenderTexture activeRenderTexture;
        private readonly int antiAliasing;

        private CaptureEditorState(
            string sceneSignature,
            string activeScenePath,
            RenderTexture activeRenderTexture,
            int antiAliasing)
        {
            this.sceneSignature = sceneSignature;
            this.activeScenePath = activeScenePath;
            this.activeRenderTexture = activeRenderTexture;
            this.antiAliasing = antiAliasing;
        }

        public static CaptureEditorState Read()
        {
            var paths = new List<string>();
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                paths.Add(scene.path + "#" + scene.name);
            }
            return new CaptureEditorState(
                string.Join("|", paths),
                SceneManager.GetActiveScene().path,
                RenderTexture.active,
                QualitySettings.antiAliasing);
        }

        public void Restore()
        {
            RenderTexture.active = activeRenderTexture;
            QualitySettings.antiAliasing = antiAliasing;
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.path == activeScenePath)
                {
                    SceneManager.SetActiveScene(scene);
                    return;
                }
            }
        }

        public bool Equals(CaptureEditorState other)
        {
            return sceneSignature == other.sceneSignature
                && activeScenePath == other.activeScenePath
                && ReferenceEquals(activeRenderTexture, other.activeRenderTexture)
                && antiAliasing == other.antiAliasing;
        }

        public override bool Equals(object obj)
        {
            return obj is CaptureEditorState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = sceneSignature == null ? 0 : sceneSignature.GetHashCode();
                hash = (hash * 397) ^ (activeScenePath == null ? 0 : activeScenePath.GetHashCode());
                hash = (hash * 397) ^ (activeRenderTexture == null ? 0 : activeRenderTexture.GetInstanceID());
                return (hash * 397) ^ antiAliasing;
            }
        }
    }
}
