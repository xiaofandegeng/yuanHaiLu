using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Tests.EditMode
{
    public class MainMenuSceneTests
    {
        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            typeof(ItemDatabase).GetField(
                "_items",
                BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
        }

        [Test]
        public void MainMenuCanvasCanDispatchPointerEvents()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var canvas = GameObject.Find("Canvas");

            Assert.That(canvas, Is.Not.Null, "MainMenu scene must contain its UI Canvas.");
            Assert.That(
                canvas.GetComponent<GraphicRaycaster>(),
                Is.Not.Null,
                "MainMenu buttons cannot receive pointer input without a GraphicRaycaster.");
        }

        [Test]
        public void DemoCameraStartsBehindWorldSprites()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Demo_YanLiuTown.unity", OpenSceneMode.Single);
            var camera = Camera.main;

            Assert.That(camera, Is.Not.Null, "Demo scene must contain a tagged Main Camera.");
            Assert.That(
                camera.transform.position.z,
                Is.LessThanOrEqualTo(-camera.nearClipPlane),
                "A 2D camera at z=0 clips every world sprite placed on z=0.");
        }
    }
}
