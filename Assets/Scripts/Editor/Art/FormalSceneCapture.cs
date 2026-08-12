using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YuanHaiLu.Editor
{
    public static class FormalSceneCapture
    {
        private const string DemoOutputPath =
            "Assets/Art/Environment/previews/demo-yanliu-gameplay.png";
        private const string MenuOutputPath =
            "Assets/Art/Characters/Player/previews/main-menu-character-selection.png";

        [MenuItem("Tools/渊海录/美术/截取正式烟柳镇预览")]
        public static void CaptureDemo()
        {
            EditorSceneManager.OpenScene(
                "Assets/Scenes/Demo_YanLiuTown.unity",
                OpenSceneMode.Single);
            var camera = Camera.main;
            if (camera == null)
                throw new System.InvalidOperationException("Demo scene has no Main Camera.");

            camera.transform.position = new Vector3(20f, 12f, -10f);
            camera.aspect = 16f / 9f;
            RenderCamera(camera, DemoOutputPath);
            Debug.Log("[FormalSceneCapture] wrote " + DemoOutputPath);
        }

        [MenuItem("Tools/渊海录/美术/截取主菜单角色选择预览")]
        public static void CaptureMainMenu()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var camera = Camera.main;
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (camera == null || canvas == null)
                throw new System.InvalidOperationException("MainMenu requires a camera and canvas.");
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            RenderCamera(camera, MenuOutputPath);
            Debug.Log("[FormalSceneCapture] wrote " + MenuOutputPath);
        }

        private static void RenderCamera(Camera camera, string outputPath)
        {
            var renderTexture = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point
            };
            var texture = new Texture2D(960, 540, TextureFormat.RGBA32, false);
            var previousActive = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(texture);
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }

            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
        }

        public static void CaptureFromCommandLine()
        {
            CaptureDemo();
        }

        public static void CaptureAllFromCommandLine()
        {
            CaptureDemo();
            CaptureMainMenu();
        }
    }
}
