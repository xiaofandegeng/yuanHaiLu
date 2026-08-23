#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using YuanHaiLu.Art;
using YuanHaiLu.Core;
using YuanHaiLu.Map;

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
                    Assert.That(
                        Physics2D.OverlapPoint(point, LayerMask.GetMask("Environment")),
                        Is.Null,
                        entry.RegionId + ":" + anchor.Id);
                }
                var renderers = roots.SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true));
                Assert.That(renderers.All(renderer => renderer.sprite != null), Is.True, entry.RegionId);
                var unload = SceneManager.UnloadSceneAsync(scene);
                while (unload != null && !unload.isDone)
                    yield return null;
            }
        }

        [UnityTest]
        public IEnumerator YanliuPortalLoadsInnAtItsDeclaredEntryAnchor()
        {
            var load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/Regions/yanliu.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!load.isDone)
                yield return null;
            yield return null;

            var player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(GameManager.Instance, Is.Not.Null);
            Assert.That(GameManager.Instance.CanPlayerAct(), Is.True,
                "A formal scene opened directly must remain in Exploration after bootstrap.");
            Assert.That(GameManager.Instance.CurrentSceneEntryMode,
                Is.EqualTo(GameManager.SceneEntryMode.Active),
                "A formal scene without SceneDirector must complete its own entry lifecycle.");
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(Camera.main.transform.position.z, Is.LessThan(-Camera.main.nearClipPlane),
                "A bootstrapped formal-scene camera must sit behind the z=0 pixel world.");
            var stats = player.GetComponent<YuanHaiLu.Character.CharacterStats>();
            int playerInstanceId = player.GetInstanceID();
            stats.currentHp = 37;
            stats.level = 7;
            var portal = Object.FindObjectsByType<AreaTrigger>(FindObjectsInactive.Include)
                .First(value => value.targetSceneName == "inn");
            portal.OnInteract(player);

            float deadline = Time.realtimeSinceStartup + 4f;
            while (SceneManager.GetActiveScene().name != "inn" &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("inn"));
            player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(player.GetInstanceID(), Is.EqualTo(playerInstanceId),
                "Formal travel must preserve the live player object and its runtime components.");
            Assert.That(GameManager.Instance.CurrentSceneEntryMode,
                Is.EqualTo(GameManager.SceneEntryMode.Active));
            stats = player.GetComponent<YuanHaiLu.Character.CharacterStats>();
            Assert.That(stats.currentHp, Is.EqualTo(37));
            Assert.That(stats.level, Is.EqualTo(7));
            Assert.That(player.GetComponent<YuanHaiLu.Character.PlayerController>().IsInputEnabled,
                Is.True,
                "Formal travel must restore player input after the destination bootstrap completes.");
            Assert.That((Vector2)player.transform.position,
                Is.EqualTo(new Vector2(2.5f, 2.5f)).Using(Vector2ComparerWithEqualsOperator.Instance));

            var cleanup = SceneManager.CreateScene("EnvironmentTraversalCleanup");
            SceneManager.SetActiveScene(cleanup);
            var inn = SceneManager.GetSceneByName("inn");
            var unload = SceneManager.UnloadSceneAsync(inn);
            while (unload != null && !unload.isDone)
                yield return null;
            if (GameManager.Instance != null)
                Object.Destroy(GameManager.Instance.gameObject);
            yield return null;
        }
    }
}
#endif
