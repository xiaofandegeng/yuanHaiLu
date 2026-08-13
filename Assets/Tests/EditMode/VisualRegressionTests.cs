using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using YuanHaiLu.Editor;

namespace YuanHaiLu.Tests.EditMode
{
    public class VisualRegressionTests
    {
        [Test]
        public void ApprovedMainMenuBaselineIsPixelSizedAndVisuallyNonEmpty()
        {
            AssertBaseline(Path.Combine(VisualRegressionCapture.BaselineRoot, "MainMenu.png"), "MainMenu");
        }

        [Test]
        public void ApprovedOutdoorBaselinesArePixelSizedAndVisuallyNonEmpty()
        {
            foreach (string sceneId in VisualRegressionCapture.OutdoorSceneIds)
            {
                string path = VisualRegressionCapture.BaselinePath(sceneId);
                AssertBaseline(path, sceneId);
            }
        }

        [Test]
        public void AllOutdoorCapturesStayWithinApprovedPixelDifference()
        {
            foreach (string sceneId in VisualRegressionCapture.OutdoorSceneIds)
            {
                string actual = Path.Combine(
                    Path.GetTempPath(),
                    "yuanhailu-visual-" + sceneId + ".png");
                VisualRegressionCapture.CaptureScene(sceneId, actual);
                Assert.That(
                    VisualRegressionCapture.ChangedPixelRatio(
                        VisualRegressionCapture.BaselinePath(sceneId),
                        actual),
                    Is.LessThanOrEqualTo(0.005f),
                    sceneId);
            }
        }

        [Test]
        public void MainMenuCaptureStaysWithinApprovedPixelDifference()
        {
            string actual = Path.Combine(Path.GetTempPath(), "yuanhailu-visual-mainmenu.png");
            VisualRegressionCapture.CaptureMainMenu(actual);
            Assert.That(
                VisualRegressionCapture.ChangedPixelRatio(
                    Path.Combine(VisualRegressionCapture.BaselineRoot, "MainMenu.png"),
                    actual),
                Is.LessThanOrEqualTo(0.005f),
                "MainMenu");
        }

        [Test]
        public void YanliuCaptureIsByteDeterministic()
        {
            string first = Path.Combine(Path.GetTempPath(), "yuanhailu-yanliu-first.png");
            string second = Path.Combine(Path.GetTempPath(), "yuanhailu-yanliu-second.png");
            VisualRegressionCapture.CaptureScene("yanliu", first);
            VisualRegressionCapture.CaptureScene("yanliu", second);

            CollectionAssert.AreEqual(File.ReadAllBytes(first), File.ReadAllBytes(second));
        }

        private static void AssertBaseline(string path, string label)
        {
            Assert.That(File.Exists(path), Is.True, label);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(path)), Is.True);
                Assert.That(texture.width, Is.EqualTo(VisualRegressionCapture.Width), label);
                Assert.That(texture.height, Is.EqualTo(VisualRegressionCapture.Height), label);
                var colors = new HashSet<uint>();
                foreach (Color32 pixel in texture.GetPixels32())
                {
                    if (pixel.a == 0) continue;
                    colors.Add((uint)(pixel.r << 24 | pixel.g << 16 | pixel.b << 8 | pixel.a));
                }
                Assert.That(colors.Count, Is.GreaterThan(24), label);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
