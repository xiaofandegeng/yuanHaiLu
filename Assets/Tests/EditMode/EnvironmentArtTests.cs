using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using YuanHaiLu.Art;
using YuanHaiLu.Editor;

namespace YuanHaiLu.Tests.EditMode
{
    public class EnvironmentArtTests
    {
        [Test]
        public void EnvironmentCatalogContainsTenRegionsAndThirteenInteriors()
        {
            EnvironmentTileBuilder.RebuildAll();
            RegionSceneBuilder.BuildAll();
            var catalog = EnvironmentArtCatalog.LoadDefault();

            Assert.That(catalog.Entries.Count, Is.EqualTo(23));
            Assert.That(catalog.Entries.Count(entry => entry.Kind == "region"), Is.EqualTo(10));
            Assert.That(catalog.Entries.Count(entry => entry.Kind == "interior"), Is.EqualTo(13));
            Assert.That(catalog.Entries.All(entry => File.Exists(entry.SceneAssetPath)), Is.True);
        }

        [TestCase("yanliu")]
        [TestCase("tianshu")]
        [TestCase("prologue_village")]
        [TestCase("pharmacy")]
        [TestCase("tomb")]
        public void RegionSceneContainsRequiredFormalLayers(string id)
        {
            RegionSceneBuilder.Build(id);
            var scene = EditorSceneManager.OpenScene(
                RegionSceneBuilder.ScenePath(id),
                OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var layerNames = roots.SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                    .Select(tilemap => tilemap.name)
                    .ToArray();
                Assert.That(layerNames, Is.EquivalentTo(new[]
                {
                    "Ground", "Water", "Lower Environment", "Buildings",
                    "Character", "Foreground", "Effects"
                }));
                var renderers = roots.SelectMany(
                    root => root.GetComponentsInChildren<SpriteRenderer>(true)).ToArray();
                Assert.That(renderers.Length, Is.GreaterThanOrEqualTo(1));
                Assert.That(renderers.All(renderer => renderer.sprite != null), Is.True);
                Assert.That(renderers.All(renderer => AssetDatabase.Contains(renderer.sprite)), Is.True);
                var definition = roots.Select(root => root.GetComponent<RegionSceneDefinition>())
                    .FirstOrDefault(value => value != null);
                Assert.That(definition, Is.Not.Null);
                Assert.That(definition.SceneId, Is.EqualTo(id));
                Assert.That(definition.Anchors.Any(anchor => anchor.Type == "entry"), Is.True);
                Assert.That(definition.Anchors.Any(anchor => anchor.Type == "exit"), Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
