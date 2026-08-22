#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using YuanHaiLu.Core;

namespace YuanHaiLu.Tests.PlayMode
{
    /// <summary>
    /// docs/16 C.2：世界与 UI 必须共用同一 480×270 逻辑展示面。
    /// 用游戏相机离屏渲染 480×270，分别开关 HUD 画布对比像素：
    /// 两帧都非黑（世界在渲），且开/关 HUD 的两帧有差异
    /// （HUD 经同一台相机出现在同一逻辑画面上，而不是漂在窗口 Overlay）。
    /// </summary>
    public class LogicalFrameCapturePlayModeTests
    {
        private const int FrameWidth = 480;
        private const int FrameHeight = 270;

        [UnityTest]
        public IEnumerator LogicalFrameRendersBothWorldAndHudThroughOneCamera()
        {
            var load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/Demo_YanLiuTown.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!load.isDone) yield return null;
            yield return null;
            yield return null;

            var pixelCameras = Object.FindObjectsByType<PixelPerfectCamera>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.That(pixelCameras.Length, Is.EqualTo(1),
                "Demo 场景必须恰好一台像素相机");
            var camera = pixelCameras[0].GetComponent<Camera>();
            Assert.That(camera, Is.Not.Null);

            var hudCanvas = GameObject.Find("[HUD Canvas]");
            Assert.That(hudCanvas, Is.Not.Null);

            var renderTexture = new RenderTexture(
                FrameWidth, FrameHeight, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                // PixelPerfectCamera 检测到渲染目标变化后按 480×270 刷新。
                yield return null;
                Assert.That(camera.orthographicSize,
                    Is.EqualTo(8.4375f).Within(0.0001f),
                    "离屏 480×270 渲染时世界覆盖契约仍必须成立");
                Assert.That(pixelCameras[0].GetCurrentScale(), Is.EqualTo(1));

                hudCanvas.SetActive(false);
                yield return null;
                var withoutHud = ReadPixels(renderTexture);

                hudCanvas.SetActive(true);
                yield return null;
                var withHud = ReadPixels(renderTexture);

                Assert.That(CountNonBlack(withoutHud), Is.GreaterThan(2000),
                    "世界必须渲染进 480×270 逻辑画面（不能是黑帧）");
                Assert.That(CountDifferent(withHud, withoutHud), Is.GreaterThan(200),
                    "HUD 必须与世界出现在同一逻辑画面（Screen Space - Camera 绑定）");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                Object.Destroy(renderTexture);
            }
        }

        private static Color32[] ReadPixels(RenderTexture source)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = source;
            var texture = new Texture2D(
                FrameWidth, FrameHeight, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, FrameWidth, FrameHeight), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            var pixels = texture.GetPixels32();
            Object.Destroy(texture);
            return pixels;
        }

        private static int CountNonBlack(Color32[] pixels)
        {
            int count = 0;
            foreach (var pixel in pixels)
            {
                if (pixel.r > 16 || pixel.g > 16 || pixel.b > 16)
                    count++;
            }
            return count;
        }

        private static int CountDifferent(Color32[] left, Color32[] right)
        {
            int count = 0;
            for (int i = 0; i < left.Length; i++)
            {
                if (Mathf.Abs(left[i].r - right[i].r) > 16 ||
                    Mathf.Abs(left[i].g - right[i].g) > 16 ||
                    Mathf.Abs(left[i].b - right[i].b) > 16)
                    count++;
            }
            return count;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                foreach (var root in activeScene.GetRootGameObjects())
                    Object.Destroy(root);
            }
            if (GameManager.Instance != null)
                Object.Destroy(GameManager.Instance.gameObject);
            yield return null;
            yield return null;
        }
    }
}
#endif
