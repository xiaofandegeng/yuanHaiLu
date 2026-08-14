using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using YuanHaiLu.Art;

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
            var previousAntiAliasing = QualitySettings.antiAliasing;
            try
            {
                QualitySettings.antiAliasing = 0;
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(fullOutputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
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
