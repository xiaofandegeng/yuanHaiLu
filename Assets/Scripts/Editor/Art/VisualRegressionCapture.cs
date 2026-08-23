using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YuanHaiLu.Art;

namespace YuanHaiLu.Editor
{
    public static class VisualRegressionCapture
    {
        public const int Width = 480;
        public const int Height = 270;
        public const string BaselineRoot = "Assets/Tests/VisualBaselines";

        public static readonly string[] OutdoorSceneIds =
        {
            "tianshu", "cangyue", "yanliu", "chisha", "youhuang", "hanyuan",
            "prologue_village", "luoyuan", "jueyun", "zhenyue"
        };

        [MenuItem("Tools/渊海录/美术/重建全部视觉基线")]
        public static void CaptureAllBaselines()
        {
            Directory.CreateDirectory(BaselineRoot);
            CaptureMainMenu(Path.Combine(BaselineRoot, "MainMenu.png"));
            foreach (string sceneId in OutdoorSceneIds)
                CaptureScene(sceneId, BaselinePath(sceneId));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[VisualRegression] captured MainMenu and 10 outdoor baselines at 480x270.");
        }

        public static void CaptureAllFromCommandLine()
        {
            CaptureAllBaselines();
        }

        public static string BaselinePath(string sceneId)
        {
            return Path.Combine(BaselineRoot, sceneId + ".png").Replace('\\', '/');
        }

        public static void CaptureScene(string sceneId, string outputPath)
        {
            EditorSceneManager.OpenScene(RegionSceneBuilder.ScenePath(sceneId), OpenSceneMode.Single);
            var definition = UnityEngine.Object.FindAnyObjectByType<RegionSceneDefinition>();
            if (definition == null || definition.SceneId != sceneId)
                throw new InvalidOperationException($"Formal scene '{sceneId}' has no matching definition.");

            var cameraObject = new GameObject("VisualRegressionCamera");
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.aspect = Width / (float)Height;
                camera.orthographicSize = Mathf.Max(
                    definition.Size.y * 0.5f,
                    definition.Size.x / (2f * camera.aspect)) + 0.5f;
                camera.transform.position = new Vector3(
                    definition.Size.x * 0.5f,
                    definition.Size.y * 0.5f,
                    -10f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
                camera.allowHDR = false;
                camera.allowMSAA = false;
                WriteCameraPng(camera, outputPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        public static float ChangedPixelRatio(string expectedPath, string actualPath)
        {
            using var expected = LoadPng(expectedPath);
            using var actual = LoadPng(actualPath);
            if (expected.width != actual.width || expected.height != actual.height)
                return 1f;
            Color32[] expectedPixels = expected.GetPixels32();
            Color32[] actualPixels = actual.GetPixels32();
            int changed = 0;
            for (var index = 0; index < expectedPixels.Length; index++)
            {
                if (!expectedPixels[index].Equals(actualPixels[index]))
                    changed++;
            }
            return changed / (float)expectedPixels.Length;
        }

        public static void CaptureMainMenu(string outputPath)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var camera = Camera.main;
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            var selector = transforms.First(value => value.name == "CharacterSelector").gameObject;
            var buttons = transforms.First(value => value.name == "ButtonContainer").gameObject;
            if (camera == null || canvas == null)
                throw new InvalidOperationException("MainMenu requires a camera and canvas.");
            selector.SetActive(true);
            buttons.SetActive(false);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            WriteCameraPng(camera, outputPath);
        }

        private static void WriteCameraPng(Camera camera, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
            var renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                filterMode = FilterMode.Point,
                useMipMap = false
            };
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            int previousAntiAliasing = QualitySettings.antiAliasing;
            try
            {
                QualitySettings.antiAliasing = 0;
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                texture.Apply(false, false);
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                QualitySettings.antiAliasing = previousAntiAliasing;
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture);
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }

            string normalized = outputPath.Replace('\\', '/');
            if (normalized.StartsWith("Assets/", StringComparison.Ordinal))
                AssetDatabase.ImportAsset(normalized, ImportAssetOptions.ForceSynchronousImport);
        }

        private static Texture2DHandle LoadPng(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidDataException("PNG decode failed: " + path);
            }
            return new Texture2DHandle(texture);
        }

        private sealed class Texture2DHandle : IDisposable
        {
            private readonly Texture2D value;

            public int width => value.width;
            public int height => value.height;

            public Texture2DHandle(Texture2D texture)
            {
                value = texture;
            }

            public Color32[] GetPixels32() => value.GetPixels32();

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }
    }
}
