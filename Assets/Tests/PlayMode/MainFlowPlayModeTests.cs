#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using YuanHaiLu.Art;
using YuanHaiLu.Core;
using YuanHaiLu.UI;

namespace YuanHaiLu.Tests.PlayMode
{
    public class MainFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator SelectedAppearanceSurvivesMainMenuToFormalDemo()
        {
            var load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/MainMenu.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!load.isDone)
                yield return null;
            yield return null;

            var menu = Object.FindAnyObjectByType<MainMenu>();
            Assert.That(menu, Is.Not.Null);
            menu.SelectAppearance("player_male_boxer");
            menu.OnNewGame();

            yield return null;
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Demo_YanLiuTown"));
            Assert.That(Object.FindAnyObjectByType<RegionSceneDefinition>()?.SceneId,
                Is.EqualTo("yanliu"));
            Assert.That(GameManager.Instance.PlayerArtId, Is.EqualTo("player_male_boxer"));
            var player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(player.GetComponent<CharacterVisual>()?.ArtId,
                Is.EqualTo("player_male_boxer"));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                foreach (var root in activeScene.GetRootGameObjects())
                    Object.Destroy(root);
            }
            if (GameManager.Instance != null)
                Object.Destroy(GameManager.Instance.gameObject);
            yield return null;
            yield return null;
        }
    }
}
#endif
