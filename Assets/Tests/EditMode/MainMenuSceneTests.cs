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
        public void MainMenuOffersThreeWeaponStylesWithFixedMalePreview()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

            var styleButtons = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(button => button.name.StartsWith("Btn_流派_"))
                .ToArray();
            var appearanceButtons = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(button => button.name.StartsWith("Btn_角色_"))
                .ToArray();

            Assert.That(styleButtons.Select(button => button.name).OrderBy(name => name),
                Is.EqualTo(new[] { "Btn_流派_dart", "Btn_流派_gauntlets", "Btn_流派_sword" }));
            Assert.That(appearanceButtons, Is.Empty);
            var preview = GameObject.Find("CharacterPreview")?.GetComponent<Image>();
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.sprite, Is.Not.Null);
            Assert.That(AssetDatabase.Contains(preview.sprite), Is.True);
            Assert.That(GameObject.Find("StyleSelectionLabel")?.GetComponent<Text>(), Is.Not.Null);
            Assert.That(GameObject.Find("StyleSelectionHint")?.GetComponent<Text>(), Is.Not.Null);
            Assert.That(GameObject.Find("Btn_设置"), Is.Null);
            Assert.That(GameObject.Find("Btn_退出"), Is.Null);
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
        public void BuildSettingsContainsMenuDemoInnAndAllTwentyThreeFormalScenes()
        {
            SetupBuildSettings.Setup();
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();

            Assert.That(scenes, Has.Length.EqualTo(26));
            Assert.That(scenes[0].path, Is.EqualTo("Assets/Scenes/MainMenu.unity"));
            Assert.That(scenes[1].path, Is.EqualTo("Assets/Scenes/Demo_YanLiuTown.unity"));
            Assert.That(scenes[2].path, Is.EqualTo("Assets/Scenes/Demo_Inn.unity"));
            Assert.That(scenes.Count(scene => scene.path.StartsWith("Assets/Scenes/Regions/")),
                Is.EqualTo(10));
            Assert.That(scenes.Count(scene => scene.path.StartsWith("Assets/Scenes/Interiors/")),
                Is.EqualTo(13));
        }
    }
}
