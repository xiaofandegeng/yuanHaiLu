using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using YuanHaiLu.Core;
using YuanHaiLu.GameSystem;
using YuanHaiLu.UI;

namespace YuanHaiLu.Tests.EditMode
{
    public class RuntimePresentationTests
    {
        [TearDown]
        public void TearDown()
        {
            TestSceneFactory.DestroyAll();
        }

        [Test]
        public void PixelPerfectCameraClearsTheFullScreenOutsideItsViewport()
        {
            var cameraObject = TestSceneFactory.Create("Main Camera");
            var mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<PixelPerfectCamera>();

            Camera clearCamera = cameraObject
                .GetComponentsInChildren<Camera>(true)
                .Single(camera => camera != mainCamera);

            Assert.That(clearCamera.rect, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
            Assert.That(clearCamera.cullingMask, Is.Zero);
            Assert.That(clearCamera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
            Assert.That(clearCamera.depth, Is.LessThan(mainCamera.depth));
        }

        [Test]
        public void CameraFollowDoesNotJumpOutsideUnconfiguredBounds()
        {
            var cameraObject = TestSceneFactory.Create("Main Camera");
            cameraObject.AddComponent<Camera>().orthographicSize = 5f;
            var follow = TestSceneFactory.AddComponentWithAwake<CameraFollow>(cameraObject);
            var target = TestSceneFactory.Create("Player");
            target.transform.position = Vector3.zero;
            follow.SetTarget(target.transform);

            InvokePrivate(follow, "LateUpdate");

            Assert.That(cameraObject.transform.position.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(cameraObject.transform.position.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void PauseMenuBuildsVisibleDefaultPanelWhenReferencesAreMissing()
        {
            var root = TestSceneFactory.Create("Pause Canvas");
            root.AddComponent<Canvas>();
            var pauseMenu = root.AddComponent<PauseMenu>();

            InvokePrivate(pauseMenu, "Start");
            pauseMenu.Pause();

            var panel = root.transform.Find("PausePanel");
            Assert.That(panel, Is.Not.Null, "Generated Demo scenes need a usable fallback pause panel.");
            Assert.That(panel.gameObject.activeSelf, Is.True);
            Assert.That(panel.GetComponentsInChildren<Button>(true).Length, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void MissingSfxWarningIsLoggedOnlyOncePerClip()
        {
            var audioManager = TestSceneFactory.Create("AudioManager").AddComponent<AudioManager>();
            int warningCount = 0;
            Application.LogCallback captureMissingAudio = (condition, _, type) =>
            {
                if (type == LogType.Warning && condition.Contains("missing_test_sfx"))
                    warningCount++;
            };

            Application.logMessageReceived += captureMissingAudio;
            try
            {
                audioManager.PlaySFX("missing_test_sfx");
                audioManager.PlaySFX("missing_test_sfx");
            }
            finally
            {
                Application.logMessageReceived -= captureMissingAudio;
            }

            Assert.That(warningCount, Is.EqualTo(1));
        }

        private static void InvokePrivate(object target, string methodName)
        {
            target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }

    }
}
