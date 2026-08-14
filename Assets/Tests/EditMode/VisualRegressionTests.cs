using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using YuanHaiLu.Art;
using YuanHaiLu.Editor;

namespace YuanHaiLu.Tests.EditMode
{
    public sealed class VisualRegressionTests
    {
        [Test]
        public void CaptureSceneWritesFixedPixelImageAndRestoresEditorState()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var before = CaptureEditorState.Read();
            var outputPath = Path.Combine(Path.GetTempPath(), "yuanhailu-yanliu-capture-test.png");

            try
            {
                VisualRegressionCapture.CaptureScene("yanliu", outputPath);

                Assert.That(File.Exists(outputPath), Is.True);
                var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.That(image.LoadImage(File.ReadAllBytes(outputPath)), Is.True);
                    Assert.That(image.width, Is.EqualTo(480));
                    Assert.That(image.height, Is.EqualTo(270));
                }
                finally
                {
                    Object.DestroyImmediate(image);
                }

                Assert.That(CaptureEditorState.Read(), Is.EqualTo(before));
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void CaptureMainMenuRestoresCanvasPresentationFields()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var canvas = Object.FindAnyObjectByType<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            var renderMode = canvas.renderMode;
            var worldCamera = canvas.worldCamera;
            var planeDistance = canvas.planeDistance;
            var outputPath = Path.Combine(Path.GetTempPath(), "yuanhailu-main-menu-capture-test.png");

            try
            {
                VisualRegressionCapture.CaptureMainMenu(outputPath);

                Assert.That(canvas.renderMode, Is.EqualTo(renderMode));
                Assert.That(canvas.worldCamera, Is.EqualTo(worldCamera));
                Assert.That(canvas.planeDistance, Is.EqualTo(planeDistance));
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void BurnedCaptureRestoresAnAlreadyOpenPrologueState()
        {
            EditorSceneManager.OpenScene(
                RegionSceneBuilder.ScenePath("prologue_village"),
                OpenSceneMode.Single);
            var controller = Object.FindAnyObjectByType<RegionEnvironmentController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.CurrentEnvironmentState, Is.EqualTo("normal"));
            var outputPath = Path.Combine(Path.GetTempPath(), "yuanhailu-prologue-burned-capture-test.png");

            try
            {
                VisualRegressionCapture.CaptureScene("prologue_village", outputPath, "burned");

                Assert.That(controller.CurrentEnvironmentState, Is.EqualTo("normal"));
                Assert.That(controller.CurrentWeatherId, Is.EqualTo("clear"));
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
