using System.IO;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using YuanHaiLu.Art;
using YuanHaiLu.Core;
using YuanHaiLu.Editor;
using YuanHaiLu.Map;

namespace YuanHaiLu.Tests.EditMode
{
    public class EnvironmentArtTests
    {
        [Test]
        public void EnvironmentCatalogContainsTenRegionsAndThirteenInteriors()
        {
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
                var ground = roots.SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                    .Single(tilemap => tilemap.name == "Ground");
                int persistedGroundCells = ground.GetTilesBlock(ground.cellBounds)
                    .Count(tile => tile != null);
                Assert.That(persistedGroundCells,
                    Is.GreaterThanOrEqualTo(definition.Size.x * definition.Size.y),
                    id + " must persist its complete formal ground Tilemap.");
                var buildings = roots.SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                    .Single(tilemap => tilemap.name == "Buildings");
                Assert.That(buildings.GetUsedTilesCount(), Is.GreaterThan(0),
                    id + " must contain authored structural tiles, not only a flat floor.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [TestCase("yanliu")]
        [TestCase("tianshu")]
        [TestCase("inn")]
        public void FormalSceneContainsPlayableEnvironmentIntegration(string id)
        {
            var scene = EditorSceneManager.OpenScene(
                RegionSceneBuilder.ScenePath(id),
                OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var allObjects = roots.SelectMany(root =>
                    root.GetComponentsInChildren<Transform>(true)).ToArray();
                Assert.That(
                    roots.SelectMany(root => root.GetComponentsInChildren<SceneBootstrapper>(true)).Count(),
                    Is.EqualTo(1),
                    id + " must bootstrap a formal player and camera when loaded directly.");
                Assert.That(
                    roots.SelectMany(root => root.GetComponentsInChildren<AreaTrigger>(true)),
                    Is.Not.Empty,
                    id + " must expose explicit connected travel portals.");
                Assert.That(
                    roots.SelectMany(root => root.GetComponentsInChildren<TilemapCollider2D>(true)),
                    Is.Not.Empty,
                    id + " must persist structural collision.");
                var buildings = roots.SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                    .Single(tilemap => tilemap.name == "Buildings");
                var usedBuildingTiles = new TileBase[buildings.GetUsedTilesCount()];
                buildings.GetUsedTilesNonAlloc(usedBuildingTiles);
                Assert.That(
                    usedBuildingTiles.OfType<Tile>()
                        .Any(tile => tile.colliderType != Tile.ColliderType.None),
                    Is.True,
                    id + " must give its Buildings TilemapCollider real collision shapes.");
                Assert.That(
                    allObjects.Count(value => value.GetComponent<BoxCollider2D>() != null),
                    Is.GreaterThanOrEqualTo(5),
                    id + " must contain map-boundary and landmark collision.");
                Assert.That(
                    roots.SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
                        .Any(renderer => renderer.sortingLayerName == "Foreground"),
                    Is.True,
                    id + " must split landmark foreground occlusion.");
                Assert.That(
                    allObjects.Any(value =>
                        value.GetComponent("YuanHaiLu.Art.RegionEnvironmentController") != null),
                    Is.True,
                    id + " must include day/night and weather presentation.");
                var environment = roots.SelectMany(root =>
                        root.GetComponentsInChildren<RegionEnvironmentController>(true))
                    .Single();
                Assert.That(environment.IsWeatherAnimated,
                    Is.EqualTo(id != "inn"),
                    id + " must use animated outdoor weather and static indoor ambience.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void EveryFormalTravelDestinationAndAnchorResolves()
        {
            var catalog = EnvironmentArtCatalog.LoadDefault();
            var anchorsByScene = new Dictionary<string, HashSet<string>>();
            foreach (var entry in catalog.Entries)
            {
                var scene = EditorSceneManager.OpenScene(entry.SceneAssetPath, OpenSceneMode.Additive);
                try
                {
                    var roots = scene.GetRootGameObjects();
                    var definition = roots.Select(root => root.GetComponent<RegionSceneDefinition>())
                        .First(value => value != null);
                    anchorsByScene.Add(
                        definition.SceneId,
                        definition.Anchors.Select(anchor => anchor.Id).ToHashSet());
                    var expectedLinks = FormalSceneTravelGraph.Outgoing(definition.SceneId).ToArray();
                    var portals = roots.SelectMany(
                        root => root.GetComponentsInChildren<AreaTrigger>(true)).ToArray();
                    Assert.That(expectedLinks, Is.Not.Empty, definition.SceneId);
                    Assert.That(portals.Length, Is.EqualTo(expectedLinks.Length), definition.SceneId);
                    foreach (var link in expectedLinks)
                    {
                        Assert.That(portals.Any(portal =>
                            portal.targetSceneName == link.TargetSceneId &&
                            portal.targetAnchorId == link.TargetAnchorId), Is.True,
                            link.SourceSceneId + "/" + link.PortalId);
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            foreach (var link in FormalSceneTravelGraph.All)
            {
                Assert.That(anchorsByScene.ContainsKey(link.TargetSceneId), Is.True,
                    link.TargetSceneId);
                Assert.That(anchorsByScene[link.TargetSceneId], Does.Contain(link.TargetAnchorId),
                    link.TargetSceneId + "/" + link.TargetAnchorId);
            }
        }
    }
}
