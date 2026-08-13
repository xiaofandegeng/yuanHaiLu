#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using YuanHaiLu.Art;

namespace YuanHaiLu.Tests.PlayMode
{
    public class EnvironmentTraversalPlayModeTests
    {
        [UnityTest]
        public IEnumerator AllFormalScenesLoadWithReachableAnchorCellsAndPersistentSprites()
        {
            var catalog = EnvironmentArtCatalog.LoadDefault();
            Assert.That(catalog.Entries.Count, Is.EqualTo(23));
            foreach (var entry in catalog.Entries)
            {
                var operation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                    entry.SceneAssetPath,
                    new LoadSceneParameters(LoadSceneMode.Additive));
                while (!operation.isDone)
                    yield return null;
                var scene = SceneManager.GetSceneByPath(entry.SceneAssetPath);
                Assert.That(scene.IsValid() && scene.isLoaded, Is.True, entry.RegionId);
                var roots = scene.GetRootGameObjects();
                var definition = roots.Select(root => root.GetComponent<RegionSceneDefinition>())
                    .FirstOrDefault(value => value != null);
                Assert.That(definition, Is.Not.Null, entry.RegionId);
                foreach (var anchor in definition.Anchors.Where(value => value.Type == "entry" || value.Type == "exit" || value.Type == "interior"))
                {
                    var point = new Vector2(anchor.Cell.x + 0.5f, anchor.Cell.y + 0.5f);
                    Assert.That(Physics2D.OverlapPoint(point), Is.Null, entry.RegionId + ":" + anchor.Id);
                }
                var renderers = roots.SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true));
                Assert.That(renderers.All(renderer => renderer.sprite != null), Is.True, entry.RegionId);
                var unload = SceneManager.UnloadSceneAsync(scene);
                while (unload != null && !unload.isDone)
                    yield return null;
            }
        }
    }
}
#endif
