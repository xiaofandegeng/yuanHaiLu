using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using YuanHaiLu.GameSystem;
using YuanHaiLu.Art;
using YuanHaiLu.Core;
using YuanHaiLu.Editor;
using UnityEditor;

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

        [Test]
        public void MainMenuContainsTwelveFormalAppearanceChoicesAndPreview()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

            var appearanceButtons = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(button => button.name.StartsWith("Btn_角色_"))
                .ToArray();

            Assert.That(appearanceButtons, Has.Length.EqualTo(12));
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            var preview = allTransforms.First(value => value.name == "CharacterPreview")
                .GetComponent<Image>();
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.sprite, Is.Not.Null);
            Assert.That(AssetDatabase.Contains(preview.sprite), Is.True);
            Assert.That(allTransforms.First(value => value.name == "CharacterSelectionLabel")
                .GetComponent<Text>(), Is.Not.Null);
            Assert.That(allTransforms.First(value => value.name == "Btn_确认角色")
                .GetComponent<Button>(), Is.Not.Null);
            Assert.That(allTransforms.First(value => value.name == "Btn_取消角色")
                .GetComponent<Button>(), Is.Not.Null);
            Assert.That(allTransforms.First(value => value.name == "CharacterSelector")
                .gameObject.activeSelf, Is.False);
        }

        [Test]
        public void DemoUsesFormalYanliuSceneAndFormalCharacterBindings()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Demo_YanLiuTown.unity", OpenSceneMode.Single);

            var definition = Object.FindAnyObjectByType<RegionSceneDefinition>();
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.SceneId, Is.EqualTo("yanliu"));

            var player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(player.GetComponent<CharacterVisual>()?.ArtId,
                Is.EqualTo(PlayerAppearance.Default.ArtId));

            var visuals = Object.FindObjectsByType<CharacterVisual>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Assert.That(visuals, Has.Length.GreaterThanOrEqualTo(7));
            Assert.That(visuals.All(value => AssetDatabase.Contains(
                value.GetComponent<SpriteRenderer>().sprite)), Is.True);
        }

        [Test]
        public void BuildSettingsContainsMenuDemoAndAllTwentyThreeFormalScenes()
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            var canonical = SetupBuildSettings.CanonicalScenePaths();

            Assert.That(scenes, Has.Length.EqualTo(25));
            CollectionAssert.AreEqual(canonical, scenes.Select(scene => scene.path).ToArray());
        }
    }
}
